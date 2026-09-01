using AutoMapper;
using GestoraWebApi.Common;
using GestoraWebApi.Models;
using GestoraWebApi.Repositories.FasciaOrarie;
using GestoraWebApi.Repositories.Postazioni;
using GestoraWebApi.Repositories.Zone;
using GestoraWebApi.Services.LogActivity;
using GestoraWebApi.Services.Postazioni;
using GestoraWebApi.Services.Postazioni.DTOs;
using MockQueryable.Moq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using System.Security.Claims;

namespace GestoraWebApi.Tests.Services;

public class PostazioneServiceTests
{
    private readonly Mock<IPostazioneRepository> _postazioneRepoMock;
    private readonly Mock<IZonaRepository> _zonaRepoMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly IMemoryCache _cache;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<ILogActivityService> _logActivityMock;
    private readonly Mock<IFasciaOrariaRepository> _fasciaRepoMock;
    private readonly PostazioneService _service;

    public PostazioneServiceTests()
    {
        _postazioneRepoMock = new Mock<IPostazioneRepository>();
        _zonaRepoMock = new Mock<IZonaRepository>();
        _mapperMock = new Mock<IMapper>();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "test-user-id") }))
        });
        _logActivityMock = new Mock<ILogActivityService>();
        _fasciaRepoMock = new Mock<IFasciaOrariaRepository>();
        _service = new PostazioneService(_postazioneRepoMock.Object, _mapperMock.Object, _zonaRepoMock.Object, _cache,
                                          _httpContextAccessorMock.Object, _logActivityMock.Object, _fasciaRepoMock.Object);
    }

    // CACHE-001: AssociaPostazioneAZonaAsync cambia ZonaId ma non invalidava la cache
    // PostazioniAttive, lasciando la vecchia ZonaId servita dalla cache per 30 minuti.
    [Fact]
    public async Task AssociaPostazioneAZonaAsync_InvalidatesPostazioniAttiveCache()
    {
        // Arrange
        var postazione = new Postazione { Id = 1, Attiva = true, ZonaId = 10 };
        var nuovaZona = new Zona { Id = 20, Nome = "Terrazza", Attiva = true };

        _zonaRepoMock.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(nuovaZona);
        _postazioneRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(postazione);
        _postazioneRepoMock.Setup(r => r.HasPrenotazioniAsync(1)).ReturnsAsync(false);

        _cache.Set(CacheKeys.PostazioniAttive, new List<PostazioneDTO> { new PostazioneDTO() });

        // Act
        await _service.AssociaPostazioneAZonaAsync(1, 20);

        // Assert
        Assert.False(_cache.TryGetValue(CacheKeys.PostazioniAttive, out _));
    }

    // FIX-001: UpdateAsync(PostazioneUpdateDTO) non validava l'esistenza della zona,
    // lasciando che una ZonaId inesistente esplodesse in un DbUpdateException tecnico.
    [Fact]
    public async Task UpdateAsync_ThrowsArgumentException_WhenZonaNonEsiste()
    {
        // Arrange
        var postazione = new Postazione { Id = 1, Numero = 5, Attiva = true, ZonaId = 10 };
        var dto = new PostazioneUpdateDTO { Id = 1, Numero = 5, CapienzaMassima = 4, ZonaId = 999 };

        _postazioneRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(postazione);
        _postazioneRepoMock.Setup(r => r.HasPrenotazioniAsync(1)).ReturnsAsync(false);
        _postazioneRepoMock.Setup(r => r.GetAllQueryable())
                            .Returns(new List<Postazione>().AsQueryable().BuildMockDbSet().Object);
        _zonaRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Zona?)null);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateAsync(dto));
        _postazioneRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Postazione>()), Times.Never);
    }

    // ─── GetRiepilogoSalaAsync — decisione 9 (riepilogo sala) ─────────────────

    [Fact]
    public async Task GetRiepilogoSalaAsync_SommaTavoliInZoneAttive_ESegnalaCoperturaTettoPerFascia()
    {
        _zonaRepoMock.Setup(r => r.GetAllZoneAttiveAsync())
                     .ReturnsAsync(new List<Zona> { new() { Id = 1, Nome = "Sala", Attiva = true } });
        _postazioneRepoMock.Setup(r => r.GetPostazioniAttiveAsync())
                           .ReturnsAsync(new List<Postazione>
                           {
                               new() { Id = 1, Numero = 1, CapienzaMassima = 4, Attiva = true, ZonaId = 1 },
                               new() { Id = 2, Numero = 2, CapienzaMassima = 6, Attiva = true, ZonaId = 1 },
                               new() { Id = 3, Numero = 3, CapienzaMassima = 2, Attiva = true, ZonaId = 2 } // zona non attiva
                           });
        _fasciaRepoMock.Setup(r => r.GetFasceAttiveAsync())
                       .ReturnsAsync(new List<FasciaOraria>
                       {
                           new() { Id = 10, GiornoSettimana = DayOfWeek.Monday, OrarioInizio = new TimeOnly(12, 0), OrarioFine = new TimeOnly(15, 0), MaxCoperti = 8, Attiva = true },
                           new() { Id = 11, GiornoSettimana = DayOfWeek.Monday, OrarioInizio = new TimeOnly(19, 0), OrarioFine = new TimeOnly(23, 0), MaxCoperti = 20, Attiva = true }
                       });

        var riepilogo = await _service.GetRiepilogoSalaAsync();

        Assert.Equal(2, riepilogo.TavoliAttivi);   // il tavolo in zona non attiva è escluso
        Assert.Equal(10, riepilogo.PostiTotali);   // 4 + 6
        Assert.True(riepilogo.Fasce.Single(f => f.FasciaOrariaId == 10).TettoCoperto);   // 10 >= 8
        Assert.False(riepilogo.Fasce.Single(f => f.FasciaOrariaId == 11).TettoCoperto);  // 10 < 20
    }
}
