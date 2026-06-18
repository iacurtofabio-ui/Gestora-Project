
using GestoraWebApi.Context;
using GestoraWebApi.Models;
using GestoraWebApi.Repositories.LogActivity;
using Microsoft.EntityFrameworkCore;

namespace GestoraWebApi.Services.LogActivity
{
    public class LogActivityService : ILogActivityService
    {
        private readonly ILogActivityRepository _repository;
        public LogActivityService(ILogActivityRepository repository)
        {
            _repository = repository;
        }

        public async Task AddAsync(Logging entity)
        {
            await _repository.AddAsync(entity);
        }

        public async Task<Logging> GetByIdAsync(long id)
        {
            return await _repository
               .GetAllQueryable()
               .FirstOrDefaultAsync(l => l.Id == id);
        }

        public async Task LogAsync(string userId, string action, string? ipAddress = null)
        {
            var log = new Logging
            {
                UserId = userId,
                Action = action,
                Timestamp = DateTime.UtcNow,
                IPAddress = ipAddress
            };

            await _repository.AddAsync(log);
        }
    }
}
