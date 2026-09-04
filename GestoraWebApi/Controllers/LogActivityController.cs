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

        /// <summary>
        /// ⚠️ ENDPOINT TEMPORANEO — da rimuovere una volta tarato <c>ForwardLimit</c> (REV-029).
        /// <para>
        /// Serve a misurare quanti proxy ci sono davvero davanti all'applicazione. Il valore di
        /// <c>ForwardLimit</c> deve corrispondere esattamente a quel numero: troppo basso e si
        /// registra l'indirizzo del proxy invece di quello del client (il caso che ha fatto
        /// nascere questa diagnostica), troppo alto e un client potrebbe iniettare un
        /// <c>X-Forwarded-For</c> fasullo e farsi passare per un altro indirizzo, aggirando il
        /// rate limit del login.
        /// </para>
        /// <para>
        /// Riservato all'Admin e volutamente limitato alla <b>richiesta corrente</b>: mostra solo
        /// i dati di chi sta chiamando, non tocca il traffico di altri utenti.
        /// </para>
        /// </summary>
        [HttpGet("diagnostica-inoltro")]
        public IActionResult DiagnosticaInoltro()
        {
            // X-Forwarded-For viene consumato dal middleware: gli anelli gia' elaborati sono
            // spostati in X-Original-Forwarded-For. Per ricostruire la catena completa servono
            // entrambi.
            string? Header(string nome) =>
                Request.Headers.TryGetValue(nome, out var valore) ? valore.ToString() : null;

            return Ok(new
            {
                // Cio' che l'applicazione crede sia l'indirizzo del client: e' il valore usato
                // dall'audit trail e dalla partizione del rate limit.
                remoteIpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),

                // La catena residua e quella gia' consumata dal middleware.
                xForwardedFor = Header("X-Forwarded-For"),
                xOriginalForwardedFor = Header("X-Original-Forwarded-For"),

                // Header alternativi usati da alcune piattaforme (Envoy, Cloudflare):
                // se uno di questi contiene l'indirizzo giusto, conviene leggere quello.
                xEnvoyExternalAddress = Header("X-Envoy-External-Address"),
                xRealIp = Header("X-Real-IP"),
                cfConnectingIp = Header("CF-Connecting-IP")
            });
        }
    }
}
