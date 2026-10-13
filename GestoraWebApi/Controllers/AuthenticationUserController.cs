using GestoraWebApi.Auth;
using GestoraWebApi.Common;
using GestoraWebApi.Context;
using GestoraWebApi.Extensions;
using GestoraWebApi.Infrastructure.Auth;
using GestoraWebApi.Infrastructure.RateLimiting;
using GestoraWebApi.Services.Auth.DTOs;
using GestoraWebApi.Services.LogActivity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace GestoraWebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthenticationUserController : Controller
    {
        private readonly IJwtTokenGenerator _tokenGenerator;
        private readonly ILogger<AuthenticationUserController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogActivityService _logActivityService;
        private readonly GestoraContext _context;
        private readonly IClock _clock;
        private readonly IEsecutoreTransazione _transazione;

        public AuthenticationUserController(IJwtTokenGenerator tokenGenerator,
                                            ILogger<AuthenticationUserController> logger,
                                            UserManager<ApplicationUser> userManager,
                                            SignInManager<ApplicationUser> signInManager,
                                            ILogActivityService logActivityService,
                                            GestoraContext context,
                                            IClock clock,
                                            IEsecutoreTransazione transazione)
        {
            _transazione = transazione;
            _clock = clock;
            _tokenGenerator = tokenGenerator;
            _logger = logger;
            _userManager = userManager;
            _signInManager = signInManager;
            _logActivityService = logActivityService;
            _context = context;
        }

        /// <summary>Registrazione pubblica — assegna automaticamente il ruolo Cliente</summary>
        [EnableRateLimiting(LimitiRichieste.Registrazione)]
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDTO request)
        {
            var user = new ApplicationUser
            {
                UserName = request.Username,
                Email = request.Email,
                DataCreazione = _clock.UtcNow
            };
            var result = await _userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                // REV-070: nessun identificativo personale nei log. Su una registrazione fallita
                // l'email non appartiene nemmeno a un nostro utente: restano il motivo e l'IP.
                _logger.LogWarning("[{Controller}] - [{Method}]: Registrazione fallita da {Ip} - {Errors}",
                    nameof(AuthenticationUserController), nameof(Register),
                    HttpContext.GetIpAddress(), string.Join(", ", result.Errors.Select(e => e.Description)));

                throw ErroriIdentity.ComeValidationException(result, "Registrazione non riuscita: controlla i dati inseriti.");
            }

            // Ogni nuovo utente registrato riceve il ruolo Cliente di default. Il risultato era
            // ignorato: se falliva, l'utente restava senza ruolo e riceveva 403 ovunque, senza
            // traccia nei log. Si elimina l'utente appena creato, cosi' puo' riprovare.
            var roleResult = await _userManager.AddToRoleAsync(user, Roles.Cliente);
            if (!roleResult.Succeeded)
            {
                _logger.LogError("[{Controller}] - [{Method}]: Ruolo Cliente non assegnato a {UserId} - {Errors}",
                    nameof(AuthenticationUserController), nameof(Register), user.Id,
                    string.Join(", ", roleResult.Errors.Select(e => e.Description)));

                await _userManager.DeleteAsync(user);
                throw new InvalidOperationException("Assegnazione del ruolo Cliente non riuscita.");
            }

            _logger.LogInformation("[{Controller}] - [{Method}]: Registrazione riuscita per {UserId}",
                nameof(AuthenticationUserController), nameof(Register), user.Id);

            // L'audit log su database identifica gia' l'utente con user.Id (primo parametro):
            // ripetere l'email nel testo del messaggio la duplicherebbe senza aggiungere nulla.
            await _logActivityService.LogAsync(user.Id, "Registrazione nuovo utente", HttpContext.GetIpAddress());

            return Ok($"Registrazione di '{user.UserName}' avvenuta con successo.");
        }

        /// <summary>Login — restituisce il token JWT con i ruoli dell'utente</summary>
        [EnableRateLimiting("LoginPolicy")]
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            // REV-070: l'email non entra nei log. Su un login fallito e' l'indirizzo di chi
            // prova, non necessariamente di un nostro utente: registrarla significa conservare
            // dati personali di terzi a ogni tentativo, anche di un attacco a dizionario.
            // Per correlare i tentativi basta l'IP; a login riuscito si usa l'UserId.
            _logger.LogInformation("[{Controller}] - [{Method}]: Tentativo di login da {Ip}",
                nameof(AuthenticationUserController), nameof(Login), HttpContext.GetIpAddress());

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                _logger.LogWarning("[{Controller}] - [{Method}]: Accesso non autorizzato da {Ip} (utente inesistente)",
                    nameof(AuthenticationUserController), nameof(Login), HttpContext.GetIpAddress());

                return Unauthorized("Credenziali non valide.");
            }

            // CheckPasswordSignInAsync (invece di CheckPasswordAsync) tiene traccia dei
            // tentativi falliti e applica il lockout configurato in AuthenticationExtensions.
            var signInResult = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

            if (signInResult.IsLockedOut)
            {
                _logger.LogWarning("[{Controller}] - [{Method}]: Account {UserId} bloccato per troppi tentativi falliti (da {Ip})",
                    nameof(AuthenticationUserController), nameof(Login), user.Id, HttpContext.GetIpAddress());

                return StatusCode(StatusCodes.Status423Locked,
                    "Account temporaneamente bloccato per troppi tentativi falliti. Riprova più tardi.");
            }

            if (!signInResult.Succeeded)
            {
                _logger.LogWarning("[{Controller}] - [{Method}]: Accesso non autorizzato per {UserId} (password errata, da {Ip})",
                    nameof(AuthenticationUserController), nameof(Login), user.Id, HttpContext.GetIpAddress());

                return Unauthorized("Credenziali non valide.");
            }

            // V2-015: un'utenza che ha fatto almeno un login non e' "mai usata" e la pulizia
            // notturna non la tocca. UpdateAsync non cambia il security stamp: i token gia'
            // emessi restano validi. Se fallisce si avvisa e si entra lo stesso: un campo di
            // manutenzione non deve impedire l'accesso.
            user.UltimoAccesso = _clock.UtcNow;
            var aggiornamento = await _userManager.UpdateAsync(user);
            if (!aggiornamento.Succeeded)
            {
                _logger.LogWarning("[{Controller}] - [{Method}]: Ultimo accesso non salvato per {UserId} - {Errors}",
                    nameof(AuthenticationUserController), nameof(Login), user.Id,
                    string.Join(", ", aggiornamento.Errors.Select(e => e.Description)));
            }

            var roles = await _userManager.GetRolesAsync(user);
            var token = _tokenGenerator.GenerateToken(user.Id, user.Email!, roles,
                await _userManager.GetSecurityStampAsync(user));

            await _logActivityService.LogAsync(user.Id, "Login", HttpContext.GetIpAddress());

            _logger.LogInformation("[{Controller}] - [{Method}]: Login riuscito per {UserId}",
                nameof(AuthenticationUserController), nameof(Login), user.Id);

            return Ok(new { Email = user.Email, Token = token });
        }

        /// <summary>
        /// V2-010 — rinnovo del token, per qualsiasi utente con un token ancora valido. Il frontend
        /// lo chiama da solo poco prima della scadenza, finché la pagina è aperta: lo Staff non viene
        /// più buttato fuori ogni ora. Il nuovo token conserva l'inizio della sessione, quindi non va
        /// mai oltre JwtSettings:MaxSessionHours dal login; superato quel limite si rifà il login.
        /// Ruoli e security stamp sono riletti adesso: un account modificato è già respinto da
        /// OnTokenValidated prima di arrivare qui. Nessuna riga nel registro attività: sarebbe una
        /// ogni ora per ogni utente collegato.
        /// </summary>
        [Authorize]
        [HttpPost("rinnova-token")]
        public async Task<IActionResult> RinnovaToken()
        {
            var userId = User.GetAuthenticatedUserId();
            var inizioSessione = JwtTokenGenerator.LeggiInizioSessione(User);

            if (inizioSessione is null || !_tokenGenerator.SessioneAncoraValida(inizioSessione.Value))
            {
                _logger.LogInformation("[{Controller}] - [{Method}]: Rinnovo rifiutato per {UserId}, sessione al limite massimo",
                    nameof(AuthenticationUserController), nameof(RinnovaToken), userId);

                return Unauthorized("Sessione scaduta. Effettua di nuovo l'accesso.");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return Unauthorized("Sessione scaduta. Effettua di nuovo l'accesso.");

            var roles = await _userManager.GetRolesAsync(user);
            var token = _tokenGenerator.GenerateToken(user.Id, user.Email!, roles,
                await _userManager.GetSecurityStampAsync(user), inizioSessione.Value);

            return Ok(new { Email = user.Email, Token = token });
        }

        /// <summary>Assegna un ruolo a un utente — solo Admin</summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpPost("assign-role")]
        public async Task<IActionResult> AssignRole([FromBody] AssignRoleDTO dto)
        {
            if (!new[] { Roles.Admin, Roles.Staff, Roles.Cliente }.Contains(dto.Role))
                return BadRequest($"Ruolo '{dto.Role}' non valido. Ruoli disponibili: Admin, Staff, Cliente.");

            var user = await _userManager.FindByIdAsync(dto.UserId);
            if (user == null)
                return NotFound($"Utente con ID '{dto.UserId}' non trovato.");

            if (await _userManager.IsInRoleAsync(user, dto.Role))
                return BadRequest($"L'utente è già nel ruolo '{dto.Role}'.");

            var result = await _userManager.AddToRoleAsync(user, dto.Role);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            // AUD-M6: i token gia' emessi portano i ruoli vecchi. Cambiare lo stamp li invalida:
            // l'utente rifa' l'accesso e riceve un token con i ruoli giusti.
            await _userManager.UpdateSecurityStampAsync(user);

            _logger.LogInformation("[{Controller}] - [{Method}]: Ruolo '{Role}' assegnato all'utente {UserId}",
                nameof(AuthenticationUserController), nameof(AssignRole), dto.Role, dto.UserId);

            await _logActivityService.LogAsync(User.GetAuthenticatedUserId(),
                $"Ruolo '{dto.Role}' assegnato a utente ID {dto.UserId}",
                IndirizzoClient.Ottieni(HttpContext));

            return Ok(new { message = $"Ruolo '{dto.Role}' assegnato con successo." });
        }

        /// <summary>Rimuove un ruolo da un utente — solo Admin</summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpDelete("remove-role")]
        public async Task<IActionResult> RemoveRole([FromBody] AssignRoleDTO dto)
        {
            // Chi chiama e' sempre un Admin e non puo' eliminare se stesso (DeleteUser): vietando
            // anche di togliersi il ruolo Admin, almeno un Admin resta sempre. Senza questo blocco,
            // rimasti a zero Admin, la schermata di primo avvio (SetupController, anonima) si
            // riaprirebbe a chiunque.
            if (dto.Role == Roles.Admin && dto.UserId == User.GetAuthenticatedUserId())
                return BadRequest("Non è possibile togliere a se stessi il ruolo Admin: chiedilo a un altro Admin.");

            var user = await _userManager.FindByIdAsync(dto.UserId);
            if (user == null)
                return NotFound($"Utente con ID '{dto.UserId}' non trovato.");

            if (!await _userManager.IsInRoleAsync(user, dto.Role))
                return BadRequest($"L'utente non è nel ruolo '{dto.Role}'.");

            var result = await _userManager.RemoveFromRoleAsync(user, dto.Role);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            // AUD-M6: senza questo l'utente conservava il ruolo tolto fino alla scadenza del token.
            await _userManager.UpdateSecurityStampAsync(user);

            _logger.LogInformation("[{Controller}] - [{Method}]: Ruolo '{Role}' rimosso dall'utente {UserId}",
                nameof(AuthenticationUserController), nameof(RemoveRole), dto.Role, dto.UserId);

            await _logActivityService.LogAsync(User.GetAuthenticatedUserId(),
                $"Ruolo '{dto.Role}' rimosso da utente ID {dto.UserId}",
                IndirizzoClient.Ottieni(HttpContext));

            return Ok(new { message = $"Ruolo '{dto.Role}' rimosso con successo." });
        }

        /// <summary>Lista tutti gli utenti con i loro ruoli — solo Admin</summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpGet("get-users")]
        public async Task<IActionResult> GetUsers()
        {
            // REV-021: prima si caricavano tutti gli utenti e poi si chiamava GetRolesAsync
            // dentro il ciclo, cioe' una query al database per ogni utente (N+1). Con pochi
            // utenti non si nota; il costo cresce pero' in modo lineare e questa e' la pagina
            // che l'Admin apre per gestirli. Ora sono due query fisse: gli utenti, e in un solo
            // colpo tutte le assegnazioni di ruolo, che poi si accostano in memoria.
            var users = await _context.Users
                .AsNoTracking()
                .Select(u => new { u.Id, u.UserName, u.Email })
                .ToListAsync();

            var assegnazioni = await (from ur in _context.UserRoles
                                      join r in _context.Roles on ur.RoleId equals r.Id
                                      select new { ur.UserId, RoleName = r.Name })
                                     .AsNoTracking()
                                     .ToListAsync();

            var ruoliPerUtente = assegnazioni
                .GroupBy(a => a.UserId)
                .ToDictionary(g => g.Key, g => (IList<string>)g.Select(a => a.RoleName!).ToList());

            var result = users.Select(u => new UserResponseDTO
            {
                Id = u.Id,
                UserName = u.UserName!,
                Email = u.Email!,
                // Un utente senza alcun ruolo non compare fra le assegnazioni: va reso con una
                // lista vuota, non saltato, altrimenti sparirebbe dall'elenco dell'Admin.
                Roles = ruoliPerUtente.TryGetValue(u.Id, out var ruoli) ? ruoli : new List<string>()
            }).ToList();

            return Ok(result);
        }

        /// <summary>Dettaglio singolo utente — solo Admin</summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpGet("get-user/{id}")]
        public async Task<IActionResult> GetUserById(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound($"Utente con ID '{id}' non trovato.");

            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new UserResponseDTO
            {
                Id = user.Id,
                UserName = user.UserName!,
                Email = user.Email!,
                Roles = roles
            });
        }

        /// <summary>Aggiorna username e/o email di un utente — solo Admin</summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpPut("update-user/{id}")]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUserDTO dto)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound($"Utente con ID '{id}' non trovato.");

            var nomeCambiato = !string.IsNullOrWhiteSpace(dto.UserName) &&
                               !string.Equals(dto.UserName, user.UserName, StringComparison.Ordinal);

            var emailCambiata = !string.IsNullOrWhiteSpace(dto.Email) &&
                                !string.Equals(dto.Email, user.Email, StringComparison.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(dto.UserName))
                user.UserName = dto.UserName;

            if (!string.IsNullOrWhiteSpace(dto.Email))
                user.Email = dto.Email;

            // V2-014: nel registro vanno i NOMI dei campi cambiati, non i valori (l'email e' un
            // dato personale e il registro non si svuota).
            var campi = new List<string>();
            if (nomeCambiato) campi.Add("nome utente");
            if (emailCambiata) campi.Add("email");

            // Modifica, aggiornamento del security stamp e traccia: una sola operazione.
            IdentityResult result = IdentityResult.Success;
            await _transazione.EseguiAsync(async () =>
            {
                result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                    return;

                // Il token porta l'email: se cambia, quelli gia' emessi vanno rifatti (AUD-M6).
                if (emailCambiata)
                    await _userManager.UpdateSecurityStampAsync(user);

                if (campi.Count > 0)
                    await _logActivityService.LogAsync(User.GetAuthenticatedUserId(),
                        $"Modificato utente ID {id} ({string.Join(", ", campi)})",
                        IndirizzoClient.Ottieni(HttpContext));
            });

            if (!result.Succeeded)
                throw ErroriIdentity.ComeValidationException(result, "Modifica dell'utente non riuscita: controlla i dati inseriti.");

            _logger.LogInformation("[{Controller}] - [{Method}]: Utente {Id} aggiornato",
                nameof(AuthenticationUserController), nameof(UpdateUser), id);

            return Ok(new { message = "Utente aggiornato con successo." });
        }

        /// <summary>Elimina un utente dal sistema — solo Admin. Non è possibile eliminare se stessi.</summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpDelete("delete-user/{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var currentUserId = User.GetAuthenticatedUserId();
            if (currentUserId == id)
                return BadRequest("Non è possibile eliminare il proprio account.");

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound($"Utente con ID '{id}' non trovato.");

            // REV-038: la relazione utente -> prenotazioni non e' piu' a cascata, quindi il
            // database rifiuterebbe comunque l'eliminazione. Il controllo si fa qui per dare un
            // messaggio comprensibile invece di un errore tecnico di chiave esterna, e perche'
            // il motivo del rifiuto ("ha delle prenotazioni") e' un'informazione utile.
            var haPrenotazioni = await _context.Prenotazioni.AnyAsync(p => p.UserId == id);
            if (haPrenotazioni)
            {
                return Conflict(new
                {
                    message = "Impossibile eliminare l'utente: ha prenotazioni registrate. " +
                              "Lo storico delle prenotazioni non va cancellato perché è la base " +
                              "dei dati di riepilogo."
                });
            }

            // V2-014: eliminazione e traccia sono una sola operazione. Prima, se il registro
            // falliva, l'utente era gia' stato eliminato senza nessuna traccia.
            IdentityResult result = IdentityResult.Success;
            await _transazione.EseguiAsync(async () =>
            {
                result = await _userManager.DeleteAsync(user);
                if (!result.Succeeded)
                    return;

                await _logActivityService.LogAsync(User.GetAuthenticatedUserId(),
                    $"Eliminato utente ID {id}",
                    IndirizzoClient.Ottieni(HttpContext));
            });

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            _logger.LogWarning("[{Controller}] - [{Method}]: Utente {Id} eliminato da Admin {AdminId}",
                nameof(AuthenticationUserController), nameof(DeleteUser), id, currentUserId);

            return Ok(new { message = "Utente eliminato con successo." });
        }

        /// <summary>Reset password di un utente — solo Admin</summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpPost("reset-password/{id}")]
        public async Task<IActionResult> ResetPassword(string id, [FromBody] AdminResetPasswordDTO dto)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound($"Utente con ID '{id}' non trovato.");

            // Genera un token di reset e applica subito la nuova password
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, dto.NewPassword);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            _logger.LogWarning("[{Controller}] - [{Method}]: Password resettata per utente {Id}",
                nameof(AuthenticationUserController), nameof(ResetPassword), id);

            await _logActivityService.LogAsync(User.GetAuthenticatedUserId(),
                $"Password resettata per utente ID {id}",
                IndirizzoClient.Ottieni(HttpContext));

            return Ok(new { message = "Password resettata con successo." });
        }
    }
}