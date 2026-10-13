using AutoMapper;
using GestoraWebApi.Common;
using GestoraWebApi.Extensions;
using GestoraWebApi.Models;
using GestoraWebApi.Repositories.Zone;
using GestoraWebApi.Services.LogActivity;
using GestoraWebApi.Services.Sala;
using GestoraWebApi.Services.Zone.DTOs;
using Microsoft.Extensions.Caching.Memory;
using GestoraWebApi.Infrastructure.Exceptions;

namespace GestoraWebApi.Services.Zone
{
    public class ZonaService : IZonaService
    {
        private readonly IZonaRepository _zonaRepository;
        private readonly IMapper _mapper;
        private readonly IMemoryCache _cache;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogActivityService _logActivity;
        private readonly IEsecutoreTransazione _transazione;
        private readonly ICoerenzaSalaService _coerenzaSala;
        private readonly IClock _clock;

        public ZonaService(IZonaRepository zonaRepository,
                            IMapper mapper,
                            IMemoryCache cache,
                            IHttpContextAccessor httpContextAccessor,
                            ILogActivityService logActivity,
                            IEsecutoreTransazione transazione,
                            ICoerenzaSalaService coerenzaSala,
                            IClock clock)
        {
            _clock = clock;
            _coerenzaSala = coerenzaSala;
            _zonaRepository = zonaRepository;
            _mapper = mapper;
            _cache = cache;
            _httpContextAccessor = httpContextAccessor;
            _logActivity = logActivity;
            _transazione = transazione;
        }

        public async Task AddAsync(ZonaDTO entity)
        {
            //validation nome univoco
            var existingZona = await _zonaRepository.GetByNameAsync(entity.Nome);

            if (existingZona != null)
                throw new ConflictException("Esiste già una zona con questo nome.");

            var zona = _mapper.Map<Zona>(entity);

            // REV-032: scrittura e traccia nell'audit trail sono una sola operazione.
            await _transazione.EseguiAsync(async () =>
            {
                await _zonaRepository.AddAsync(zona);
                await _logActivity.LogAsync(_httpContextAccessor.HttpContext.GetAuthenticatedUserId(), $"Creata zona '{zona.Nome}'", _httpContextAccessor.HttpContext.GetIpAddress());
            });

            // La cache si invalida dopo il commit: prima non avrebbe senso, perche' la modifica
            // potrebbe ancora non esserci.
            InvalidateZoneCache();
        }

        public async Task DeleteAsync(long zonaId)
        {
            // Controllo se la zona esiste
            var zona = await _zonaRepository.GetByIdAsync(zonaId);

            if (zona == null)
                throw new NotFoundException($"La zona con ID {zonaId} non esiste.");

            // Controllo se la zona è assegnata ad almeno una postazione
            bool usata = await _zonaRepository.IsZonaUsataAsync(zonaId);

            if (usata)
                throw new ConflictException("Non è possibile cancellare la zona perché è assegnata ad almeno una postazione.");

            await _transazione.EseguiAsync(async () =>
            {
                await _zonaRepository.DeleteAsync(zona);
                await _logActivity.LogAsync(_httpContextAccessor.HttpContext.GetAuthenticatedUserId(), $"Eliminata zona '{zona.Nome}' (ID {zonaId})", _httpContextAccessor.HttpContext.GetIpAddress());
            });

            InvalidateZoneCache();
        }

        public async Task<List<ZonaDTO>> GetAllZoneAsync()
        {
            if (_cache.TryGetValue(CacheKeys.ZoneAll, out List<ZonaDTO>? cached))
                return cached!;

            var allZone = await _zonaRepository.GetAllZoneAsync();
            var result = _mapper.Map<List<ZonaDTO>>(allZone);

            _cache.Set(CacheKeys.ZoneAll, result, CacheKeys.Durata);

            return result;
        }

        public async Task<List<ZonaDTO>> GetAllZoneAttiveAsync()
        {
            if (_cache.TryGetValue(CacheKeys.ZoneAttive, out List<ZonaDTO>? cached))
                return cached!;

            var zoneAttive = await _zonaRepository.GetAllZoneAttiveAsync();
            var result = _mapper.Map<List<ZonaDTO>>(zoneAttive);

            _cache.Set(CacheKeys.ZoneAttive, result, CacheKeys.Durata);

            return result;
        }

