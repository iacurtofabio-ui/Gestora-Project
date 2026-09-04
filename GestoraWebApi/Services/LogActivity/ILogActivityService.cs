using GestoraWebApi.Common;
using GestoraWebApi.Models;
using GestoraWebApi.Services.LogActivity.DTOs;

namespace GestoraWebApi.Services.LogActivity
{
    public interface ILogActivityService
    {
        Task AddAsync(Logging entity);
        Task<Logging> GetByIdAsync(long id);
        Task LogAsync(string userId, string action, string? ipAddress = null);

        /// <summary>
        /// REV-037: lettura paginata e filtrata dell'audit trail. Finora la tabella si scriveva
        /// soltanto: per rileggerla bisognava collegarsi al database.
        /// </summary>
        Task<PagedResult<LogActivityDTO>> GetLogAsync(LogActivityQueryParams query);
    }
}
