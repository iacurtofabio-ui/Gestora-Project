using GestoraWebApi.Models;
using GestoraWebApi.Services.FasciaOrarie.DTOs;

namespace GestoraWebApi.Repositories.FasciaOrarie
{
    public interface IFasciaOrariaRepository
    {
        IQueryable<FasciaOraria> GetAllQueryable();
        Task AddAsync(FasciaOraria entity);
        Task<FasciaOraria> GetByIdAsync(long id);

        /// <summary>
        /// Legge la fascia bloccandone la riga (SELECT ... FOR UPDATE). Da chiamare solo dentro
        /// una transazione: il lock vive fino al commit, fuori da una transazione non serve a niente.
        /// </summary>
        Task<FasciaOraria?> GetByIdConLockAsync(long id);
        Task UpdateAsync(FasciaOraria entity);
        Task DeleteAsync(FasciaOraria entity);
        Task<bool> HasPrenotazioniFutureAsync(long fasciaId, DateOnly daData);
        Task<List<FasciaOraria>> GetFasceAttiveAsync();
        Task<List<FasciaOraria>> GetFasceByGiornoAsync(DayOfWeek giorno);
        Task<int> CountNumeroCopertiFasciaOrariaAsync(long fasciaId, DateOnly data);
        Task<bool> IsFasciaUsataAsync(long fasciaId);
        Task<List<FasciaOraria>> GetAllFasceAsync();
    }
}