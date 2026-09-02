using AutoMapper;
using GestoraWebApi.Models;
using GestoraWebApi.Repositories.Zone;
using GestoraWebApi.Services.LogActivity;
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
        _service     = new ZonaService(_repoMock.Object, _mapperMock.Object, _cache,
                                        _httpContextAccessorMock.Object, _logActivityMock.Object);
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
}
