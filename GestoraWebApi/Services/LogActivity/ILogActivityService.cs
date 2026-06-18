using GestoraWebApi.Models;

namespace GestoraWebApi.Services.LogActivity
{
    public interface ILogActivityService
    {
        Task AddAsync(Logging entity);
        Task<Logging> GetByIdAsync(long id);
        Task LogAsync(string userId, string action, string? ipAddress = null);
    }
}
