using GestoraWebApi.Models;

namespace GestoraWebApi.Repositories.LogActivity
{
    public interface ILogActivityRepository
    {
        IQueryable<Logging> GetAllQueryable();
        Task AddAsync(Logging log);
    }
}
