using GestoraWebApi.Context;
using GestoraWebApi.Models;
using Microsoft.EntityFrameworkCore;

namespace GestoraWebApi.Repositories.Postazioni
{
    public class PostazioneRepository : IPostazioneRepository
    {
        private readonly GestoraContext _context;
        private readonly DbSet<Postazione> _dbSet;

        public PostazioneRepository(GestoraContext context)
        {
            _context = context;
            _dbSet = _context.Set<Postazione>();
        }

        public IQueryable<Postazione> GetAllQueryable()
        {
            return _dbSet.AsNoTracking();
        }

        public async Task AddAsync(Postazione entity)
        {
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Postazione>> GetPostazioniAttiveAsync()
        {
            return await _dbSet
                .Where(p => p.Attiva)
                .Include(p => p.PrenotazioniPostazioni)
                .OrderByDescending(p => p.CapienzaMassima)
                .ToListAsync();
        }

        public async Task<Postazione> GetByIdAsync(long id)
        {
            return await _dbSet
                           .Include(p => p.PrenotazioniPostazioni)
                           .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<bool> HasPrenotazioniAsync(long postazioneId)
        {
            return await _dbSet
                .Where(p => p.Id == postazioneId)
                .SelectMany(p => p.PrenotazioniPostazioni)
                .AnyAsync();
        }
        public async Task UpdateAsync(Postazione postazione)
        {
            _dbSet.Update(postazione);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Postazione>> GetPostazioniDisponibiliAsync()
        {
            return await _dbSet
                    .Where(p => p.Attiva)
                    .Include(p => p.PrenotazioniPostazioni)
                    .Where(p => !p.PrenotazioniPostazioni.Any())
                    .OrderBy(p => p.Numero)
                    .ToListAsync();
        }

        public async Task<List<Postazione>> GetPostazioniPerZonaAsync(long zonaId)
        {
            return await _dbSet
                   .Where(p => p.ZonaId == zonaId
                               && p.Attiva
                               && !p.PrenotazioniPostazioni.Any())
                   .Include(p => p.PrenotazioniPostazioni)
                   .OrderBy(p => p.Numero)
                   .ToListAsync();
        }

        public async Task DeleteAsync(Postazione postazione)
        {
            _context.Remove(postazione);
            await _context.SaveChangesAsync();
        }
    }
}
