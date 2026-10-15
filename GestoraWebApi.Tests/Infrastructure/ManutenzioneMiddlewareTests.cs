using GestoraWebApi.Common;
using GestoraWebApi.Infrastructure.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;
using System.Text.Json;

namespace GestoraWebApi.Tests.Infrastructure
{
    /// <summary>
    /// V2-016: con la manutenzione accesa nessuna chiamata a /api arriva ai controller, /health
    /// resta libero; spenta, il middleware non cambia niente.
    /// </summary>
    public class ManutenzioneMiddlewareTests
    {
        private static IOptionsMonitor<ManutenzioneSettings> Impostazioni(bool attiva)
        {
            var monitor = new Mock<IOptionsMonitor<ManutenzioneSettings>>();
            monitor.Setup(m => m.CurrentValue).Returns(new ManutenzioneSettings { Attiva = attiva });
            return monitor.Object;
        }

        private static async Task<(DefaultHttpContext Contesto, bool ProseguitoOltre)> Esegui(bool attiva, string percorso)
        {
            var proseguito = false;
            var middleware = new ManutenzioneMiddleware(_ => { proseguito = true; return Task.CompletedTask; }, Impostazioni(attiva));
            var contesto = new DefaultHttpContext();
            contesto.Request.Path = percorso;
            contesto.Response.Body = new MemoryStream();

            await middleware.InvokeAsync(contesto);
            return (contesto, proseguito);
        }

        [Theory]
        [InlineData("/api/AuthenticationUser/register")]
        [InlineData("/api/Prenotazione/check-disponibilita")]
        [InlineData("/API/Setup/stato")]
        public async Task Attiva_SuApi_Risponde503ESiFerma(string percorso)
        {
            var (contesto, proseguito) = await Esegui(attiva: true, percorso);

            Assert.False(proseguito);
            Assert.Equal(StatusCodes.Status503ServiceUnavailable, contesto.Response.StatusCode);
            Assert.Equal("application/json", contesto.Response.ContentType);
        }

        [Fact]
        public async Task Attiva_CorpoConIlMessaggioDiManutenzione()
        {
            var (contesto, _) = await Esegui(attiva: true, "/api/Zona/get-all-zone");

            contesto.Response.Body.Position = 0;
            using var json = await JsonDocument.ParseAsync(contesto.Response.Body);
            Assert.Equal(503, json.RootElement.GetProperty("statusCode").GetInt32());
            Assert.Equal(ManutenzioneMiddleware.Messaggio, json.RootElement.GetProperty("message").GetString());
        }

        [Fact]
        public async Task Attiva_HealthResta_Libero()
        {
            var (contesto, proseguito) = await Esegui(attiva: true, "/health");

            Assert.True(proseguito);
            Assert.Equal(StatusCodes.Status200OK, contesto.Response.StatusCode);
        }

        [Fact]
        public async Task Attiva_PercorsoCheIniziaConApiMaNonESottoApi_Prosegue()
        {
            var (_, proseguito) = await Esegui(attiva: true, "/apidoc");

            Assert.True(proseguito);
        }

        [Theory]
        [InlineData("/api/AuthenticationUser/login")]
        [InlineData("/health")]
        public async Task Spenta_NonCambiaNiente(string percorso)
        {
            var (contesto, proseguito) = await Esegui(attiva: false, percorso);

            Assert.True(proseguito);
            Assert.Equal(StatusCodes.Status200OK, contesto.Response.StatusCode);
        }
    }
}
