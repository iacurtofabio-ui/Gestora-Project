using AutoMapper;
using GestoraWebApi.Common;
using GestoraWebApi.Extensions;
using GestoraWebApi.Models;
using GestoraWebApi.Repositories.Zone;
using GestoraWebApi.Services.LogActivity;
using GestoraWebApi.Services.Zone.DTOs;
using Microsoft.Extensions.Caching.Memory;

namespace GestoraWebApi.Services.Zone
{
    public class ZonaService : IZonaService
    {
        private readonly IZonaRepository _zonaRepository;
        private readonly IMapper _mapper;
        private readonly IMemoryCache _cache;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogActivityService _logActivity;

        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

        public ZonaService(IZonaRepository zonaRepository,
                            IMapper mapper,
                            IMemoryCache cache,
                            IHttpContextAccessor httpContextAccessor,
                            ILogActivityService logActivity)
        {
            _zonaRepository = zonaRepository;
            _mapper = mapper;
            _cache = cache;
            _httpContextAccessor = httpContextAccessor;
            _logActivity = logActivity;
        }

        public async Task AddAsync(ZonaDTO entity)
        {
            //validation nome univoco
            var existingZona = await _zonaRepository.GetByNameAsync(entity.Nome);

            if (existingZona != null)
                throw new InvalidOperationException("Esiste già una zona con questo nome.");

            var zona = _mapper.Map<Zona>(entity);
            await _zonaRepository.AddAsync(zona);

            InvalidateZoneCache();
            await _logActivity.LogAsync(GetAuthenticatedUserId(), $"Creata zona '{zona.Nome}'", GetIpAddress());
        }

        public async Task DeleteAsync(long zonaId)
        {
            // Controllo se la zona esiste
            var zona = await _zonaRepository.GetByIdAsync(zonaId);

            if (zona == null)
                throw new InvalidOperationException($"La zona con ID {zonaId} non esiste.");

            // Controllo se la zona è assegnata ad almeno una postazione
            bool usata = await _zonaRepository.IsZonaUsataAsync(zonaId);

            if (usata)
                throw new InvalidOperationException("Non è possibile cancellare la zona perché è assegnata ad almeno una postazione.");

            await _zonaRepository.DeleteAsync(zona);

            InvalidateZoneCache();
            await _logActivity.LogAsync(GetAuthenticatedUserId(), $"Eliminata zona '{zona.Nome}' (ID {zonaId})", GetIpAddress());
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
                throw new InvalidOperationException("Zona non trovata.");

            //validation nome univoco
            var zonaWithSameName = await _zonaRepository.GetByNameAsync(entity.Nome);

            if (zonaWithSameName != null && zonaWithSameName.Id != entity.Id)
                throw new InvalidOperationException("Esiste già una zona con questo nome.");

            existingZona.Nome = entity.Nome;
            existingZona.Attiva = entity.Attiva;

            await _zonaRepository.UpdateAsync(existingZona);

            InvalidateZoneCache();
            await _logActivity.LogAsync(GetAuthenticatedUserId(), $"Modificata zona '{existingZona.Nome}' (ID {existingZona.Id})", GetIpAddress());
        }

        public async Task UpdateStatoZonaAsync(long zonaId, bool attiva)
        {
            var zona = await _zonaRepository.GetByIdAsync(zonaId);

            if (zona == null)
                throw new KeyNotFoundException($"Zona con ID {zonaId} non trovata.");

            await _zonaRepository.UpdateStatoZonaAsync(zonaId, attiva);

            InvalidateZoneCache();
            await _logActivity.LogAsync(GetAuthenticatedUserId(),
                $"Zona '{zona.Nome}' (ID {zonaId}) impostata come {(attiva ? "attiva" : "non attiva")}", GetIpAddress());
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