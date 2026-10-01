using AutoMapper;
using GestoraWebApi.Mappings;
using GestoraWebApi.Models;
using GestoraWebApi.Services.Postazioni.DTOs;

namespace GestoraWebApi.Tests.Mappings
{
    /// <summary>
    /// La casella "Attiva" del modale di modifica tavolo non arrivava mai al modello: il DTO di
    /// modifica non aveva il campo. L'Admin vedeva "aggiornato con successo" ma il tavolo
    /// restava attivo e continuava a ricevere prenotazioni.
    /// </summary>
    public class PostazioneMappingProfileTests
    {
        private readonly IMapper _mapper;

        public PostazioneMappingProfileTests()
        {
            var configurazione = new MapperConfiguration(cfg => cfg.AddProfile<PostazioneMappingProfile>());

            _mapper = configurazione.CreateMapper();
        }

        [Fact]
        public void UpdateDTO_DisattivaIlTavolo()
        {
            var postazione = new Postazione { Id = 1, Numero = 5, CapienzaMassima = 4, ZonaId = 10, Attiva = true };
            var dto = new PostazioneUpdateDTO { Id = 1, Numero = 5, CapienzaMassima = 4, ZonaId = 10, Attiva = false };

            _mapper.Map(dto, postazione);

            Assert.False(postazione.Attiva);
        }

        [Fact]
        public void UpdateDTO_RiattivaIlTavolo()
        {
            var postazione = new Postazione { Id = 1, Numero = 5, CapienzaMassima = 4, ZonaId = 10, Attiva = false };
            var dto = new PostazioneUpdateDTO { Id = 1, Numero = 5, CapienzaMassima = 4, ZonaId = 10, Attiva = true };

            _mapper.Map(dto, postazione);

            Assert.True(postazione.Attiva);
        }
    }
}
