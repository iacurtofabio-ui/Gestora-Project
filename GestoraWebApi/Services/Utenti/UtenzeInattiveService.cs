using GestoraWebApi.Auth;
using GestoraWebApi.Common;
using GestoraWebApi.Repositories.Utenti;
using GestoraWebApi.Services.LogActivity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace GestoraWebApi.Services.Utenti
{
    /// <summary>
    /// V2-015: la registrazione è pubblica e nessuno eliminava mai un'utenza, quindi gli account
    /// creati e mai usati (veri o finti) restavano per sempre. Qui si tolgono quelli che dopo
    /// <see cref="UtenzeSettings.GiorniUtenzaMaiUsata"/> giorni non sono mai entrati e non hanno
    /// prenotazioni. Chi conta come "mai usata" lo decide <see cref="IUtentiRepository"/>.
    /// </summary>
    public class UtenzeInattiveService : IUtenzeInattiveService
    {
        /// <summary>
        /// Tetto per esecuzione: dopo un'ondata di registrazioni finte il job non deve restare
        /// acceso per minuti. Il resto passa le notti successive.
        /// </summary>
        public const int MassimoPerEsecuzione = 500;

        public const string AzioneRegistro = "Utenza mai usata eliminata automaticamente";

        private readonly IUtentiRepository _repository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogActivityService _logActivityService;
        private readonly IClock _clock;
        private readonly UtenzeSettings _impostazioni;
        private readonly ILogger<UtenzeInattiveService> _logger;

        public UtenzeInattiveService(IUtentiRepository repository,
                                     UserManager<ApplicationUser> userManager,
                                     ILogActivityService logActivityService,
                                     IClock clock,
                                     IOptions<UtenzeSettings> impostazioni,
                                     ILogger<UtenzeInattiveService> logger)
        {
            _repository = repository;
            _userManager = userManager;
            _logActivityService = logActivityService;
            _clock = clock;
            _impostazioni = impostazioni.Value;
            _logger = logger;
        }

        public async Task<int> EliminaUtenzeMaiUsateAsync()
        {
            var soglia = _clock.UtcNow.AddDays(-_impostazioni.GiorniUtenzaMaiUsata);
            var candidati = await _repository.GetUtenzeMaiUsateAsync(soglia, MassimoPerEsecuzione);

            var eliminati = 0;
            foreach (var utente in candidati)
            {
                // Un errore su un utente (es. una prenotazione arrivata nel frattempo, fermata
                // dalla FK) non deve bloccare gli altri: si annota e si passa al successivo.
                try
                {
                    var esito = await _userManager.DeleteAsync(utente);
                    if (!esito.Succeeded)
                    {
                        _logger.LogWarning("[{Service}] - [{Method}]: Utenza {UserId} non eliminata - {Errors}",
                            nameof(UtenzeInattiveService), nameof(EliminaUtenzeMaiUsateAsync), utente.Id,
                            string.Join(", ", esito.Errors.Select(e => e.Description)));
                        continue;
                    }

                    await _logActivityService.LogAsync(utente.Id, AzioneRegistro);
                    eliminati++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[{Service}] - [{Method}]: Errore eliminando l'utenza {UserId}",
                        nameof(UtenzeInattiveService), nameof(EliminaUtenzeMaiUsateAsync), utente.Id);
                }
            }

            _logger.LogInformation("[{Service}] - [{Method}]: {Eliminati} utenze mai usate eliminate su {Candidati} trovate (create prima del {Soglia:yyyy-MM-dd})",
                nameof(UtenzeInattiveService), nameof(EliminaUtenzeMaiUsateAsync), eliminati, candidati.Count, soglia);

            return eliminati;
        }
    }
}
