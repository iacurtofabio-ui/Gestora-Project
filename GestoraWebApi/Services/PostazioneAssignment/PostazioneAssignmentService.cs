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

        public async Task<List<Postazione>> AssegnaPostazioneDisponibileAsync(PrenotazioneCreateDTO dto, long? excludePrenotazioneId = null)
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

            var singolaPostazione = postazioniLibere
                .Where(p => p.CapienzaMassima >= dto.NumeroCoperti)
                .OrderBy(p => p.CapienzaMassima)
                .FirstOrDefault();

            if (singolaPostazione != null)
                return new List<Postazione> { singolaPostazione };

            var candidati = new List<(long ZonaId, List<Postazione> Selezionate, int Surplus)>();

            foreach (var gruppoZona in postazioniLibere.GroupBy(p => p.ZonaId))
            {
                var ordered = gruppoZona.OrderByDescending(p => p.CapienzaMassima).ToList();
                var selezionate = new List<Postazione>();
                int acc = 0;

                foreach (var p in ordered)
                {
                    selezionate.Add(p);
                    acc += p.CapienzaMassima;

                    if (acc >= dto.NumeroCoperti)
                    {
                        candidati.Add((gruppoZona.Key, selezionate, acc - dto.NumeroCoperti));
                        break;
                    }
                }
            }

            if (!candidati.Any())
                throw new InvalidOperationException("Non ci sono postazioni libere o attive per i coperti richiesti nella fascia oraria selezionata.");

            var migliore = candidati
                .OrderBy(c => c.Selezionate.Count)
                .ThenBy(c => c.Surplus)
                .First();

            return migliore.Selezionate;
        }

        public List<List<Postazione>> TrovaCombinazioniDisponibili(List<Postazione> postazioniLibere, int numeroCoperti)
        {
            var combinazioni = new List<List<Postazione>>();

            var singola = postazioniLibere
                .Where(p => p.CapienzaMassima >= numeroCoperti)
                .OrderBy(p => p.CapienzaMassima)
                .FirstOrDefault();

            if (singola != null)
            {
                combinazioni.Add(new List<Postazione> { singola });
                return combinazioni;
            }

            foreach (var gruppoZona in postazioniLibere.GroupBy(p => p.ZonaId))
            {
                var ordered = gruppoZona.OrderByDescending(p => p.CapienzaMassima).ToList();
                var selezionate = new List<Postazione>();
                int acc = 0;

                foreach (var p in ordered)
                {
                    selezionate.Add(p);
                    acc += p.CapienzaMassima;

                    if (acc >= numeroCoperti)
                    {
                        combinazioni.Add(new List<Postazione>(selezionate));
                        break;
                    }
                }
            }

            return combinazioni;
        }
    }
}
