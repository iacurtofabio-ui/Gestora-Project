using GestoraWebApi.Services.Prenotazioni;
using Quartz;

namespace GestoraWebApi.Background
{
    // AUD-M10: JobsController puo' forzare il job mentre gira gia' dal cron (o al risveglio
    // dell'app su Azure F1, che recupera le esecuzioni perse): due esecuzioni insieme
    // lavorerebbero sulle stesse righe.
    [DisallowConcurrentExecution]
    public class PrenotazioniJob : IJob
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PrenotazioniJob> _logger;

        public PrenotazioniJob(IServiceProvider serviceProvider, ILogger<PrenotazioniJob> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("PrenotazioniJob started at {Time}", DateTime.UtcNow);

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var prenotazioniService = scope.ServiceProvider.GetRequiredService<IPrenotazioniService>();
                await prenotazioniService.AutomaticCompletPrenotazioniAsync();

                _logger.LogInformation("PrenotazioniJob completed at {Time}", DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il completamento automatico delle prenotazioni.");
            }
        }
    }
}
