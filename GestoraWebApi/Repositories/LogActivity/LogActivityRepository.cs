using GestoraWebApi.Context;
using GestoraWebApi.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading;

namespace GestoraWebApi.Repositories.LogActivity
{
    public class LogActivityRepository : ILogActivityRepository
    {
        private readonly GestoraContext _context;
        private readonly DbSet<Logging> _dbSet;

        public LogActivityRepository(GestoraContext context)
        {
            _context = context;
            _dbSet = _context.Set<Logging>();
        }

        public IQueryable<Logging> GetAllQueryable()
        {
            return _dbSet.AsNoTracking();
        }

        public async Task AddAsync(Logging log)
        {
            await _dbSet.AddAsync(log);
            await _context.SaveChangesAsync();
        }
    }
}
