using GestoraWebApi.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quartz;

namespace GestoraWebApi.Controllers
{
    // Solo Admin: forzare un job schedulato è un'operazione operativa/di manutenzione,
    // non una funzionalità applicativa.
    [Authorize(Roles = Roles.Admin)]
    [ApiController]
    [Route("api/[controller]")]
    public class JobsController : ControllerBase
    {
        private readonly ISchedulerFactory _schedulerFactory;
        private readonly ILogger<JobsController> _logger;

        public JobsController(ISchedulerFactory schedulerFactory, ILogger<JobsController> logger)
        {
            _schedulerFactory = schedulerFactory;
            _logger = logger;
        }

        /// <summary>
        /// Forza l'esecuzione immediata di un job Quartz già registrato (es. "PrenotazioniJob",
        /// "PrenotazioniCleanupJob"), senza attendere il trigger cron. Esegue esattamente lo
        /// stesso codice che girerebbe alla schedulazione reale (stessa risoluzione DI, stesso
        /// service, stessa scrittura sul DB) — utile per verificare un flusso automatizzato a
        /// comando invece che aspettarne l'orario, o per rieseguirlo manualmente in caso di
        /// necessità operativa. Solo Admin.
        /// </summary>
        [HttpPost("trigger/{jobName}")]
        public async Task<IActionResult> TriggerJobAsync(string jobName)
        {
            var scheduler = await _schedulerFactory.GetScheduler();
            var jobKey = new JobKey(jobName);

            if (!await scheduler.CheckExists(jobKey))
                return NotFound(new { message = $"Nessun job registrato con nome '{jobName}'." });

            await scheduler.TriggerJob(jobKey);
            _logger.LogInformation("Job '{JobName}' forzato manualmente via API da un Admin.", jobName);

            return Accepted(new
            {
                message = $"Job '{jobName}' avviato manualmente. L'esecuzione è asincrona: controlla i log per l'esito."
            });
        }
    }
}
