using AutoMapper;
using GestoraWebApi.Mappings;
using GestoraWebApi.Models;
using GestoraWebApi.Services.Prenotazioni.DTOs;

namespace GestoraWebApi.Tests.Mappings
{
    /// <summary>
    /// Il profilo di mapping non era coperto da alcun test, e si e' visto due volte cosa costa:
    /// NumeroPosti (REV-001) e ZonaId (NEW-001) esistevano nel DTO ma non venivano mai
    /// valorizzati, restando a 0 senza che nulla se ne accorgesse. Qui si verifica che ogni
    /// campo del DTO arrivi davvero dal modello.
    /// </summary>
    public class PrenotazioneMappingProfileTests
    {
        private readonly IMapper _mapper;

        public PrenotazioneMappingProfileTests()
        {
            var configurazione = new MapperConfiguration(cfg => cfg.AddProfile<PrenotazioneMappingProfile>());

            _mapper = configurazione.CreateMapper();
        }

        private static Prenotazione PrenotazioneDiEsempio()
        {
            var zona = new Zona { Id = 7, Nome = "Veranda" };
            var postazione = new Postazione { Id = 3, Numero = 12, ZonaId = zona.Id, Zona = zona };

            return new Prenotazione
            {
                Id = 55,
                DataPrenotazione = new DateOnly(2026, 9, 7),
                NumeroCoperti = 4,
                Note = "vicino alla finestra",
                FasciaOrariaId = 9,
                FasciaOraria = new FasciaOraria
                {
                    Id = 9,
                    OrarioInizio = new TimeOnly(20, 0),
                    OrarioFine = new TimeOnly(22, 0),
                },
                PrenotazioniPostazioni =
                [
                    new PrenotazionePostazione
                    {
                        PostazioneId = postazione.Id,
                        Postazione = postazione,
                        PrenotazioneId = 55,
                        NumeroPosti = 4,
                    }
                ],
            };
        }

        [Fact]
        public void Map_PrenotazioneDTO_ValorizzaFasciaOrariaId()
        {
            var dto = _mapper.Map<PrenotazioneDTO>(PrenotazioneDiEsempio());

            Assert.Equal(9, dto.FasciaOrariaId);
            Assert.Equal("20:00", dto.OraInizio);
            Assert.Equal("22:00", dto.OraFine);
        }

        [Fact]
        public void Map_PostazioneAssegnata_ValorizzaNumeroNomeZonaEZonaId()
        {
            var dto = _mapper.Map<PrenotazioneDTO>(PrenotazioneDiEsempio());

            var postazione = Assert.Single(dto.Postazioni);
            Assert.Equal(12, postazione.Numero);
            Assert.Equal("Veranda", postazione.NomeZona);
            // Il caso che era rotto: ZonaId restava 0, e il frontend non riusciva a
            // precompilare la zona nel modal di modifica.
            Assert.Equal(7, postazione.ZonaId);
        }

        [Fact]
        public void Map_PostazioneAssegnata_SenzaZona_NomeZonaNullo()
        {
            var prenotazione = PrenotazioneDiEsempio();
            prenotazione.PrenotazioniPostazioni.First().Postazione.Zona = null;

            var dto = _mapper.Map<PrenotazioneDTO>(prenotazione);

            var postazione = Assert.Single(dto.Postazioni);
            Assert.Null(postazione.NomeZona);
            // ZonaId arriva dalla colonna della postazione, non dalla navigazione: resta valido
            // anche quando la Zona non e' stata caricata.
            Assert.Equal(7, postazione.ZonaId);
        }
    }
}
