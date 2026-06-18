using GestoraWebApi.Models;
using GestoraWebApi.Services.Zone.DTOs;

namespace GestoraWebApi.Repositories.Zone
{
    public interface IZonaRepository : IRepository<Zona>
    {
        Task<Zona?> GetByNameAsync(string nome);
        Task<List<Zona>> GetAllZoneAttiveAsync();
        Task<bool> IsZonaUsataAsync(long zonaId);
        Task<List<Zona>> GetAllZoneAsync();
        Task UpdateStatoZonaAsync(long zonaId, bool attiva);
    }
}