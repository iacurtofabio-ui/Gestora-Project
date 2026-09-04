
using GestoraWebApi.Common;
using GestoraWebApi.Services.LogActivity.DTOs;
using GestoraWebApi.Context;
using GestoraWebApi.Models;
using GestoraWebApi.Repositories.LogActivity;
using Microsoft.EntityFrameworkCore;

namespace GestoraWebApi.Services.LogActivity
{
    public class LogActivityService : ILogActivityService
    {
        private readonly ILogActivityRepository _repository;
        private readonly GestoraContext _context;

        public LogActivityService(ILogActivityRepository repository, GestoraContext context)
        {
            _repository = repository;
            _context = context;
        }

        /// <summary>
        /// REV-037: lettura dell'audit trail. Il nome utente si prende con una join esplicita
        /// invece che con una navigazione: Logging non ha una FK verso Utenti, ed e' voluto -
        /// la traccia deve sopravvivere all'utente, altrimenti eliminandolo si perderebbe anche
        /// la storia di cosa ha fatto. Per gli id senza corrispondenza il nome resta null.
        /// </summary>
        public async Task<PagedResult<LogActivityDTO>> GetLogAsync(LogActivityQueryParams query)
        {
            var queryable = _repository.GetAllQueryable();

            if (!string.IsNullOrWhiteSpace(query.UserId))
                queryable = queryable.Where(l => l.UserId == query.UserId);

            if (query.Da.HasValue)
                queryable = queryable.Where(l => l.Timestamp >= query.Da.Value);

            if (query.A.HasValue)
                queryable = queryable.Where(l => l.Timestamp <= query.A.Value);

            if (!string.IsNullOrWhiteSpace(query.Azione))
                queryable = queryable.Where(l => l.Action.Contains(query.Azione));

            var totalCount = await queryable.CountAsync();

            // Dal piu' recente, con Id come secondo criterio: due eventi possono cadere nello
            // stesso istante, e senza un ordine totale la paginazione non e' stabile (REV-020).
            var pagina = queryable
                .OrderByDescending(l => l.Timestamp)
                .ThenByDescending(l => l.Id)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize);

            // Left join: le righe restano tutte anche quando l'utente non esiste piu'.
            var items = await (from l in pagina
                               join u in _context.Users on l.UserId equals u.Id into utenti
                               from u in utenti.DefaultIfEmpty()
                               select new LogActivityDTO
                               {
                                   Id = l.Id,
                                   UserId = l.UserId,
                                   UserName = u == null ? null : u.UserName,
                                   Action = l.Action,
                                   Timestamp = l.Timestamp,
                                   IPAddress = l.IPAddress
                               })
                              .OrderByDescending(l => l.Timestamp)
                              .ThenByDescending(l => l.Id)
                              .ToListAsync();

            return new PagedResult<LogActivityDTO>
            {
                Items = items,
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize
            };
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
