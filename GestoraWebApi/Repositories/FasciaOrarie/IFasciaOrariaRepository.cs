using GestoraWebApi.Models;
using GestoraWebApi.Services.FasciaOrarie.DTOs;

namespace GestoraWebApi.Repositories.FasciaOrarie
{
    public interface IFasciaOrariaRepository
    {
        IQueryable<FasciaOraria> GetAllQueryable();
        Task AddAsync(FasciaOraria entity);
        Task<FasciaOraria> GetByIdAsync(long id);
        Task UpdateAsync(FasciaOraria entity);
        Task DeleteAsync(FasciaOraria entity);
        Task<bool> IsAssignedToPrenotazioneAsync(long fasciaId);
        Task<List<FasciaOraria>> GetFasceAttiveAsync();
        Task<List<FasciaOraria>> GetFasceByGiornoAsync(DayOfWeek giorno);
        Task<int> CountNumeroCopertiFasciaOrariaAsync(long fasciaId, DateOnly data);
        Task<bool> IsFasciaUsataAsync(long fasciaId);
        Task<List<FasciaOraria>> GetAllFasceAsync();
    }
}