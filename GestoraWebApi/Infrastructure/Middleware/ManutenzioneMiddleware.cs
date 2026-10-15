using GestoraWebApi.Common;
using GestoraWebApi.Models;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text.Json;

namespace GestoraWebApi.Infrastructure.Middleware
{
    /// <summary>
    /// V2-016 — blocco «portale in manutenzione». Finché la v3 non è rilasciata la produzione resta
    /// chiusa: il frontend mostra la sua schermata, ma l'API resta raggiungibile da chiunque
    /// conosca l'indirizzo. Senza questo blocco si potrebbe ancora registrarsi o far partire email
    /// chiamando l'API direttamente.
    ///
    /// Con Manutenzione:Attiva = true ogni richiesta sotto /api riceve 503 con il corpo
    /// ErrorDetails di sempre. /health resta libero: serve ad Azure e alle verifiche dopo un
    /// rilascio. Va registrato dopo UseCors, così il browser può leggere la risposta.
    /// </summary>
    public class ManutenzioneMiddleware
    {
        public const string Messaggio = "Gestora è in manutenzione: torna presto.";

        private readonly RequestDelegate _next;
        private readonly IOptionsMonitor<ManutenzioneSettings> _settings;

        public ManutenzioneMiddleware(RequestDelegate next, IOptionsMonitor<ManutenzioneSettings> settings)
        {
            _next = next;
            _settings = settings;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!_settings.CurrentValue.Attiva || !context.Request.Path.StartsWithSegments("/api"))
            {
                await _next(context);
                return;
            }

            var risposta = new ErrorDetails
            {
                StatusCode = (int)HttpStatusCode.ServiceUnavailable,
                Message = Messaggio
            };

            context.Response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
            context.Response.ContentType = "application/json";

            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            await context.Response.WriteAsync(JsonSerializer.Serialize(risposta, options));
        }
    }
}
