using AutoMapper;
using GestoraWebApi.Common;
using GestoraWebApi.Extensions;
using GestoraWebApi.Models;
using GestoraWebApi.Repositories.Zone;
using GestoraWebApi.Services.LogActivity;
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

        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

        public ZonaService(IZonaRepository zonaRepository,
                            IMapper mapper,
                            IMemoryCache cache,
                            IHttpContextAccessor httpContextAccessor,
                            ILogActivityService logActivity,
                            IEsecutoreTransazione transazione)
        {
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
                await _logActivity.LogAsync(GetAuthenticatedUserId(), $"Creata zona '{zona.Nome}'", GetIpAddress());
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
                await _logActivity.LogAsync(GetAuthenticatedUserId(), $"Eliminata zona '{zona.Nome}' (ID {zonaId})", GetIpAddress());
            });

            InvalidateZoneCache();
        }

        public async Task<List<ZonaDTO>> GetAllZoneAsync()
        {
            if (_cache.TryGetValue(CacheKeys.ZoneAll, out List<ZonaDTO>? cached))
                return cached!;

            var allZone = await _zonaRepository.GetAllZoneAsync();
            var result = _mapper.Map<List<ZonaDTO>>(allZone);

            _cache.Set(CacheKeys.ZoneAll, result, CacheDuration);

            return result;
        }

        public async Task<List<ZonaDTO>> GetAllZoneAttiveAsync()
        {
            if (_cache.TryGetValue(CacheKeys.ZoneAttive, out List<ZonaDTO>? cached))
                return cached!;

            var zoneAttive = await _zonaRepository.GetAllZoneAttiveAsync();
            var result = _mapper.Map<List<ZonaDTO>>(zoneAttive);

            _cache.Set(CacheKeys.ZoneAttive, result, CacheDuration);

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

            existingZona.Nome = entity.Nome;
            existingZona.Attiva = entity.Attiva;

            await _transazione.EseguiAsync(async () =>
            {
                await _zonaRepository.UpdateAsync(existingZona);
                await _logActivity.LogAsync(GetAuthenticatedUserId(), $"Modificata zona '{existingZona.Nome}' (ID {existingZona.Id})", GetIpAddress());
            });

            InvalidateZoneCache();
        }

        public async Task UpdateStatoZonaAsync(long zonaId, bool attiva)
        {
            var zona = await _zonaRepository.GetByIdAsync(zonaId);

            if (zona == null)
                throw new KeyNotFoundException($"Zona con ID {zonaId} non trovata.");

            await _transazione.EseguiAsync(async () =>
            {
                await _zonaRepository.UpdateStatoZonaAsync(zonaId, attiva);
                await _logActivity.LogAsync(GetAuthenticatedUserId(),
                    $"Zona '{zona.Nome}' (ID {zonaId}) impostata come {(attiva ? "attiva" : "non attiva")}", GetIpAddress());
            });

            InvalidateZoneCache();
        }

        private void InvalidateZoneCache()
        {
            _cache.Remove(CacheKeys.ZoneAll);
            _cache.Remove(CacheKeys.ZoneAttive);
        }

        private string GetAuthenticatedUserId()
            => _httpContextAccessor.HttpContext?.User.GetAuthenticatedUserId()
               ?? throw new UnauthorizedAccessException("Utente non autenticato.");

        private string? GetIpAddress()
            => _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
    }
}