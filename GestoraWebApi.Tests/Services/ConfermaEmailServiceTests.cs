using System.Text;
using GestoraWebApi.Auth;
using GestoraWebApi.Common;
using GestoraWebApi.Services.Email;
using GestoraWebApi.Services.LogActivity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MockQueryable.Moq;
using Moq;

namespace GestoraWebApi.Tests.Services
{
    /// <summary>V2-003: conferma dell'email. Lo UserManager e l'invio sono finti: si prova la logica del servizio.</summary>
    public class ConfermaEmailServiceTests
    {
        private const string TokenIdentity = "CfDJ8+token/con=caratteri+speciali";

        private readonly Mock<UserManager<ApplicationUser>> _userManager;
        private readonly Mock<IInvioEmail> _invioEmail = new();
        private readonly Mock<ILogActivityService> _log = new();
        private readonly ConfermaEmailService _service;

        public ConfermaEmailServiceTests()
        {
            _userManager = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(), null!, null!, null!, null!, null!, null!, null!, null!);

            var settings = Options.Create(new EmailSettings { UrlFrontend = "https://gestora.example/" });
            _service = new ConfermaEmailService(_userManager.Object, _invioEmail.Object, _log.Object,
                settings, NullLogger<ConfermaEmailService>.Instance);
        }

        private static ApplicationUser Utente(bool confermato = false) => new()
        {
            Id = "utente-1",
            UserName = "Mario",
            Email = "mario@example.com",
            EmailConfirmed = confermato
        };

        private static string InUrl(string token) => WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

        // --- Invio ---

        [Fact]
        public async Task InviaConferma_MandaIlLinkConTokenCodificatoERitornaTrue()
        {
            var utente = Utente();
            _userManager.Setup(m => m.GenerateEmailConfirmationTokenAsync(utente)).ReturnsAsync(TokenIdentity);
            string? testoInviato = null;
            _invioEmail.Setup(i => i.InviaAsync("mario@example.com", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, string, CancellationToken>((_, _, testo, _) => testoInviato = testo)
                .Returns(Task.CompletedTask);

            var inviata = await _service.InviaConfermaAsync(utente);

            Assert.True(inviata);
            Assert.NotNull(testoInviato);
            // Senza "/" doppio dopo l'indirizzo del frontend, con userId e token pronti per l'URL.
            Assert.Contains($"https://gestora.example/conferma-email?userId=utente-1&token={InUrl(TokenIdentity)}", testoInviato);
            Assert.DoesNotContain("+", InUrl(TokenIdentity));
        }

        [Fact]
        public async Task InviaConferma_InvioFallito_RitornaFalseSenzaLanciare()
        {
            var utente = Utente();
            _userManager.Setup(m => m.GenerateEmailConfirmationTokenAsync(utente)).ReturnsAsync(TokenIdentity);
            _invioEmail.Setup(i => i.InviaAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("SMTP non raggiungibile"));

            var inviata = await _service.InviaConfermaAsync(utente);

            Assert.False(inviata);
        }

        // --- Conferma ---

        [Fact]
        public async Task Conferma_TokenValido_ConfermaERegistraNelLog()
        {
            var utente = Utente();
            _userManager.Setup(m => m.FindByIdAsync("utente-1")).ReturnsAsync(utente);
            _userManager.Setup(m => m.ConfirmEmailAsync(utente, TokenIdentity)).ReturnsAsync(IdentityResult.Success);

            var esito = await _service.ConfermaAsync("utente-1", InUrl(TokenIdentity), "1.2.3.4");

            Assert.True(esito);
            _userManager.Verify(m => m.ConfirmEmailAsync(utente, TokenIdentity), Times.Once);
            _log.Verify(l => l.LogAsync("utente-1", "Email confermata", "1.2.3.4"), Times.Once);
        }

        [Fact]
        public async Task Conferma_UtenteSconosciuto_RitornaFalse()
        {
            _userManager.Setup(m => m.FindByIdAsync("x")).ReturnsAsync((ApplicationUser?)null);

            Assert.False(await _service.ConfermaAsync("x", InUrl(TokenIdentity), "1.2.3.4"));
        }

        [Fact]
        public async Task Conferma_GiaConfermata_RitornaTrueSenzaConfermareDiNuovo()
        {
            // Secondo click sul link: la prima conferma ha gia' cambiato il security stamp.
            var utente = Utente(confermato: true);
            _userManager.Setup(m => m.FindByIdAsync("utente-1")).ReturnsAsync(utente);

            var esito = await _service.ConfermaAsync("utente-1", InUrl(TokenIdentity), "1.2.3.4");

            Assert.True(esito);
            _userManager.Verify(m => m.ConfirmEmailAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
            _log.Verify(l => l.LogAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()), Times.Never);
        }

        [Fact]
        public async Task Conferma_TokenNonDecodificabile_RitornaFalse()
        {
            var utente = Utente();
            _userManager.Setup(m => m.FindByIdAsync("utente-1")).ReturnsAsync(utente);

            var esito = await _service.ConfermaAsync("utente-1", "!!!non-base64!!!", "1.2.3.4");

            Assert.False(esito);
            _userManager.Verify(m => m.ConfirmEmailAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Conferma_TokenRifiutatoDaIdentity_RitornaFalseSenzaRegistro()
        {
            var utente = Utente();
            _userManager.Setup(m => m.FindByIdAsync("utente-1")).ReturnsAsync(utente);
            _userManager.Setup(m => m.ConfirmEmailAsync(utente, TokenIdentity))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "InvalidToken" }));
            _userManager.Setup(m => m.Users).Returns(new List<ApplicationUser> { Utente() }.AsQueryable().BuildMockDbSet().Object);

            var esito = await _service.ConfermaAsync("utente-1", InUrl(TokenIdentity), "1.2.3.4");

            Assert.False(esito);
            _log.Verify(l => l.LogAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()), Times.Never);
        }

