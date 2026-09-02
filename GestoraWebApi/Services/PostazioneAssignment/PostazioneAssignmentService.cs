using GestoraWebApi.Enums;
using GestoraWebApi.Models;
using GestoraWebApi.Repositories.Postazioni;
using GestoraWebApi.Repositories.Prenotazioni;
using GestoraWebApi.Repositories.Zone;
using GestoraWebApi.Services.Prenotazioni.DTOs;
using Microsoft.EntityFrameworkCore;
using GestoraWebApi.Infrastructure.Exceptions;

namespace GestoraWebApi.Services.PostazioneAssignment
{
    public class PostazioneAssignmentService : IPostazioneAssignmentService
    {
        private readonly IPostazioneRepository _postazioneRepository;
        private readonly IPrenotazioniRepository _prenotazioniRepository;
        private readonly IZonaRepository _zonaRepository;

        public PostazioneAssignmentService(IPostazioneRepository postazioneRepository,
                                           IPrenotazioniRepository prenotazioniRepository,
                                           IZonaRepository zonaRepository)
        {
            _postazioneRepository = postazioneRepository;
            _prenotazioniRepository = prenotazioniRepository;
            _zonaRepository = zonaRepository;
        }

        public async Task<List<PostazioneAssegnata>> AssegnaPostazioneDisponibileAsync(PrenotazioneCreateDTO dto, long? excludePrenotazioneId = null)
        {
            // REV-024: escludi i tavoli che appartengono a zone disattivate.
            var zoneAttiveIds = (await _zonaRepository.GetAllZoneAttiveAsync()).Select(z => z.Id).ToHashSet();
            var postazioni = (await _postazioneRepository.GetPostazioniAttiveAsync())
                .Where(p => zoneAttiveIds.Contains(p.ZonaId))
                .ToList();

            if (dto.ZonaId.HasValue)
                postazioni = postazioni.Where(p => p.ZonaId == dto.ZonaId.Value).ToList();

            if (!postazioni.Any())
            {
                if (dto.ZonaId.HasValue)
                    throw new ConflictException("Non ci sono postazioni libere nella zona preferita selezionata.");
                else
                    throw new ConflictException("Non ci sono postazioni libere.");
            }

            var postazioniOccupateIds = await _prenotazioniRepository.GetAllQueryableAsync()
                .Where(pr =>
                    pr.DataPrenotazione == dto.DataPrenotazione &&
                    pr.FasciaOrariaId == dto.FasciaOrariaId &&
                    pr.Stato != StatoPrenotazione.Annullata &&
                    (!excludePrenotazioneId.HasValue || pr.Id != excludePrenotazioneId.Value))
                .SelectMany(pr => pr.PrenotazioniPostazioni.Select(pp => pp.PostazioneId))
                .ToListAsync();

            var postazioniLibere = postazioni
                .Where(p => !postazioniOccupateIds.Contains(p.Id))
                .ToList();

            if (!postazioniLibere.Any())
                throw new ConflictException("Non ci sono postazioni libere per la fascia oraria selezionata.");

            var migliore = AssegnazioneTavoli.TrovaMigliorCombinazione(postazioniLibere, dto.NumeroCoperti);

            if (migliore == null)
                throw new ConflictException("Non ci sono postazioni libere o attive per i coperti richiesti nella fascia oraria selezionata.");

            var distribuzione = AssegnazioneTavoli.DistribuisciCoperti(migliore, dto.NumeroCoperti);

            return migliore
                .Select(p => new PostazioneAssegnata(p, distribuzione[p.Id]))
                .ToList();
        }
    }
}
