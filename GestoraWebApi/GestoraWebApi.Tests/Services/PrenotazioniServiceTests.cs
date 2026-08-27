using AutoMapper;
using GestoraWebApi.Context;
using GestoraWebApi.Enums;
using GestoraWebApi.Models;
using GestoraWebApi.Repositories.FasciaOrarie;
using GestoraWebApi.Repositories.Prenotazioni;
using GestoraWebApi.Repositories.Zone;
using GestoraWebApi.Services.PostazioneAssignment;
using GestoraWebApi.Services.Prenotazioni;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using GestoraWebApi.Services.LogActivity;

namespace GestoraWebApi.Tests.Services;

public class PrenotazioniServiceTests
{
    private readonly Mock<IPrenotazioniRepository> _prenotazioniRepoMock;
    private readonly Mock<IPostazioneAssignmentService> _assignmentServiceMock;
    private readonly Mock<IFasciaOrariaRepository> _fasciaRepoMock;
    private readonly Mock<IZonaRepository> _zonaRepoMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<IHttpContextAccessor> _httpContextMock;
    private readonly Mock<ILogger<PrenotazioniService>> _loggerMock;
    private readonly GestoraContext _context;
    private readonly PrenotazioniService _service;
    private readonly Mock<ILogActivityService> _logActivityMock;

