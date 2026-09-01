using GestoraWebApi.Context;
using GestoraWebApi.Enums;
using GestoraWebApi.Models;
using Microsoft.EntityFrameworkCore;


namespace GestoraWebApi.Repositories.Prenotazioni
{
    public class PrenotazioniRepository : IPrenotazioniRepository
    {
        private readonly GestoraContext _context;
        public PrenotazioniRepository(GestoraContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Prenotazione entity)
        {
            await _context.Prenotazioni.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public async Task<Prenotazione?> GetByIdAsync(long id)
        {
            return await _context.Prenotazioni
                .AsNoTracking()
                .Include(p => p.User)
                .Include(p => p.FasciaOraria)
                .Include(p => p.PrenotazioniPostazioni)
                    .ThenInclude(pp => pp.Postazione)
                        .ThenInclude(po => po.Zona)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        IQueryable<Prenotazione> IRepository<Prenotazione>.GetAllQueryableAsync()
        {
            return _context.Prenotazioni.AsNoTracking();
        }

        public async Task UpdateAsync(Prenotazione entity)
        {
            _context.Prenotazioni.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Prenotazione entity)
        {
            _context.Prenotazioni.Remove(entity);
            await _context.SaveChangesAsync();
        }

        public async Task<Prenotazione?> GetTrackedByIdAsync(long id)
        {
            return await _context.Prenotazioni
                .Include(p => p.PrenotazioniPostazioni)
                .Include(p => p.FasciaOraria)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<List<Prenotazione>> GetPrenotazioniByDataAsync(DateOnly data)
        {
            return await _context.Prenotazioni
                .AsNoTracking()
                .Where(p => p.DataPrenotazione == data && p.Stato != StatoPrenotazione.Annullata)
                .Include(p => p.PrenotazioniPostazioni)
                .ThenInclude(pp => pp.Postazione)
                .ToListAsync();
        }

        public async Task<List<Prenotazione>> GetAllPrenotazioniAsync()
        {
            return await _context.Prenotazioni
                .AsNoTracking()
                .OrderBy(p => p.DataPrenotazione)
                .ToListAsync();
        }

        public async Task<List<FasciaOraria>> GetFasceOrarieByDayAsync(DayOfWeek giorno)
        {
            return await _context.FasciaOrarie
                .Where(f => f.Attiva && f.GiornoSettimana == giorno)
                .OrderBy(f => f.OrarioInizio)
                .ToListAsync();
        }
    }
}
