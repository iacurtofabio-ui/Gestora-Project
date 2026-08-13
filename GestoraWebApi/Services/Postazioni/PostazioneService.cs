using AutoMapper;
using GestoraWebApi.Common;
using GestoraWebApi.Enums;
using GestoraWebApi.Models;
using GestoraWebApi.Repositories.Postazioni;
using GestoraWebApi.Repositories.Zone;
using GestoraWebApi.Services.Postazioni.DTOs;
using GestoraWebApi.Services.Prenotazioni.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics.Eventing.Reader;

namespace GestoraWebApi.Services.Postazioni
{
    public class PostazioneService : IPostazioneService
    {
        private readonly IPostazioneRepository _postazioneRepository;
        private readonly IZonaRepository _zonaRepository;
        private readonly IMapper _mapper;
        private readonly IMemoryCache _cache;

        // Valori consentiti per la capienza massima
        private static readonly int[] CapienzaConsentita = { 2, 4, 8 };
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

        public PostazioneService(IPostazioneRepository postazioneRepository, IMapper mapper, IZonaRepository zonaRepository, IMemoryCache cache)
        {
            _postazioneRepository = postazioneRepository;
            _zonaRepository = zonaRepository;
            _mapper = mapper;
            _cache = cache;
        }
        public async Task AddAsync(PostazioneDTO dto)
        {
            await ValidatePostazioneAsync(dto);

            var postazione = _mapper.Map<Postazione>(dto);
            await _postazioneRepository.AddAsync(postazione);

            _cache.Remove(CacheKeys.PostazioniAttive);
        }

        public async Task DeleteAsync(long postazioneId)
        {
            var postazione = await _postazioneRepository.GetByIdAsync(postazioneId);
            if (postazione == null)
                throw new KeyNotFoundException($"Postazione con ID {postazioneId} non trovata.");

            // Controllo se ha prenotazioni associate
            if (postazione.PrenotazioniPostazioni != null && postazione.PrenotazioniPostazioni.Any())
                throw new InvalidOperationException($"Impossibile eliminare la postazione {postazioneId}: ci sono prenotazioni associate.");

            await _postazioneRepository.DeleteAsync(postazione);

            _cache.Remove(CacheKeys.PostazioniAttive);
        }

