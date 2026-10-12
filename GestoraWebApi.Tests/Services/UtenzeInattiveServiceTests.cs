using GestoraWebApi.Auth;
using GestoraWebApi.Common;
using GestoraWebApi.Repositories.Utenti;
using GestoraWebApi.Services.LogActivity;
using GestoraWebApi.Services.Utenti;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace GestoraWebApi.Tests.Services;

/// <summary>
/// V2-015: chi è candidato lo decide il repository (UtentiRepositoryTests). Qui si prova cosa fa
/// il service con i candidati: la soglia giusta, l'eliminazione, la traccia nel registro, e che
/// un utente che non si riesce a eliminare non fermi gli altri.
/// </summary>
public class UtenzeInattiveServiceTests
{
    private static readonly DateTime Adesso = new(2026, 10, 12, 10, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IUtentiRepository> _repository = new();
    private readonly Mock<UserManager<ApplicationUser>> _userManager =
        new(Mock.Of<IUserStore<ApplicationUser>>(), null!, null!, null!, null!, null!, null!, null!, null!);
    private readonly Mock<ILogActivityService> _registro = new();

    private UtenzeInattiveService Service() => new(
        _repository.Object,
        _userManager.Object,
        _registro.Object,
        new TestClock(Adesso),
        Options.Create(new UtenzeSettings { GiorniUtenzaMaiUsata = 180 }),
        NullLogger<UtenzeInattiveService>.Instance);

    private void Candidati(params ApplicationUser[] utenti) =>
        _repository.Setup(r => r.GetUtenzeMaiUsateAsync(It.IsAny<DateTime>(), It.IsAny<int>()))
                   .ReturnsAsync(utenti.ToList());

    [Fact]
    public async Task ChiedeAlRepositoryLeUtenzeCreatePrimaDi180Giorni_ConIlTetto()
    {
        Candidati();

        await Service().EliminaUtenzeMaiUsateAsync();

        _repository.Verify(r => r.GetUtenzeMaiUsateAsync(
            Adesso.AddDays(-180), UtenzeInattiveService.MassimoPerEsecuzione), Times.Once);
    }

    [Fact]
    public async Task EliminaICandidati_ELasciaUnaRigaNelRegistroPerOgnuno()
    {
        var a = new ApplicationUser { Id = "a" };
        var b = new ApplicationUser { Id = "b" };
        Candidati(a, b);
        _userManager.Setup(m => m.DeleteAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);

        var eliminati = await Service().EliminaUtenzeMaiUsateAsync();

        Assert.Equal(2, eliminati);
        _registro.Verify(l => l.LogAsync("a", UtenzeInattiveService.AzioneRegistro, null), Times.Once);
        _registro.Verify(l => l.LogAsync("b", UtenzeInattiveService.AzioneRegistro, null), Times.Once);
    }

    [Fact]
    public async Task UnUtenteNonEliminabile_NonFermaGliAltri_ENonFinisceNelRegistro()
    {
        var rifiutato = new ApplicationUser { Id = "rifiutato" };
        var inErrore = new ApplicationUser { Id = "in-errore" };
        var ok = new ApplicationUser { Id = "ok" };
        Candidati(rifiutato, inErrore, ok);
        _userManager.Setup(m => m.DeleteAsync(rifiutato))
                    .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "no" }));
        // Es. una prenotazione arrivata nel frattempo: la FK Restrict fa fallire il DELETE.
        _userManager.Setup(m => m.DeleteAsync(inErrore)).ThrowsAsync(new InvalidOperationException("FK"));
        _userManager.Setup(m => m.DeleteAsync(ok)).ReturnsAsync(IdentityResult.Success);

        var eliminati = await Service().EliminaUtenzeMaiUsateAsync();

        Assert.Equal(1, eliminati);
        _registro.Verify(l => l.LogAsync("ok", It.IsAny<string>(), It.IsAny<string?>()), Times.Once);
        _registro.Verify(l => l.LogAsync("rifiutato", It.IsAny<string>(), It.IsAny<string?>()), Times.Never);
        _registro.Verify(l => l.LogAsync("in-errore", It.IsAny<string>(), It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task NessunCandidato_NonEliminaNulla()
    {
        Candidati();

        var eliminati = await Service().EliminaUtenzeMaiUsateAsync();

        Assert.Equal(0, eliminati);
        _userManager.Verify(m => m.DeleteAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }
}
