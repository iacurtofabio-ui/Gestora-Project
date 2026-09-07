using GestoraWebApi.Auth;
using GestoraWebApi.Common;
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
        /// ⚠️ ENDPOINT TEMPORANEO — da rimuovere una volta confermato REV-029 in produzione.
        /// <para>
        /// Mostra affiancati l'indirizzo che l'applicazione usa davvero e gli header di inoltro
        /// grezzi. Serve perche' la catena di proxy non e' riproducibile in locale: in sviluppo
        /// questi header non esistono proprio, quindi l'unico modo di verificare la lettura e'
        /// guardarla dall'ambiente reale.
        /// </para>
        /// <para>
        /// Riservato all'Admin e volutamente limitato alla <b>richiesta corrente</b>: mostra solo
        /// i dati di chi sta chiamando, non tocca il traffico di altri utenti.
        /// </para>
        /// </summary>
        [HttpGet("diagnostica-inoltro")]
        public IActionResult DiagnosticaInoltro()
        {
            // Header grezzi, come arrivano.
            string? Header(string nome) =>
                Request.Headers.TryGetValue(nome, out var valore) ? valore.ToString() : null;

            return Ok(new
            {
                // ⬅️ Il valore che finisce davvero nell'audit trail e nella partizione del
                // rate limit: e' quello che deve corrispondere all'indirizzo di chi chiama.
                indirizzoUsatoDallApplicazione = IndirizzoClient.Ottieni(HttpContext),

                // Quello che vedrebbe l'applicazione senza la lettura esplicita dell'header:
                // dietro il proxy e' il suo indirizzo, uguale per tutti i client.
                remoteIpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),

                // La catena di inoltro: e' da qui che IndirizzoClient prende l'ultimo anello.
                xForwardedFor = Header("X-Forwarded-For"),

                // Se valorizzato, significa che un middleware ha consumato l'header prima di noi:
                // e' il caso da tenere d'occhio se un domani si reintroduce UseForwardedHeaders.
                xOriginalForwardedFor = Header("X-Original-Forwarded-For"),

                xForwardedProto = Header("X-Forwarded-Proto"),

                // Header alternativi di alcune piattaforme (Envoy, Cloudflare), tenuti per
                // riferimento: qui risultano tutti vuoti.
                xEnvoyExternalAddress = Header("X-Envoy-External-Address"),
                xRealIp = Header("X-Real-IP"),
                cfConnectingIp = Header("CF-Connecting-IP")
            });
        }
    }
}
