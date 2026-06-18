using GestoraWebApi.Context;
using GestoraWebApi.Models;
using GestoraWebApi.Services.Zone.DTOs;
using Microsoft.EntityFrameworkCore;

namespace GestoraWebApi.Repositories.Zone
{
    public class ZonaRepository : IZonaRepository
    {
        private readonly GestoraContext _context;
        private readonly ILogger<ZonaRepository> _logger;

        public ZonaRepository(GestoraContext context,
                              ILogger<ZonaRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task AddAsync(Zona entity)
        {
            var zona = await _context.Zone.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Zona entity)
        {
            _context.Zone.Remove(entity);
            await _context.SaveChangesAsync();
        }

        //public Task<IQueryable<Zona>> GetAllQueryableAsync()
        //{
        //    throw new NotImplementedException();
        //}

        public async Task<List<Zona>> GetAllZoneAsync()
        {
            return await _context.Zone
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<Zona>> GetAllZoneAttiveAsync()
        {
            return await _context.Zone
                .AsNoTracking()
                .Where(z => z.Attiva)
                .ToListAsync();
        }

        public async Task<Zona?> GetByIdAsync(long id)
        {
            return await _context.Zone
                .AsNoTracking()
                .FirstOrDefaultAsync(z => z.Id == id);
        }

        public async Task<Zona?> GetByNameAsync(string nome)
        {
            return await _context.Zone
                .AsNoTracking()
                .FirstOrDefaultAsync(z => z.Nome == nome);
        }

        public async Task<bool> IsZonaUsataAsync(long zonaId)
        {
            return await _context.Postazioni
                .AsNoTracking()
                .AnyAsync(p => p.ZonaId == zonaId);
        }

        public async Task UpdateAsync(Zona entity)
        {
            _context.Zone.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateStatoZonaAsync(long zonaId, bool attiva)
        {
           var zona = await _context.Zone.FirstOrDefaultAsync(z => z.Id == zonaId);

            zona.Attiva = attiva;
            _context.Zone.Update(zona);
            await _context.SaveChangesAsync();
        }

        IQueryable<Zona> IRepository<Zona>.GetAllQueryableAsync()
        {
            throw new NotImplementedException();
        }
    }
}
