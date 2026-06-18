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
    private readonly Mock<IPrenotazioniRepository>      _prenotazioniRepoMock;
    private readonly Mock<IPostazioneAssignmentService> _assignmentServiceMock;
    private readonly Mock<IFasciaOrariaRepository>      _fasciaRepoMock;
    private readonly Mock<IZonaRepository>              _zonaRepoMock;
    private readonly Mock<IMapper>                      _mapperMock;
    private readonly Mock<IHttpContextAccessor>         _httpContextMock;
    private readonly Mock<ILogger<PrenotazioniService>> _loggerMock;
    private readonly GestoraContext                     _context;
    private readonly PrenotazioniService                _service;
    private readonly Mock<ILogActivityService> _logActivityMock;

    public PrenotazioniServiceTests()
    {
        _prenotazioniRepoMock  = new Mock<IPrenotazioniRepository>();
        _assignmentServiceMock = new Mock<IPostazioneAssignmentService>();
        _fasciaRepoMock        = new Mock<IFasciaOrariaRepository>();
        _zonaRepoMock          = new Mock<IZonaRepository>();
        _mapperMock            = new Mock<IMapper>();
        _httpContextMock       = new Mock<IHttpContextAccessor>();
        _loggerMock            = new Mock<ILogger<PrenotazioniService>>();
        _logActivityMock       = new Mock<ILogActivityService>();

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