        [Fact]
        public async Task Conferma_DueRichiesteInsieme_LaPerdenteRitornaTrueSeNelDatabaseEGiaConfermata()
        {
            // Doppio clic o pagina caricata due volte: l'altra richiesta ha confermato un attimo prima
            // e a questa Identity risponde ConcurrencyFailure. Per chi ha cliccato e' un successo.
            var utente = Utente();
            _userManager.Setup(m => m.FindByIdAsync("utente-1")).ReturnsAsync(utente);
            _userManager.Setup(m => m.ConfirmEmailAsync(utente, TokenIdentity))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "ConcurrencyFailure" }));
            _userManager.Setup(m => m.Users).Returns(new List<ApplicationUser> { Utente(confermato: true) }.AsQueryable().BuildMockDbSet().Object);

            var esito = await _service.ConfermaAsync("utente-1", InUrl(TokenIdentity), "1.2.3.4");

            Assert.True(esito);
            _log.Verify(l => l.LogAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()), Times.Never);
        }

        // --- Reinvio ---

        [Fact]
        public async Task Reinvia_EmailNonConfermata_MandaLaEmail()
        {
            var utente = Utente();
            _userManager.Setup(m => m.FindByEmailAsync("mario@example.com")).ReturnsAsync(utente);
            _userManager.Setup(m => m.GenerateEmailConfirmationTokenAsync(utente)).ReturnsAsync(TokenIdentity);

            await _service.ReinviaAsync("mario@example.com");

            _invioEmail.Verify(i => i.InviaAsync("mario@example.com", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Reinvia_EmailSconosciuta_NonFaNienteENonLancia()
        {
            _userManager.Setup(m => m.FindByEmailAsync("ignoto@example.com")).ReturnsAsync((ApplicationUser?)null);

            await _service.ReinviaAsync("ignoto@example.com");

            _invioEmail.Verify(i => i.InviaAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Reinvia_EmailGiaConfermata_NonManda()
        {
            _userManager.Setup(m => m.FindByEmailAsync("mario@example.com")).ReturnsAsync(Utente(confermato: true));

            await _service.ReinviaAsync("mario@example.com");

            _invioEmail.Verify(i => i.InviaAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
