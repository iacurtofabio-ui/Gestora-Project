using GestoraWebApi.Enums;
using GestoraWebApi.Models;
using GestoraWebApi.Repositories.Postazioni;
using GestoraWebApi.Repositories.Prenotazioni;
using GestoraWebApi.Services.Prenotazioni.DTOs;
using Microsoft.EntityFrameworkCore;

namespace GestoraWebApi.Services.PostazioneAssignment
{
    public class PostazioneAssignmentService : IPostazioneAssignmentService
    {
        private readonly IPostazioneRepository _postazioneRepository;
        private readonly IPrenotazioniRepository _prenotazioniRepository;

        public PostazioneAssignmentService(IPostazioneRepository postazioneRepository,
                                           IPrenotazioniRepository prenotazioniRepository)
        {
            _postazioneRepository = postazioneRepository;
            _prenotazioniRepository = prenotazioniRepository;
        }

        public async Task<List<PostazioneAssegnata>> AssegnaPostazioneDisponibileAsync(PrenotazioneCreateDTO dto, long? excludePrenotazioneId = null)
        {
            var postazioni = await _postazioneRepository.GetPostazioniAttiveAsync();

            if (dto.ZonaId.HasValue)
                postazioni = postazioni.Where(p => p.ZonaId == dto.ZonaId.Value).ToList();

            if (!postazioni.Any())
            {
                if (dto.ZonaId.HasValue)
                    throw new InvalidOperationException("Non ci sono postazioni libere nella zona preferita selezionata.");
                else
                    throw new InvalidOperationException("Non ci sono postazioni libere.");
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
                throw new InvalidOperationException("Non ci sono postazioni libere per la fascia oraria selezionata.");

            var migliore = AssegnazioneTavoli.TrovaMigliorCombinazione(postazioniLibere, dto.NumeroCoperti);

            if (migliore == null)
                throw new InvalidOperationException("Non ci sono postazioni libere o attive per i coperti richiesti nella fascia oraria selezionata.");

            var distribuzione = AssegnazioneTavoli.DistribuisciCoperti(migliore, dto.NumeroCoperti);

            return migliore
                .Select(p => new PostazioneAssegnata(p, distribuzione[p.Id]))
                .ToList();
        }

        public List<List<Postazione>> TrovaCombinazioniDisponibili(List<Postazione> postazioniLibere, int numeroCoperti)
        {
            var migliore = AssegnazioneTavoli.TrovaMigliorCombinazione(postazioniLibere, numeroCoperti);

            return migliore == null
                ? new List<List<Postazione>>()
                : new List<List<Postazione>> { migliore };
        }
    }
}
