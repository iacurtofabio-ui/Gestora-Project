using AutoMapper;
using GestoraWebApi.Common;
using GestoraWebApi.Enums;
using GestoraWebApi.Models;
using GestoraWebApi.Repositories.FasciaOrarie;
using GestoraWebApi.Repositories.Prenotazioni;
using GestoraWebApi.Services.FasciaOrarie.DTOs;
using GestoraWebApi.Services.Postazioni.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace GestoraWebApi.Services.FasciaOrarie
{
    public class FasciaOrariaService : IFasciaOrariaService
    {
        private readonly IFasciaOrariaRepository _fasciaRepository;
        private readonly IMapper _mapper;
        private readonly IMemoryCache _cache;

        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

        public FasciaOrariaService(IFasciaOrariaRepository fasciaRepository, IMapper mapper, IMemoryCache cache)
        {
            _fasciaRepository = fasciaRepository;
            _mapper = mapper;
            _cache = cache;
        }
        public async Task AddAsync(FasciaOrariaDTO dto)
        {
            var orarioInizio = ParseOrario(dto.OrarioInizio, nameof(dto.OrarioInizio));
            var orarioFine = ParseOrario(dto.OrarioFine, nameof(dto.OrarioFine));

            await GuardSovrapposizioneAsync(dto.Id, dto.GiornoSettimana, orarioInizio, orarioFine);

            var fascia = new FasciaOraria
            {
                Id = dto.Id,
                GiornoSettimana = dto.GiornoSettimana,
                MaxPrenotazioni = dto.MaxPrenotazioni,
                Attiva = dto.Attiva,
                OrarioInizio = TimeOnly.FromTimeSpan(orarioInizio),
                OrarioFine = TimeOnly.FromTimeSpan(orarioFine)
            };

            await _fasciaRepository.AddAsync(fascia);

            _cache.Remove(CacheKeys.FasceAttive);
            _cache.Remove(CacheKeys.FascePerGiorno + (int)dto.GiornoSettimana);
        }

        /// <summary>
        /// Controlla che (inizio, fine) su un dato giorno non si sovrapponga a nessuna fascia
        /// esistente, attiva o disattivata. Va incluse anche le disattivate: altrimenti una
        /// fascia disattivata sovrapposta può essere riattivata più tardi senza controlli
        /// (problema A) e produrre due fasce attive sovrapposte (problema B).
        /// </summary>
        private async Task GuardSovrapposizioneAsync(long idDaEscludere, DayOfWeek giorno, TimeSpan inizio, TimeSpan fine)
        {
            var sovrapposta = await _fasciaRepository
                .GetAllQueryable()
                .AnyAsync(f =>
                    f.Id != idDaEscludere &&
                    f.GiornoSettimana == giorno &&
                    (
                        inizio < f.OrarioFine.ToTimeSpan() &&
                        fine > f.OrarioInizio.ToTimeSpan()
                    )
                );

            if (sovrapposta)
            {
                throw new InvalidOperationException("Operazione non riuscita! La fascia oraria si sovrappone ad una già esistente.");
            }
        }

        private static TimeSpan ParseOrario(string orario, string nomeCampo)
        {
            if (!TimeSpan.TryParse(orario, out var risultato))
            {
                throw new ArgumentException($"Il campo '{nomeCampo}' non è un orario valido: '{orario}'.");
            }

            return risultato;
        }

        public async Task<DisponibilitaFasciaDTO> VerificaDisponibilitaPerFasciaAsync(long fasciaId, DateOnly data)
        {
            var fascia = await _fasciaRepository.GetByIdAsync(fasciaId);

            if (fascia == null || !fascia.Attiva)
                throw new KeyNotFoundException("Fascia oraria non trovata o non attiva.");

            if (fascia.GiornoSettimana != data.DayOfWeek)
                throw new ArgumentException(
                    $"La data {data:dd/MM/yyyy} è un {data.DayOfWeek}, " +
                    $"ma la fascia selezionata è attiva il {fascia.GiornoSettimana}.");

            var copertiOccupati = await _fasciaRepository.CountNumeroCopertiFasciaOrariaAsync(fasciaId, data);

            var postiDisponibili = fascia.MaxPrenotazioni - copertiOccupati;

            return new DisponibilitaFasciaDTO
            {
                FasciaId = fascia.Id,
                PostiTotali = fascia.MaxPrenotazioni,
                PostiOccupati = copertiOccupati,
                PostiDisponibili = postiDisponibili < 0 ? 0 : postiDisponibili,
                Disponibile = postiDisponibili > 0
            };
        }

        public async Task DeleteAsync(long fasciaId)
        {
            var fascia = await _fasciaRepository.GetByIdAsync(fasciaId);

            if (fascia == null)
                throw new InvalidOperationException($"Fascia oraria con id {fasciaId} non esiste.");

            bool fasciaUsata = await _fasciaRepository.IsFasciaUsataAsync(fasciaId);

            if (fasciaUsata)
                throw new InvalidOperationException("Impossibile eliminare la fascia perchè associata almeno ad una prenotazione!");

            await _fasciaRepository.DeleteAsync(fascia);

            _cache.Remove(CacheKeys.FasceAttive);
        }

        public async Task<FasciaOraria> GetByIdAsync(long id)
        {
            return await _fasciaRepository
                           .GetAllQueryable()
                           .FirstOrDefaultAsync(f => f.Id == id);
        }

        public async Task<List<FasciaOrariaDTO>> GetFasceAttiveAsync()
        {
            if (_cache.TryGetValue(CacheKeys.FasceAttive, out List<FasciaOrariaDTO>? cached))
                return cached!;

            var fasceAttive = await _fasciaRepository.GetFasceAttiveAsync();

            var result = fasceAttive.Select(f => new FasciaOrariaDTO
            {
                Id = f.Id,
                GiornoSettimana = f.GiornoSettimana,
                MaxPrenotazioni = f.MaxPrenotazioni,
                Attiva = f.Attiva,
                OrarioInizio = f.OrarioInizio.ToTimeSpan().ToString(@"hh\:mm"),
                OrarioFine = f.OrarioFine.ToTimeSpan().ToString(@"hh\:mm")
            }).ToList();

            _cache.Set(CacheKeys.FasceAttive, result, CacheDuration);

            return result;
        }

        public async Task<List<FasciaOrariaDTO>> GetFasceByGiornoAsync(DayOfWeek giorno)
        {
            var cacheKey = CacheKeys.FascePerGiorno + (int)giorno;

            if (_cache.TryGetValue(cacheKey, out List<FasciaOrariaDTO>? cached))
                return cached!;

            var fasce = await _fasciaRepository.GetFasceByGiornoAsync(giorno);

            var result = fasce.Select(f => new FasciaOrariaDTO
            {
                Id = f.Id,
                GiornoSettimana = f.GiornoSettimana,
                MaxPrenotazioni = f.MaxPrenotazioni,
                Attiva = f.Attiva,
                OrarioInizio = f.OrarioInizio.ToTimeSpan().ToString(@"hh\:mm"),
                OrarioFine = f.OrarioFine.ToTimeSpan().ToString(@"hh\:mm")
            }).ToList();

            _cache.Set(cacheKey, result, CacheDuration);

            return result;
        }

        public async Task<FasciaOrariaDTO> GetFasciaOrariaDTOByIdAsync(long id)
        {
            var fascia = await _fasciaRepository
                            .GetAllQueryable()
                            .FirstOrDefaultAsync(f => f.Id == id);

            if (fascia == null)
            {
                return null;
            }

            return new FasciaOrariaDTO
            {
                //usare _mapper per convertire FasciaOraria con FasciaOrariaDTO
                Id = fascia.Id,
                GiornoSettimana = fascia.GiornoSettimana,
                MaxPrenotazioni = fascia.MaxPrenotazioni,
                Attiva = fascia.Attiva,
                OrarioInizio = fascia.OrarioInizio.ToTimeSpan().ToString(@"hh\:mm"),
                OrarioFine = fascia.OrarioFine.ToTimeSpan().ToString(@"hh\:mm")
            };
        }

        public async Task UpdateAsync(FasciaOrariaDTO dto)
        {
            var orarioInizio = ParseOrario(dto.OrarioInizio, nameof(dto.OrarioInizio));
            var orarioFine = ParseOrario(dto.OrarioFine, nameof(dto.OrarioFine));

            // Recupera la fascia esistente
            var existing = await _fasciaRepository.GetByIdAsync(dto.Id);
            if (existing == null)
            {
                throw new KeyNotFoundException($"Fascia oraria con ID {dto.Id} non trovata.");
            }

            // Verifica se la fascia è già assegnata a una prenotazione: in tal caso non è modificabile
            var assegnata = await _fasciaRepository.IsAssignedToPrenotazioneAsync(dto.Id);
            if (assegnata)
            {
                throw new InvalidOperationException("Impossibile modificare la fascia: è già assegnata a una prenotazione.");
            }

            await GuardSovrapposizioneAsync(dto.Id, dto.GiornoSettimana, orarioInizio, orarioFine);

            var giornoPrecedente = existing.GiornoSettimana;

            // Applico le modifiche sull'entità esistente
            existing.GiornoSettimana = dto.GiornoSettimana;
            existing.MaxPrenotazioni = dto.MaxPrenotazioni;
            existing.Attiva = dto.Attiva;
            existing.OrarioInizio = TimeOnly.FromTimeSpan(orarioInizio);
            existing.OrarioFine = TimeOnly.FromTimeSpan(orarioFine);

            await _fasciaRepository.UpdateAsync(existing);

            _cache.Remove(CacheKeys.FasceAttive);
            _cache.Remove(CacheKeys.FascePerGiorno + (int)giornoPrecedente);
            if (giornoPrecedente != dto.GiornoSettimana)
            {
                _cache.Remove(CacheKeys.FascePerGiorno + (int)dto.GiornoSettimana);
            }
        }

        Task<FasciaOrariaDTO> IService<FasciaOrariaDTO>.GetByIdAsync(long id)
        {
            throw new NotImplementedException();
        }

        public async Task<List<FasciaOrariaDTO>> GetAllFasceAsync()
        {

            var fasce = await _fasciaRepository.GetAllFasceAsync();

            var result = fasce.Select(f => new FasciaOrariaDTO
            {
                Id = f.Id,
                GiornoSettimana = f.GiornoSettimana,
                MaxPrenotazioni = f.MaxPrenotazioni,
                Attiva = f.Attiva,
                OrarioInizio = f.OrarioInizio.ToTimeSpan().ToString(@"hh\:mm"),
                OrarioFine = f.OrarioFine.ToTimeSpan().ToString(@"hh\:mm")
            }).ToList();

            return result;
        }

        public async Task UpdateStatoAsync(long id, bool attiva)
        {
            var fascia = await _fasciaRepository.GetByIdAsync(id);

            if (fascia == null)
                throw new KeyNotFoundException($"Fascia oraria con ID {id} non trovata.");

            if (attiva)
            {
                await GuardSovrapposizioneAsync(id, fascia.GiornoSettimana,
                    fascia.OrarioInizio.ToTimeSpan(), fascia.OrarioFine.ToTimeSpan());
            }

            fascia.Attiva = attiva;

            await _fasciaRepository.UpdateAsync(fascia);

            _cache.Remove(CacheKeys.FasceAttive);
            _cache.Remove(CacheKeys.FascePerGiorno + (int)fascia.GiornoSettimana);
        }
    }
}