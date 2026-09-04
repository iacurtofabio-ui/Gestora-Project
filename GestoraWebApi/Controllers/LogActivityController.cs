using GestoraWebApi.Auth;
using GestoraWebApi.Services.LogActivity;
using GestoraWebApi.Services.LogActivity.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestoraWebApi.Controllers
{
    /// <summary>
    /// REV-037: lettura dell'audit trail. Finora la tabella <c>LogActivities</c> si scriveva e
    /// basta: per sapere chi aveva fatto cosa bisognava collegarsi al database di produzione,
    /// cioe' in pratica non si guardava mai.
    /// Riservato all'Admin: e' il registro di chi ha fatto cosa, e comprende gli indirizzi IP.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = Roles.Admin)]
    public class LogActivityController : ControllerBase
    {
        private readonly ILogActivityService _logActivityService;
        private readonly ILogger<LogActivityController> _logger;

        public LogActivityController(ILogActivityService logActivityService,
                                     ILogger<LogActivityController> logger)
        {
            _logActivityService = logActivityService;
            _logger = logger;
        }

        /// <summary>
        /// Elenco paginato del registro attivita', dal piu' recente. Filtri opzionali: utente,
        /// intervallo di date (UTC) e ricerca libera sul testo dell'azione.
        /// </summary>
        [HttpGet("get-log")]
        public async Task<IActionResult> GetLog([FromQuery] LogActivityQueryParams query)
        {
            var risultato = await _logActivityService.GetLogAsync(query);

            _logger.LogInformation("[{Controller}] - [{Method}]: letta pagina {Page} del registro attività ({Count} righe su {Totale})",
                nameof(LogActivityController), nameof(GetLog), risultato.Page, risultato.Items.Count, risultato.TotalCount);

            // Nessun 404 sulla lista vuota: un intervallo senza eventi e' una risposta, non un
            // errore (REV-031).
            return Ok(risultato);
        }
    }
}