        public async Task<ZonaDTO> GetByIdAsync(long id)
        {
            var zona = await _zonaRepository.GetByIdAsync(id);

            if (zona == null)
                return null;

            return _mapper.Map<ZonaDTO>(zona);
        }

        public async Task<bool> IsZonaUsataAsync(long Id)
        {
            return await _zonaRepository.IsZonaUsataAsync(Id);
        }

        public async Task UpdateAsync(ZonaDTO entity)
        {
            var existingZona = await _zonaRepository.GetByIdAsync(entity.Id);

            if (existingZona == null)
                throw new NotFoundException("Zona non trovata.");

            //validation nome univoco
            var zonaWithSameName = await _zonaRepository.GetByNameAsync(entity.Nome);

            if (zonaWithSameName != null && zonaWithSameName.Id != entity.Id)
                throw new ConflictException("Esiste già una zona con questo nome.");

            // V2-007: spegnere una zona toglie i suoi tavoli dai posti della sala.
            if (existingZona.Attiva && !entity.Attiva)
                await VerificaDisattivazioneAsync(existingZona);

            existingZona.Nome = entity.Nome;
            existingZona.Attiva = entity.Attiva;

            await _transazione.EseguiAsync(async () =>
            {
                await _zonaRepository.UpdateAsync(existingZona);
                await _logActivity.LogAsync(_httpContextAccessor.HttpContext.GetAuthenticatedUserId(), $"Modificata zona '{existingZona.Nome}' (ID {existingZona.Id})", _httpContextAccessor.HttpContext.GetIpAddress());
            });

            InvalidateZoneCache();
        }

        public async Task UpdateStatoZonaAsync(long zonaId, bool attiva)
        {
            var zona = await _zonaRepository.GetByIdAsync(zonaId);

            if (zona == null)
                throw new KeyNotFoundException($"Zona con ID {zonaId} non trovata.");

            if (!attiva)
                await VerificaDisattivazioneAsync(zona);

            await _transazione.EseguiAsync(async () =>
            {
                await _zonaRepository.UpdateStatoZonaAsync(zonaId, attiva);
                await _logActivity.LogAsync(_httpContextAccessor.HttpContext.GetAuthenticatedUserId(),
                    $"Zona '{zona.Nome}' (ID {zonaId}) impostata come {(attiva ? "attiva" : "non attiva")}", _httpContextAccessor.HttpContext.GetIpAddress());
            });

            InvalidateZoneCache();
        }

        /// <summary>
        /// Riattivare una zona aggiunge posti: si controllano solo gli spegnimenti.
        /// </summary>
        private async Task VerificaDisattivazioneAsync(Zona zona)
        {
            await GuardImpegniFuturiAsync(zona);

            // V2-007: la zona spenta non deve portare i posti della sala sotto il tetto di una
            // fascia attiva.
            await _coerenzaSala.VerificaModificaSalaAsync(tavoli => tavoli, zone => zone.Where(id => id != zona.Id));
        }

        /// <summary>
        /// V2-012: stessa regola dei tavoli (V2-009). Spegnere una zona lascerebbe prenotazioni
        /// ancora da servire assegnate a tavoli che non si possono più prenotare, senza avviso:
        /// si blocca e si dice quale prenotazione sposta o annulla prima chi gestisce la sala.
        /// </summary>
        private async Task GuardImpegniFuturiAsync(Zona zona)
        {
            var impegni = await _zonaRepository.GetImpegniFuturiAsync(zona.Id, _clock.TodayInRome);
            if (impegni.Count == 0)
                return;

            throw new ConflictException(
                $"Impossibile disattivare la zona '{zona.Nome}': ha tavoli assegnati " +
                $"{DescrizioneImpegni.Descrivi(impegni)}. Sposta o annulla prima quelle prenotazioni.");
        }

        private void InvalidateZoneCache()
        {
            _cache.Remove(CacheKeys.ZoneAll);
            _cache.Remove(CacheKeys.ZoneAttive);
        }

    }
}