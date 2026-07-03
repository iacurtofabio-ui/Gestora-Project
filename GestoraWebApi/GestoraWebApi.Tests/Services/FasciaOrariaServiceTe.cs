using AutoMapper;
using GestoraWebApi.Models;
using GestoraWebApi.Repositories.FasciaOrarie;
using GestoraWebApi.Services.FasciaOrarie;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestoraWebApi.Tests.Services
{
    public class FasciaOrariaServiceTests
    {
        private readonly Mock<IFasciaOrariaRepository> _repoMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly IMemoryCache _cache;
        private readonly FasciaOrariaService _service;

        public FasciaOrariaServiceTests()
        {
            _repoMock = new Mock<IFasciaOrariaRepository>();
            _mapperMock = new Mock<IMapper>();
            _cache = new MemoryCache(new MemoryCacheOptions());
            _service = new FasciaOrariaService(_repoMock.Object,
            _mapperMock.Object, _cache);
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
            var fascia = new FasciaOraria { Id = 1, Attiva = true };
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(fascia);

            // Act
            await _service.UpdateStatoAsync(1, false);

            // Assert
            Assert.False(fascia.Attiva);
            _repoMock.Verify(r => r.UpdateAsync(fascia), Times.Once);
        }
    }
}
