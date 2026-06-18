using GestoraWebApi.Models;
using GestoraWebApi.Services.Zone.DTOs;

namespace GestoraWebApi.Services.Zone
{
    public interface IZonaService : IService<ZonaDTO>
    {
        Task<List<ZonaDTO>> GetAllZoneAttiveAsync();
        Task<bool> IsZonaUsataAsync(long zonaId);
        Task<List<ZonaDTO>> GetAllZoneAsync();
        Task UpdateStatoZonaAsync(long zonaId, bool attiva);

    }
}
