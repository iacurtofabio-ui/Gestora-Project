using GestoraWebApi.Services.Prenotazioni;
using Quartz;

namespace GestoraWebApi.Background
{
    public class PrenotazioniCleanupJob : IJob
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PrenotazioniCleanupJob> _logger;
        public PrenotazioniCleanupJob(IServiceProvider serviceProvider, ILogger<PrenotazioniCleanupJob> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }
        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("PrenotazioniCleanupJob started at {Time}", DateTime.Now);

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var prenotazioniService = scope.ServiceProvider.GetRequiredService<IPrenotazioniService>();
                await prenotazioniService.AutomaticDeletePrenotazioniAsync();

                _logger.LogInformation("PrenotazioniCleanupJob completed at {Time}", DateTime.Now);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la pulizia automatica delle vecchie prenotazioni.");
            }
        }

    }
}
