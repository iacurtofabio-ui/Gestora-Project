using AutoMapper;
using GestoraWebApi.Models;
using GestoraWebApi.Repositories.Zone;
using GestoraWebApi.Services.LogActivity;
using GestoraWebApi.Services.Sala;
using GestoraWebApi.Services.Zone;
using GestoraWebApi.Services.Zone.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using System.Security.Claims;
using GestoraWebApi.Infrastructure.Exceptions;

namespace GestoraWebApi.Tests.Services;

public class ZonaServiceTests
{
    private readonly Mock<IZonaRepository> _repoMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly IMemoryCache _cache;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<ILogActivityService> _logActivityMock;
    private readonly EsecutoreTransazioneFinto _transazione;
    private readonly Mock<ICoerenzaSalaService> _coerenzaSala = new();
    private readonly ZonaService _service;

    public ZonaServiceTests()
    {
        _repoMock    = new Mock<IZonaRepository>();
        _mapperMock  = new Mock<IMapper>();
        _cache       = new MemoryCache(new MemoryCacheOptions());
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "test-user-id") }))
        });
        _logActivityMock = new Mock<ILogActivityService>();
        _transazione = new EsecutoreTransazioneFinto();
        _service     = new ZonaService(_repoMock.Object, _mapperMock.Object, _cache,
                                        _httpContextAccessorMock.Object, _logActivityMock.Object,
                                        _transazione, _coerenzaSala.Object);
    }

    [Fact]
    public async Task AddAsync_ThrowsConflictException_WhenNomeEsiste()
    {
        // Arrange
        var dto = new ZonaDTO { Nome = "Terrazza" };
        _repoMock.Setup(r => r.GetByNameAsync("Terrazza"))
                 .ReturnsAsync(new Zona { Id = 1, Nome = "Terrazza" });

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() => _service.AddAsync(dto));
    }

    [Fact]
    public async Task AddAsync_CallsRepositoryAddAsync_WhenNomeNonEsiste()
    {
        // Arrange
        var dto  = new ZonaDTO { Nome = "Giardino" };
        var zona = new Zona { Nome = "Giardino" };

        _repoMock.Setup(r => r.GetByNameAsync("Giardino")).ReturnsAsync((Zona?)null);
        _mapperMock.Setup(m => m.Map<Zona>(dto)).Returns(zona);

        // Act
        await _service.AddAsync(dto);

        // Assert
        _repoMock.Verify(r => r.AddAsync(zona), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ThrowsNotFoundException_WhenZonaNonEsiste()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Zona?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteAsync(99));
    }

    [Fact]
    public async Task DeleteAsync_ThrowsConflictException_WhenZonaUsata()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Zona { Id = 1, Nome = "Sala" });
        _repoMock.Setup(r => r.IsZonaUsataAsync(1)).ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() => _service.DeleteAsync(1));
    }

    [Fact]
    public async Task GetAllZoneAsync_HitsRepository_OnlyOnce_WhenCalledTwice()
    {
        // Arrange
        var zone = new List<Zona> { new Zona { Id = 1, Nome = "Sala", Attiva = true } };
        var dtos = new List<ZonaDTO> { new ZonaDTO { Nome = "Sala" } };

        _repoMock.Setup(r => r.GetAllZoneAsync()).ReturnsAsync(zone);
        _mapperMock.Setup(m => m.Map<List<ZonaDTO>>(zone)).Returns(dtos);

        // Act
        var firstCall  = await _service.GetAllZoneAsync();
        var secondCall = await _service.GetAllZoneAsync();

        // Assert: seconda chiamata servita dalla cache, repository chiamato una sola volta
        _repoMock.Verify(r => r.GetAllZoneAsync(), Times.Once);
        Assert.Same(firstCall, secondCall);
    }
    // ─── REV-032 — audit log nella stessa transazione della scrittura ────────

    [Fact]
    public async Task AddAsync_ScritturaEAuditLog_StannoNellaStessaOperazioneAtomica()
    {
        // L'esecutore finto qui non esegue il blocco: se ne' la scrittura ne' il log partono,
        // vuol dire che sono entrambi dentro. Prima erano due SaveChanges separati e la zona
        // poteva restare a database senza nessuna traccia di chi l'aveva creata.
        _transazione.Esegui = false;
        _repoMock.Setup(r => r.GetByNameAsync("Terrazza")).ReturnsAsync((Zona?)null);
        _mapperMock.Setup(m => m.Map<Zona>(It.IsAny<ZonaDTO>()))
                   .Returns(new Zona { Id = 1, Nome = "Terrazza", Attiva = true });

        await _service.AddAsync(new ZonaDTO { Nome = "Terrazza", Attiva = true });

        Assert.Equal(1, _transazione.Chiamate);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<Zona>()), Times.Never);
        _logActivityMock.Verify(l => l.LogAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task AddAsync_ConTransazioneEseguita_ScriveERegistra()
    {
        _repoMock.Setup(r => r.GetByNameAsync("Terrazza")).ReturnsAsync((Zona?)null);
        _mapperMock.Setup(m => m.Map<Zona>(It.IsAny<ZonaDTO>()))
                   .Returns(new Zona { Id = 1, Nome = "Terrazza", Attiva = true });

        await _service.AddAsync(new ZonaDTO { Nome = "Terrazza", Attiva = true });

        _repoMock.Verify(r => r.AddAsync(It.IsAny<Zona>()), Times.Once);
        _logActivityMock.Verify(l => l.LogAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()), Times.Once);
    }

    // Il controllo di validita' resta fuori dalla transazione: aprirla per poi scoprire che la
    // richiesta e' da rifiutare sarebbe lavoro sprecato sul database.
    [Fact]
    public async Task AddAsync_NonApreLaTransazione_SeIlNomeEGiaUsato()
    {
        _repoMock.Setup(r => r.GetByNameAsync("Sala")).ReturnsAsync(new Zona { Id = 9, Nome = "Sala" });

        await Assert.ThrowsAsync<ConflictException>(() => _service.AddAsync(new ZonaDTO { Nome = "Sala" }));

        Assert.Equal(0, _transazione.Chiamate);
    }

    // ─── V2-007 — spegnere una zona toglie i suoi tavoli dai posti della sala ──

    private void SalaSottoIlTetto() =>
        _coerenzaSala.Setup(c => c.VerificaModificaSalaAsync(It.IsAny<Func<IReadOnlyList<TavoloSala>, IEnumerable<TavoloSala>>>(),
                                                              It.IsAny<Func<IReadOnlySet<long>, IEnumerable<long>>?>()))
                     .ThrowsAsync(new ConflictException("sotto il tetto"));

    private void NessunControllo() =>
        _coerenzaSala.Verify(c => c.VerificaModificaSalaAsync(It.IsAny<Func<IReadOnlyList<TavoloSala>, IEnumerable<TavoloSala>>>(),
                                                              It.IsAny<Func<IReadOnlySet<long>, IEnumerable<long>>?>()), Times.Never);

    [Fact]
    public async Task UpdateStatoZonaAsync_Disattivare_ControllaLaSalaSenzaLaZona()
    {
        _repoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(new Zona { Id = 2, Nome = "Dehors", Attiva = true });
        Func<IReadOnlySet<long>, IEnumerable<long>>? zoneDopo = null;
        _coerenzaSala.Setup(c => c.VerificaModificaSalaAsync(It.IsAny<Func<IReadOnlyList<TavoloSala>, IEnumerable<TavoloSala>>>(),
                                                              It.IsAny<Func<IReadOnlySet<long>, IEnumerable<long>>?>()))
                     .Callback<Func<IReadOnlyList<TavoloSala>, IEnumerable<TavoloSala>>, Func<IReadOnlySet<long>, IEnumerable<long>>?>((_, z) => zoneDopo = z)
                     .Returns(Task.CompletedTask);

        await _service.UpdateStatoZonaAsync(2, false);

        Assert.NotNull(zoneDopo);
        Assert.Equal(new long[] { 1, 3 }, zoneDopo!(new HashSet<long> { 1, 2, 3 }).OrderBy(x => x));
    }

    [Fact]
    public async Task UpdateStatoZonaAsync_Disattivare_SottoIlTetto_NonSalva()
    {
        _repoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(new Zona { Id = 2, Nome = "Dehors", Attiva = true });
        SalaSottoIlTetto();

        await Assert.ThrowsAsync<ConflictException>(() => _service.UpdateStatoZonaAsync(2, false));
        _repoMock.Verify(r => r.UpdateStatoZonaAsync(It.IsAny<long>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task UpdateStatoZonaAsync_Riattivare_NessunControllo()
    {
        _repoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(new Zona { Id = 2, Nome = "Dehors", Attiva = false });

        await _service.UpdateStatoZonaAsync(2, true);

        NessunControllo();
    }

    [Fact]
    public async Task UpdateAsync_DisattivaLaZona_SottoIlTetto_NonSalva()
    {
        _repoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(new Zona { Id = 2, Nome = "Dehors", Attiva = true });
        _repoMock.Setup(r => r.GetByNameAsync("Dehors")).ReturnsAsync(new Zona { Id = 2, Nome = "Dehors", Attiva = true });
        SalaSottoIlTetto();

        await Assert.ThrowsAsync<ConflictException>(() =>
            _service.UpdateAsync(new ZonaDTO { Id = 2, Nome = "Dehors", Attiva = false }));
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Zona>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_SoloNome_NessunControllo()
    {
        _repoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(new Zona { Id = 2, Nome = "Dehors", Attiva = true });
        _repoMock.Setup(r => r.GetByNameAsync("Terrazza")).ReturnsAsync((Zona?)null);

        await _service.UpdateAsync(new ZonaDTO { Id = 2, Nome = "Terrazza", Attiva = true });

        NessunControllo();
    }
}
