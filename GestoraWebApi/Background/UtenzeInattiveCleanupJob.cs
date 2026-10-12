using GestoraWebApi.Services.Utenti;
using Quartz;

namespace GestoraWebApi.Background
{
    // V2-015: stessa struttura di PrenotazioniCleanupJob. Due esecuzioni insieme (cron e avvio
    // manuale da JobsController) proverebbero a eliminare gli stessi utenti.
    [DisallowConcurrentExecution]
    public class UtenzeInattiveCleanupJob : IJob
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<UtenzeInattiveCleanupJob> _logger;

        public UtenzeInattiveCleanupJob(IServiceProvider serviceProvider, ILogger<UtenzeInattiveCleanupJob> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("UtenzeInattiveCleanupJob started at {Time}", DateTime.UtcNow);

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IUtenzeInattiveService>();
                await service.EliminaUtenzeMaiUsateAsync();

                _logger.LogInformation("UtenzeInattiveCleanupJob completed at {Time}", DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la pulizia automatica delle utenze mai usate.");
            }
        }
    }
}
