using AutoMapper;
using GestoraWebApi.Models;
using GestoraWebApi.Repositories.FasciaOrarie;
using GestoraWebApi.Services.FasciaOrarie;
using GestoraWebApi.Services.FasciaOrarie.DTOs;
using GestoraWebApi.Services.LogActivity;
using MockQueryable.Moq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace GestoraWebApi.Tests.Services
{
    public class FasciaOrariaServiceTests
    {
        private readonly Mock<IFasciaOrariaRepository> _repoMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly IMemoryCache _cache;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly Mock<ILogActivityService> _logActivityMock;
        private readonly FasciaOrariaService _service;

        public FasciaOrariaServiceTests()
        {
            _repoMock = new Mock<IFasciaOrariaRepository>();
            _mapperMock = new Mock<IMapper>();
            _cache = new MemoryCache(new MemoryCacheOptions());
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "test-user-id") }))
            });
            _logActivityMock = new Mock<ILogActivityService>();
            _service = new FasciaOrariaService(_repoMock.Object,
            _mapperMock.Object, _cache, _httpContextAccessorMock.Object, _logActivityMock.Object);
        }

        [Fact]
        public async Task GetAllFasceAsync_ReturnsAllFasce_RegardlessOfStatoAttiva()
        {
            // Arrange
            var fasce = new List<FasciaOraria>
                {
                    new() { Id = 1, Attiva = true,  GiornoSettimana = DayOfWeek.Monday,
                                                    OrarioInizio = new TimeOnly(12, 0),
                                                    OrarioFine = new TimeOnly(14, 0),
                                                    MaxPrenotazioni = 10 },

                    new() { Id = 2, Attiva = false, GiornoSettimana = DayOfWeek.Tuesday,
                                                    OrarioInizio = new TimeOnly(19, 0),
                                                    OrarioFine = new TimeOnly(21, 0),
                                                    MaxPrenotazioni = 8 }
                };

            _repoMock
                  .Setup(r => r.GetAllFasceAsync())
                  .ReturnsAsync(fasce);
            // Act
            var result = await _service.GetAllFasceAsync();

            // Assert
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task UpdateStatoAsync_ThrowsKeyNotFoundException_WhenFasciaNotFound()
        {
            // Arrange
            _repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((FasciaOraria?)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.UpdateStatoAsync(99, true));
        }

        [Fact]
        public async Task UpdateStatoAsync_SetsAttivaCorrectly_WhenFasciaExists()
        {
            // Arrange
            var fascia = new FasciaOraria { Id = 1, Attiva = true, GiornoSettimana = DayOfWeek.Monday,
                                             OrarioInizio = new TimeOnly(12, 0), OrarioFine = new TimeOnly(14, 0) };
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(fascia);

            // Act
            await _service.UpdateStatoAsync(1, false);

            // Assert
            Assert.False(fascia.Attiva);
            _repoMock.Verify(r => r.UpdateAsync(fascia), Times.Once);
        }

        // --- FIX-004 A: UpdateStatoAsync deve controllare la sovrapposizione quando riattiva ---

        [Fact]
        public async Task UpdateStatoAsync_ThrowsInvalidOperationException_WhenReactivatingOverlapsAttiva()
        {
            // Arrange: fascia disattivata 12:00-14:00 lunedì, si sovrappone a una attiva 13:00-15:00
            var fasciaDaRiattivare = new FasciaOraria { Id = 1, Attiva = false, GiornoSettimana = DayOfWeek.Monday,
                                                          OrarioInizio = new TimeOnly(12, 0), OrarioFine = new TimeOnly(14, 0) };
            var fasciaAttivaEsistente = new FasciaOraria { Id = 2, Attiva = true, GiornoSettimana = DayOfWeek.Monday,
                                                             OrarioInizio = new TimeOnly(13, 0), OrarioFine = new TimeOnly(15, 0) };

            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(fasciaDaRiattivare);
            _repoMock.Setup(r => r.GetAllQueryable())
                     .Returns(new List<FasciaOraria> { fasciaDaRiattivare, fasciaAttivaEsistente }.AsQueryable().BuildMockDbSet().Object);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateStatoAsync(1, true));
            _repoMock.Verify(r => r.UpdateAsync(It.IsAny<FasciaOraria>()), Times.Never);
        }

        // --- FIX-004 B: la sovrapposizione va controllata anche contro fasce disattivate ---

        [Fact]
        public async Task AddAsync_ThrowsInvalidOperationException_WhenOverlapsFasciaDisattivata()
        {
            // Arrange: fascia disattivata 12:00-14:00 lunedì già presente
            var fasciaDisattivata = new FasciaOraria { Id = 1, Attiva = false, GiornoSettimana = DayOfWeek.Monday,
                                                         OrarioInizio = new TimeOnly(12, 0), OrarioFine = new TimeOnly(14, 0) };

            _repoMock.Setup(r => r.GetAllQueryable())
                     .Returns(new List<FasciaOraria> { fasciaDisattivata }.AsQueryable().BuildMockDbSet().Object);

            var dto = new FasciaOrariaDTO
            {
                GiornoSettimana = DayOfWeek.Monday,
                OrarioInizio = "13:00",
                OrarioFine = "15:00",
                MaxPrenotazioni = 10,
                Attiva = true
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.AddAsync(dto));
            _repoMock.Verify(r => r.AddAsync(It.IsAny<FasciaOraria>()), Times.Never);
        }

        // --- FIX-004 C: un orario non parsabile deve fallire in modo esplicito, non salvare 00:00 ---

        [Fact]
        public async Task AddAsync_ThrowsArgumentException_WhenOrarioInizioNonValido()
        {
            // Arrange
            _repoMock.Setup(r => r.GetAllQueryable())
                     .Returns(new List<FasciaOraria>().AsQueryable().BuildMockDbSet().Object);

            var dto = new FasciaOrariaDTO
            {
                GiornoSettimana = DayOfWeek.Monday,
                OrarioInizio = "non-un-orario",
                OrarioFine = "15:00",
                MaxPrenotazioni = 10,
                Attiva = true
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.AddAsync(dto));
            _repoMock.Verify(r => r.AddAsync(It.IsAny<FasciaOraria>()), Times.Never);
        }

        // --- CACHE-001: le scritture devono invalidare anche la cache per giorno ---

        [Fact]
        public async Task AddAsync_InvalidatesCachePerGiorno_AfterInsert()
        {
            // Arrange
            _repoMock.Setup(r => r.GetAllQueryable())
                     .Returns(new List<FasciaOraria>().AsQueryable().BuildMockDbSet().Object);

            var cacheKey = GestoraWebApi.Common.CacheKeys.FascePerGiorno + (int)DayOfWeek.Monday;
            _cache.Set(cacheKey, new List<FasciaOrariaDTO> { new FasciaOrariaDTO() });

            var dto = new FasciaOrariaDTO
            {
                GiornoSettimana = DayOfWeek.Monday,
                OrarioInizio = "12:00",
                OrarioFine = "14:00",
                MaxPrenotazioni = 10,
                Attiva = true
            };

            // Act
            await _service.AddAsync(dto);

            // Assert
            Assert.False(_cache.TryGetValue(cacheKey, out _));
        }

        [Fact]
        public async Task UpdateStatoAsync_InvalidatesCachePerGiorno_AfterUpdate()
        {
            // Arrange
            var fascia = new FasciaOraria { Id = 1, Attiva = true, GiornoSettimana = DayOfWeek.Tuesday,
                                             OrarioInizio = new TimeOnly(9, 0), OrarioFine = new TimeOnly(10, 0) };
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(fascia);

            var cacheKey = GestoraWebApi.Common.CacheKeys.FascePerGiorno + (int)DayOfWeek.Tuesday;
            _cache.Set(cacheKey, new List<FasciaOrariaDTO> { new FasciaOrariaDTO() });

            // Act
            await _service.UpdateStatoAsync(1, false);

            // Assert
            Assert.False(_cache.TryGetValue(cacheKey, out _));
        }
    }
}
