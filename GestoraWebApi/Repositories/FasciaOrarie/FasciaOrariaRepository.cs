using GestoraWebApi.Context;
using GestoraWebApi.Enums;
using GestoraWebApi.Models;
using Microsoft.EntityFrameworkCore;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace GestoraWebApi.Repositories.FasciaOrarie
{
    public class FasciaOrariaRepository : IFasciaOrariaRepository
    {
        private readonly GestoraContext _context;
        private readonly DbSet<FasciaOraria> _dbSet;

        public FasciaOrariaRepository(GestoraContext context)
        {
            _context = context;
            _dbSet = _context.Set<FasciaOraria>();
        }

        public IQueryable<FasciaOraria> GetAllQueryable()
        {
            return _dbSet.AsNoTracking();
        }

        public async Task AddAsync(FasciaOraria entity)
        {
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public Task<FasciaOraria> GetByIdAsync(long id)
        {
            //recupera la fascia oraria per ID
            return _dbSet.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id);
        }

        public async Task UpdateAsync(FasciaOraria entity)
        {
            _context.FasciaOrarie.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> IsAssignedToPrenotazioneAsync(long fasciaId)
        {
            return await _context.Set<Prenotazione>()
                                 .AsNoTracking()
                                 .AnyAsync(p => p.FasciaOrariaId == fasciaId);
        }

        public async Task<List<FasciaOraria>> GetFasceAttiveAsync()
        {
            return await _context.Set<FasciaOraria>()
                                 .AsNoTracking()
                                 .Where(f => f.Attiva)
                                 .ToListAsync();
        }

        public async Task<List<FasciaOraria>> GetFasceByGiornoAsync(DayOfWeek giorno)
        {
            return await _context.Set<FasciaOraria>()
                                 .AsNoTracking()
                                 .Where(f => f.Attiva && f.GiornoSettimana == giorno)
                                 .OrderBy(f => f.OrarioInizio)
                                 .ToListAsync();
        }

        public async Task<int> CountNumeroCopertiFasciaOrariaAsync(long fasciaId, DateOnly data)
        {

            return await _context.Prenotazioni
                    .Where(p => p.FasciaOrariaId == fasciaId &&
                    p.DataPrenotazione == data &&
                    p.Stato != StatoPrenotazione.Annullata)
                    .SumAsync(p => p.NumeroCoperti);
        }

        public async Task<bool> IsFasciaUsataAsync(long fasciaId)
        {
            return await _context.Prenotazioni
                .AsNoTracking()
                .AnyAsync(p => p.FasciaOrariaId == fasciaId);
        }

        public async Task DeleteAsync(FasciaOraria entity)
        {
            _context.FasciaOrarie.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}