        public async Task<Postazione> GetByIdAsync(long id)
        {
            return await _postazioneRepository
               .GetAllQueryable()
               .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<PostazioneDTO> GetPostazioneDTOByIdAsync(long id)
        {
            var postazione = await _postazioneRepository
                .GetAllQueryable()
                .Include(p => p.PrenotazioniPostazioni)
                .FirstOrDefaultAsync(p => p.Id == id);

            var dto = _mapper.Map<PostazioneDTO>(postazione);

            // Prende le PrenotazioneId associate
            dto.PrenotazioneId = postazione?.PrenotazioniPostazioni?
                                .Select(pp => pp.PrenotazioneId)
                                .ToList() ?? new List<long>();

            return dto;

        }

        public Task UpdateAsync(PostazioneDTO entity)
        {
            throw new NotImplementedException();
        }

        Task<PostazioneDTO> IService<PostazioneDTO>.GetByIdAsync(long id)
        {
            throw new NotImplementedException();
        }

        private async Task ValidatePostazioneAsync(PostazioneDTO dto)
        {
            if (!dto.Attiva)
                throw new InvalidOperationException("La postazione selezionata non è attiva.");

            var existingPostazione = await _postazioneRepository
                .GetAllQueryable()
                .FirstOrDefaultAsync(p => p.Numero == dto.Numero);

            if (existingPostazione != null)
                throw new InvalidOperationException($"Esiste già una postazione con il numero '{dto.Numero}'.");

            var zona = await _zonaRepository.GetByIdAsync(dto.ZonaId);
            if (zona == null)
                throw new ArgumentException("La zona specificata per la postazione non esiste.");
        }

        public async Task UpdateAsync(PostazioneUpdateDTO dto)
        {
            // Recupero postazione dal repository
            var postazione = await _postazioneRepository.GetByIdAsync(dto.Id);
            if (postazione == null)
                throw new KeyNotFoundException($"Postazione con ID {dto.Id} non trovata.");

            // Controllo prenotazioni
            if (await _postazioneRepository.HasPrenotazioniAsync(dto.Id))
                throw new InvalidOperationException("Impossibile aggiornare la postazione: esistono prenotazioni associate.");

            // Controllo numero duplicato
            var existingPostazione = await _postazioneRepository
                .GetAllQueryable()
                .FirstOrDefaultAsync(p => p.Numero == dto.Numero && p.Id != dto.Id);

            if (existingPostazione != null)
                throw new InvalidOperationException($"Esiste già una postazione con il numero '{dto.Numero}'.");

            // Controllo esistenza zona (FIX-001: evita il messaggio tecnico EF su violazione FK)
            var zona = await _zonaRepository.GetByIdAsync(dto.ZonaId);
            if (zona == null)
                throw new ArgumentException("La zona specificata per la postazione non esiste.");

            // Mappaggio dei campi consentiti con AutoMapper
            _mapper.Map(dto, postazione);

            await _postazioneRepository.UpdateAsync(postazione);

            _cache.Remove(CacheKeys.PostazioniAttive);
        }

        public async Task<List<PostazioneDTO>> GetPostazioniAttiveAsync()
        {
            if (_cache.TryGetValue(CacheKeys.PostazioniAttive, out List<PostazioneDTO>? cached))
                return cached!;

            var postazioni = await _postazioneRepository.GetPostazioniAttiveAsync();

            var result = postazioni.Select(p => new PostazioneDTO
            {
                Id = p.Id,
                Numero = p.Numero,
                CapienzaMassima = p.CapienzaMassima,
                Attiva = p.Attiva,
                ZonaId = p.ZonaId,
                PrenotazioneId = p.PrenotazioniPostazioni?
                          .Select(pp => pp.PrenotazioneId)
                          .ToList() ?? new List<long>()
            }).ToList();

            _cache.Set(CacheKeys.PostazioniAttive, result, CacheDuration);

            return result;
        }

        public async Task<List<PostazioniDisponibiliDTO>> GetPostazioniDisponibiliAsync()
        {
            var postazioni = await _postazioneRepository.GetPostazioniDisponibiliAsync();

            var dto = postazioni.Select(p => new PostazioniDisponibiliDTO
            {
                Id = p.Id,
                Numero = p.Numero,
                CapienzaMassima = p.CapienzaMassima,
                Attiva = p.Attiva,
                ZonaId = p.ZonaId,
                
            }).ToList();

            return dto;
        }

        public async Task<List<PostazioneDTO>> GetPostazioniPerZonaAsync(long zonaId)
        {
            // Verifica zona
            var zona = await _zonaRepository.GetByIdAsync(zonaId);
            if (zona == null)
                throw new ArgumentException($"La zona con ID {zonaId} non esiste.");
            if (!zona.Attiva)
                throw new InvalidOperationException($"La zona con ID {zonaId} non è attiva.");

            // Recupera postazioni libere per zona
            var postazioni = await _postazioneRepository.GetPostazioniPerZonaAsync(zonaId);

            // Mapping manuale
            return postazioni.Select(p => new PostazioneDTO
            {
                Id = p.Id,
                Numero = p.Numero,
                CapienzaMassima = p.CapienzaMassima,
                Attiva = p.Attiva,
                ZonaId = p.ZonaId,
                PrenotazioneId = p.PrenotazioniPostazioni?
                    .Select(pp => pp.PrenotazioneId)
                    .ToList() ?? new List<long>()
            }).ToList();
        }

        public async Task AssociaPostazioneAZonaAsync(long postazioneId, long zonaId)
        {
            #region Validazioni
            var zona = await _zonaRepository.GetByIdAsync(zonaId);
            if (zona == null)
                throw new ArgumentException($"La zona con ID {zonaId} non esiste.");

            if (!zona.Attiva)
                throw new InvalidOperationException($"Impossibile associare: la zona con ID {zonaId} non è attiva.");

            var postazione = await _postazioneRepository.GetByIdAsync(postazioneId);
            if (postazione == null)
                throw new KeyNotFoundException($"Postazione con ID {postazioneId} non trovata.");

            if (!postazione.Attiva)
                throw new InvalidOperationException($"Impossibile associare: la postazione con ID {postazioneId} non è attiva.");

            if (await _postazioneRepository.HasPrenotazioniAsync(postazioneId))
                throw new InvalidOperationException(
                    $"Impossibile associare! Esistono prenotazioni associate alla postazione {postazioneId}."
                );
            #endregion

            postazione.ZonaId = zonaId;

            await _postazioneRepository.UpdateAsync(postazione);

            _cache.Remove(CacheKeys.PostazioniAttive);
        }
    }
}