    public PrenotazioniServiceTests()
    {
        _prenotazioniRepoMock = new Mock<IPrenotazioniRepository>();
        _assignmentServiceMock = new Mock<IPostazioneAssignmentService>();
        _fasciaRepoMock = new Mock<IFasciaOrariaRepository>();
        _zonaRepoMock = new Mock<IZonaRepository>();
        _mapperMock = new Mock<IMapper>();
        _httpContextMock = new Mock<IHttpContextAccessor>();
        _loggerMock = new Mock<ILogger<PrenotazioniService>>();
        _logActivityMock = new Mock<ILogActivityService>();


        // ↓ BLOCCO DA AGGIUNGERE ↓
        // Ruolo Staff: i test di questa classe esercitano il percorso Admin/Staff (nessun
        // vincolo di ownership/cutoff, vedi RBAC-002) su prenotazioni non necessariamente
        // proprie dell'utente autenticato.
        var claims = new[] {
  new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "user-test-123"),
  new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, GestoraWebApi.Auth.Roles.Staff) };
        var identity = new
    System.Security.Claims.ClaimsIdentity(claims,
    "Test");
        var claimsPrincipal = new System.Security.
    Claims.ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext
        {
            User = claimsPrincipal
        };
        _httpContextMock.Setup(h =>
    h.HttpContext).Returns(httpContext);
        // ↑ FINE BLOCCO ↑

        var options = new DbContextOptionsBuilder<GestoraContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new GestoraContext(options);

        _service = new PrenotazioniService(
            _prenotazioniRepoMock.Object,
            _assignmentServiceMock.Object,
            _fasciaRepoMock.Object,
            _mapperMock.Object,
            _context,
            _httpContextMock.Object,
            _zonaRepoMock.Object,
            _loggerMock.Object,
            _logActivityMock.Object);

    }

    // ─── DeleteAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ThrowsKeyNotFoundException_WhenPrenotazioneNonEsiste()
    {
        // Arrange
        _prenotazioniRepoMock.Setup(r => r.GetByIdAsync(99))
                             .ReturnsAsync((Prenotazione?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.DeleteAsync(99));
    }

    [Fact]
    public async Task DeleteAsync_ThrowsInvalidOperationException_WhenStatoIsInCorso()
    {
        // Arrange
        var prenotazione = new Prenotazione { Id = 1, NumeroCoperti = 2, Stato = StatoPrenotazione.InCorso };
        _prenotazioniRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(prenotazione);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeleteAsync(1));
    }

    [Fact]
    public async Task DeleteAsync_ThrowsInvalidOperationException_WhenStatoIsCompletata()
    {
        // Arrange
        var prenotazione = new Prenotazione { Id = 1, NumeroCoperti = 2, Stato = StatoPrenotazione.Completata };
        _prenotazioniRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(prenotazione);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeleteAsync(1));
    }

    // ─── AnnullaPrenotazioneAsync ──────────────────────────────────────────────

    [Fact]
    public async Task AnnullaPrenotazioneAsync_ThrowsKeyNotFoundException_WhenNonEsiste()
    {
        // Arrange
        _prenotazioniRepoMock.Setup(r => r.GetTrackedByIdAsync(99))
                             .ReturnsAsync((Prenotazione?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.AnnullaPrenotazioneAsync(99));
    }

    [Fact]
    public async Task AnnullaPrenotazioneAsync_ThrowsInvalidOperationException_WhenGiaCompletata()
    {
        // Arrange
        var prenotazione = new Prenotazione { Id = 1, NumeroCoperti = 2, Stato = StatoPrenotazione.Completata };
        _prenotazioniRepoMock.Setup(r => r.GetTrackedByIdAsync(1)).ReturnsAsync(prenotazione);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.AnnullaPrenotazioneAsync(1));
    }

    [Fact]
    public async Task AnnullaPrenotazioneAsync_SetsStatoAnnullata_WhenAttiva()
    {
        // Arrange
        var prenotazione = new Prenotazione { Id = 1, NumeroCoperti = 2, Stato = StatoPrenotazione.Attiva };
        _prenotazioniRepoMock.Setup(r => r.GetTrackedByIdAsync(1)).ReturnsAsync(prenotazione);

        // Act
        await _service.AnnullaPrenotazioneAsync(1);

        // Assert
        Assert.Equal(StatoPrenotazione.Annullata, prenotazione.Stato);
        _prenotazioniRepoMock.Verify(r => r.UpdateAsync(prenotazione), Times.Once);
    }

    // ─── AnnullaPrenotazioneAsync — RBAC-002 (cutoff self-service Cliente) ──────

    private void SetUserAsCliente(string userId)
    {
        var claims = new[] { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId) };
        var identity = new System.Security.Claims.ClaimsIdentity(claims, "Test");
        _httpContextMock.Setup(h => h.HttpContext).Returns(new DefaultHttpContext { User = new System.Security.Claims.ClaimsPrincipal(identity) });
    }

    [Fact]
    public async Task AnnullaPrenotazioneAsync_ThrowsUnauthorized_WhenClienteAnnullaPrenotazioneAltrui()
    {
        // Arrange
        SetUserAsCliente("user-test-123");
        var prenotazione = new Prenotazione { Id = 1, NumeroCoperti = 2, Stato = StatoPrenotazione.Attiva, UserId = "altro-utente" };
        _prenotazioniRepoMock.Setup(r => r.GetTrackedByIdAsync(1)).ReturnsAsync(prenotazione);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.AnnullaPrenotazioneAsync(1));
    }

    [Fact]
    public async Task AnnullaPrenotazioneAsync_ThrowsInvalidOperationException_WhenClienteOltreCutoff()
    {
        // Arrange — fascia che inizia "ora" (stesso istante UTC, senza offset): il limite di
        // cutoff (2h prima) è già passato indipendentemente dal fuso orario di GetNowInRome.
        SetUserAsCliente("user-test-123");
        var fasciaAdesso = new FasciaOraria { Id = 1, OrarioInizio = TimeOnly.FromDateTime(DateTime.UtcNow) };
        var prenotazione = new Prenotazione
        {
            Id = 1,
            NumeroCoperti = 2,
            Stato = StatoPrenotazione.Attiva,
            UserId = "user-test-123",
            DataPrenotazione = DateOnly.FromDateTime(DateTime.UtcNow),
            FasciaOraria = fasciaAdesso
        };
        _prenotazioniRepoMock.Setup(r => r.GetTrackedByIdAsync(1)).ReturnsAsync(prenotazione);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.AnnullaPrenotazioneAsync(1));
    }

    [Fact]
    public async Task AnnullaPrenotazioneAsync_SetsStatoAnnullata_WhenClienteEntroCutoff()
    {
        // Arrange — prenotazione fra 2 giorni: ampiamente oltre le 2 ore di cutoff, a
        // prescindere dal fuso orario.
        SetUserAsCliente("user-test-123");
        var fasciaFraDueGiorni = new FasciaOraria { Id = 1, OrarioInizio = new TimeOnly(20, 0) };
        var prenotazione = new Prenotazione
        {
            Id = 1,
            NumeroCoperti = 2,
            Stato = StatoPrenotazione.Attiva,
            UserId = "user-test-123",
            DataPrenotazione = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)),
            FasciaOraria = fasciaFraDueGiorni
        };
        _prenotazioniRepoMock.Setup(r => r.GetTrackedByIdAsync(1)).ReturnsAsync(prenotazione);

        // Act
        await _service.AnnullaPrenotazioneAsync(1);

        // Assert
        Assert.Equal(StatoPrenotazione.Annullata, prenotazione.Stato);
    }

    // ─── ConfermaPrenotazioneAsync ─────────────────────────────────────────────

    [Fact]
    public async Task ConfermaPrenotazioneAsync_ThrowsKeyNotFoundException_WhenNonEsiste()
    {
        // Arrange
        _prenotazioniRepoMock.Setup(r => r.GetTrackedByIdAsync(99))
                             .ReturnsAsync((Prenotazione?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.ConfermaPrenotazioneAsync(99));
    }

    [Fact]
    public async Task ConfermaPrenotazioneAsync_ThrowsInvalidOperationException_WhenNonAttiva()
    {
        // Arrange
        var prenotazione = new Prenotazione { Id = 1, NumeroCoperti = 2, Stato = StatoPrenotazione.InCorso };
        _prenotazioniRepoMock.Setup(r => r.GetTrackedByIdAsync(1)).ReturnsAsync(prenotazione);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.ConfermaPrenotazioneAsync(1));
    }

    [Fact]
    public async Task ConfermaPrenotazioneAsync_SetsStatoInCorso_WhenAttiva()
    {
        // Arrange
        var prenotazione = new Prenotazione { Id = 1, NumeroCoperti = 2, Stato = StatoPrenotazione.Attiva };
        _prenotazioniRepoMock.Setup(r => r.GetTrackedByIdAsync(1)).ReturnsAsync(prenotazione);

        // Act
        await _service.ConfermaPrenotazioneAsync(1);

        // Assert
        Assert.Equal(StatoPrenotazione.InCorso, prenotazione.Stato);
        _prenotazioniRepoMock.Verify(r => r.UpdateAsync(prenotazione), Times.Once);
    }
}
