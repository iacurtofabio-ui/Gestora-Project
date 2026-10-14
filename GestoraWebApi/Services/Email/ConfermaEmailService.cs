using System.Text;
using GestoraWebApi.Auth;
using GestoraWebApi.Common;
using GestoraWebApi.Services.LogActivity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GestoraWebApi.Services.Email
{
    public interface IConfermaEmailService
    {
        /// <summary>Genera il token e manda il link di conferma. False se l'invio non è riuscito (l'utente resta).</summary>
        Task<bool> InviaConfermaAsync(ApplicationUser user, CancellationToken ct = default);

        /// <summary>Conferma l'email. False se utente o token non sono validi (o scaduti).</summary>
        Task<bool> ConfermaAsync(string userId, string token, string ip);

        /// <summary>
        /// Rimanda il link. Non dice mai se l'email esiste o è già confermata: chi chiama riceve
        /// sempre la stessa risposta, così non si può scoprire chi è registrato.
        /// </summary>
        Task ReinviaAsync(string email);
    }

    /// <summary>V2-003: conferma dell'email alla registrazione (token di Identity, link inviato per email).</summary>
    public class ConfermaEmailService : IConfermaEmailService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IInvioEmail _invioEmail;
        private readonly ILogActivityService _logActivityService;
        private readonly EmailSettings _settings;
        private readonly ILogger<ConfermaEmailService> _logger;

        public ConfermaEmailService(UserManager<ApplicationUser> userManager,
                                    IInvioEmail invioEmail,
                                    ILogActivityService logActivityService,
                                    IOptions<EmailSettings> settings,
                                    ILogger<ConfermaEmailService> logger)
        {
            _userManager = userManager;
            _invioEmail = invioEmail;
            _logActivityService = logActivityService;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<bool> InviaConfermaAsync(ApplicationUser user, CancellationToken ct = default)
        {
            try
            {
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

                // Il token di Identity contiene caratteri che negli indirizzi web danno problemi
                // (+, /, =): si manda in Base64 adatto agli URL e lo si decodifica alla conferma.
                var tokenInUrl = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
                var link = $"{_settings.UrlFrontend.TrimEnd('/')}/conferma-email?userId={Uri.EscapeDataString(user.Id)}&token={tokenInUrl}";

                var testo =
                    $"Ciao {user.UserName},{Environment.NewLine}{Environment.NewLine}" +
                    $"per completare la registrazione a Gestora conferma il tuo indirizzo email aprendo questo link:{Environment.NewLine}{Environment.NewLine}" +
                    $"{link}{Environment.NewLine}{Environment.NewLine}" +
                    "Il link è valido per 24 ore. Se non ti sei registrato tu, ignora questo messaggio.";

                await _invioEmail.InviaAsync(user.Email!, "Conferma la tua email su Gestora", testo, ct);
                return true;
            }
            catch (Exception ex)
            {
                // REV-070: nei log l'UserId, non l'email. L'utente resta registrato e puo' chiedere il reinvio.
                _logger.LogError(ex, "[{Service}] - [{Method}]: Invio dell'email di conferma non riuscito per {UserId}",
                    nameof(ConfermaEmailService), nameof(InviaConfermaAsync), user.Id);
                return false;
            }
        }

        public async Task<bool> ConfermaAsync(string userId, string token, string ip)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return false;

            // Cliccare due volte il link non deve sembrare un errore: la prima conferma ha gia'
            // cambiato il security stamp e il token non varrebbe piu'.
            if (user.EmailConfirmed)
                return true;

            string tokenOriginale;
            try
            {
                tokenOriginale = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
            }
            catch (FormatException)
            {
                return false;
            }

            var risultato = await _userManager.ConfirmEmailAsync(user, tokenOriginale);
            if (!risultato.Succeeded)
            {
                // Due richieste insieme (doppio clic, pagina caricata due volte): una conferma, l'altra
                // trova i dati gia' cambiati (ConcurrencyFailure). Se l'email risulta confermata nel
                // database, per chi ha cliccato e' un successo. Query nuova e non tracciata: l'utente
                // di questa richiesta e' ancora quello letto prima della conferma dell'altra.
                var oraConfermata = await _userManager.Users.AsNoTracking()
                    .AnyAsync(u => u.Id == user.Id && u.EmailConfirmed);
                if (oraConfermata)
                    return true;

                _logger.LogWarning("[{Service}] - [{Method}]: Conferma email rifiutata per {UserId} da {Ip} - {Errors}",
                    nameof(ConfermaEmailService), nameof(ConfermaAsync), user.Id, ip,
                    string.Join(", ", risultato.Errors.Select(e => e.Code)));
                return false;
            }

            await _logActivityService.LogAsync(user.Id, "Email confermata", ip);
            return true;
        }

        public async Task ReinviaAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null || user.EmailConfirmed)
                return;

            await InviaConfermaAsync(user);
        }
    }
}
