using GestoraWebApi.Auth;
using GestoraWebApi.Context;
using GestoraWebApi.Extensions;
using GestoraWebApi.Infrastructure.Auth;
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

        public AuthenticationUserController(IJwtTokenGenerator tokenGenerator,
                                            ILogger<AuthenticationUserController> logger,
                                            UserManager<ApplicationUser> userManager,
                                            SignInManager<ApplicationUser> signInManager,
                                            ILogActivityService logActivityService,
                                            GestoraContext context)
        {
            _tokenGenerator = tokenGenerator;
            _logger = logger;
            _userManager = userManager;
            _signInManager = signInManager;
            _logActivityService = logActivityService;
            _context = context;
        }

        private string? GetIpAddress()
            => HttpContext.Connection.RemoteIpAddress?.ToString();

        /// <summary>Registrazione pubblica — assegna automaticamente il ruolo Cliente</summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDTO request)
        {
            var user = new ApplicationUser { UserName = request.Username, Email = request.Email };
            var result = await _userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                // REV-070: nessun identificativo personale nei log. Su una registrazione fallita
                // l'email non appartiene nemmeno a un nostro utente: restano il motivo e l'IP.
                _logger.LogWarning("[{Controller}] - [{Method}]: Registrazione fallita da {Ip} - {Errors} - {Data}",
                    nameof(AuthenticationUserController), nameof(Register),
                    GetIpAddress(), string.Join(", ", result.Errors.Select(e => e.Description)), DateTime.Now);

                return BadRequest(result.Errors);
            }

            // Ogni nuovo utente registrato riceve il ruolo Cliente di default
            await _userManager.AddToRoleAsync(user, Roles.Cliente);

            _logger.LogInformation("[{Controller}] - [{Method}]: Registrazione riuscita per {UserId} - {Data}",
                nameof(AuthenticationUserController), nameof(Register), user.Id, DateTime.Now);

            // L'audit log su database identifica gia' l'utente con user.Id (primo parametro):
            // ripetere l'email nel testo del messaggio la duplicherebbe senza aggiungere nulla.
            await _logActivityService.LogAsync(user.Id, "Registrazione nuovo utente", GetIpAddress());

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
            _logger.LogInformation("[{Controller}] - [{Method}]: Tentativo di login da {Ip} - {Timestamp}",
                nameof(AuthenticationUserController), nameof(Login), GetIpAddress(), DateTime.UtcNow);

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                _logger.LogWarning("[{Controller}] - [{Method}]: Accesso non autorizzato da {Ip} (utente inesistente) - {Timestamp}",
                    nameof(AuthenticationUserController), nameof(Login), GetIpAddress(), DateTime.UtcNow);

                return Unauthorized("Credenziali non valide.");
            }

            // CheckPasswordSignInAsync (invece di CheckPasswordAsync) tiene traccia dei
            // tentativi falliti e applica il lockout configurato in AuthenticationExtensions.
            var signInResult = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

            if (signInResult.IsLockedOut)
            {
                _logger.LogWarning("[{Controller}] - [{Method}]: Account {UserId} bloccato per troppi tentativi falliti (da {Ip}) - {Timestamp}",
                    nameof(AuthenticationUserController), nameof(Login), user.Id, GetIpAddress(), DateTime.UtcNow);

                return StatusCode(StatusCodes.Status423Locked,
                    "Account temporaneamente bloccato per troppi tentativi falliti. Riprova più tardi.");
            }

            if (!signInResult.Succeeded)
            {
                _logger.LogWarning("[{Controller}] - [{Method}]: Accesso non autorizzato per {UserId} (password errata, da {Ip}) - {Timestamp}",
                    nameof(AuthenticationUserController), nameof(Login), user.Id, GetIpAddress(), DateTime.UtcNow);

                return Unauthorized("Credenziali non valide.");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var token = _tokenGenerator.GenerateToken(user.Id, user.Email!, roles);

            await _logActivityService.LogAsync(user.Id, "Login", GetIpAddress());

            _logger.LogInformation("[{Controller}] - [{Method}]: Login riuscito per {UserId} - {Timestamp}",
                nameof(AuthenticationUserController), nameof(Login), user.Id, DateTime.UtcNow);

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

            _logger.LogInformation("[{Controller}] - [{Method}]: Ruolo '{Role}' assegnato all'utente {UserId} - {Data}",
                nameof(AuthenticationUserController), nameof(AssignRole), dto.Role, dto.UserId, DateTime.Now);

            await _logActivityService.LogAsync(User.GetAuthenticatedUserId(),
                $"Ruolo '{dto.Role}' assegnato a utente ID {dto.UserId}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            return Ok(new { message = $"Ruolo '{dto.Role}' assegnato con successo." });
        }

        /// <summary>Rimuove un ruolo da un utente — solo Admin</summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpDelete("remove-role")]
        public async Task<IActionResult> RemoveRole([FromBody] AssignRoleDTO dto)
        {
            var user = await _userManager.FindByIdAsync(dto.UserId);
            if (user == null)
                return NotFound($"Utente con ID '{dto.UserId}' non trovato.");

            if (!await _userManager.IsInRoleAsync(user, dto.Role))
                return BadRequest($"L'utente non è nel ruolo '{dto.Role}'.");

            var result = await _userManager.RemoveFromRoleAsync(user, dto.Role);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            _logger.LogInformation("[{Controller}] - [{Method}]: Ruolo '{Role}' rimosso dall'utente {UserId} - {Data}",
                nameof(AuthenticationUserController), nameof(RemoveRole), dto.Role, dto.UserId, DateTime.Now);

            await _logActivityService.LogAsync(User.GetAuthenticatedUserId(),
                $"Ruolo '{dto.Role}' rimosso da utente ID {dto.UserId}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

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

            if (!string.IsNullOrWhiteSpace(dto.UserName))
                user.UserName = dto.UserName;

            if (!string.IsNullOrWhiteSpace(dto.Email))
                user.Email = dto.Email;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            _logger.LogInformation("[{Controller}] - [{Method}]: Utente {Id} aggiornato - {Data}",
                nameof(AuthenticationUserController), nameof(UpdateUser), id, DateTime.Now);

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

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            _logger.LogWarning("[{Controller}] - [{Method}]: Utente {Id} eliminato da Admin {AdminId} - {Data}",
                nameof(AuthenticationUserController), nameof(DeleteUser), id, currentUserId, DateTime.Now);

            await _logActivityService.LogAsync(User.GetAuthenticatedUserId(),
                $"Eliminato utente ID {id}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

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

            _logger.LogWarning("[{Controller}] - [{Method}]: Password resettata per utente {Id} - {Data}",
                nameof(AuthenticationUserController), nameof(ResetPassword), id, DateTime.Now);

            await _logActivityService.LogAsync(User.GetAuthenticatedUserId(),
                $"Password resettata per utente ID {id}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            return Ok(new { message = "Password resettata con successo." });
        }
    }
}