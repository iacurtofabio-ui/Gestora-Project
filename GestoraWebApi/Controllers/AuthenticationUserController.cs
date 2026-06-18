using GestoraWebApi.Auth;
using GestoraWebApi.Extensions;
using GestoraWebApi.Infrastructure.Auth;
using GestoraWebApi.Services.Auth.DTOs;
using GestoraWebApi.Services.LogActivity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;

namespace GestoraWebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthenticationUserController : Controller
    {
        private readonly IJwtTokenGenerator _tokenGenerator;
        private readonly ILogger<AuthenticationUserController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogActivityService _logActivityService;

        public AuthenticationUserController(IJwtTokenGenerator tokenGenerator,
                                            ILogger<AuthenticationUserController> logger,
                                            UserManager<ApplicationUser> userManager,
                                            ILogActivityService logActivityService)
        {
            _tokenGenerator = tokenGenerator;
            _logger = logger;
            _userManager = userManager;
            _logActivityService = logActivityService;
        }

        /// <summary>Registrazione pubblica — assegna automaticamente il ruolo Cliente</summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDTO request)
        {
            var user = new ApplicationUser { UserName = request.Username, Email = request.Email };
            var result = await _userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                _logger.LogWarning("[{Controller}] - [{Method}]: Registrazione fallita per {Email} - {Errors} - {Data}",
                    nameof(AuthenticationUserController), nameof(Register),
                    request.Email, string.Join(", ", result.Errors.Select(e => e.Description)), DateTime.Now);

                return BadRequest(result.Errors);
            }

            // Ogni nuovo utente registrato riceve il ruolo Cliente di default
            await _userManager.AddToRoleAsync(user, Roles.Cliente);

            _logger.LogInformation("[{Controller}] - [{Method}]: Registrazione riuscita per {Email} - {Data}",
                nameof(AuthenticationUserController), nameof(Register), request.Email, DateTime.Now);

            await _logActivityService.LogAsync(user.Id, $"Registrazione nuovo utente: {user.Email}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            return Ok($"Registrazione di '{user.UserName}' avvenuta con successo.");
        }

        /// <summary>Login — restituisce il token JWT con i ruoli dell'utente</summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            _logger.LogInformation("[{Controller}] - [{Method}]: Tentativo di login per {Email} - {Timestamp}",
                nameof(AuthenticationUserController), nameof(Login), request.Email, DateTime.UtcNow);

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
            {
                _logger.LogWarning("[{Controller}] - [{Method}]: Accesso non autorizzato per {Email} - {Timestamp}",
                    nameof(AuthenticationUserController), nameof(Login), request.Email, DateTime.UtcNow);

                return Unauthorized("Credenziali non valide.");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var token = _tokenGenerator.GenerateToken(user.Id, user.Email!, roles);

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            await _logActivityService.LogAsync(user.Id, "Login", ipAddress);

            _logger.LogInformation("[{Controller}] - [{Method}]: Login riuscito per {Email} - {Timestamp}",
                nameof(AuthenticationUserController), nameof(Login), request.Email, DateTime.UtcNow);

            return Ok(new { Email = user.Email, Token = token });
        }

        /// <summary>
        /// Crea il primo utente Admin del sistema — pubblico, ma si blocca se esiste già almeno un Admin.
        /// Usare esclusivamente al primo avvio su un DB pulito.
        /// </summary>
        [HttpPost("seed-admin")]
        public async Task<IActionResult> SeedAdmin([FromBody] RegisterDTO request)
        {
            // Blocca se esiste già almeno un utente con ruolo Admin
            var admins = await _userManager.GetUsersInRoleAsync(Roles.Admin);
            if (admins.Any())
                return Conflict("Un utente Admin esiste già nel sistema. Endpoint disabilitato.");

            var user = new ApplicationUser { UserName = request.Username, Email = request.Email };
            var result = await _userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            await _userManager.AddToRoleAsync(user, Roles.Admin);

            _logger.LogWarning("[{Controller}] - [{Method}]: Primo Admin '{Email}' creato via seed-admin - {Data}",
                nameof(AuthenticationUserController), nameof(SeedAdmin), request.Email, DateTime.Now);

            return Ok($"Utente Admin '{user.UserName}' creato con successo. Effettua il login per ottenere il token.");
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
            var users = _userManager.Users.ToList();

            var result = new List<UserResponseDTO>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                result.Add(new UserResponseDTO
                {
                    Id = user.Id,
                    UserName = user.UserName!,
                    Email = user.Email!,
                    Roles = roles
                });
            }

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