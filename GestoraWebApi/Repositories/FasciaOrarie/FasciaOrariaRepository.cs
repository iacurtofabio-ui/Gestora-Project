using GestoraWebApi.Context;
using GestoraWebApi.Enums;
using GestoraWebApi.Models;
using GestoraWebApi.Services.FasciaOrarie.DTOs;
using Microsoft.EntityFrameworkCore;

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

        public Task<FasciaOraria?> GetByIdConLockAsync(long id)
        {
            // CAP-001: FOR UPDATE serializza chi prenota sulla stessa fascia. Il lock resta
            // finche' la transazione chiamante non fa commit: fuori da una transazione la riga
            // viene rilasciata subito e il metodo equivale a GetByIdAsync.
            return _dbSet
                .FromSqlInterpolated($"SELECT * FROM \"FasceOrarie\" WHERE \"Id\" = {id} FOR UPDATE")
                .AsNoTracking()
                .FirstOrDefaultAsync();
        }

        public async Task UpdateAsync(FasciaOraria entity)
        {
            _context.FasciaOrarie.Update(entity);
            await _context.SaveChangesAsync();
        }

        // Stesso criterio dei tavoli (REV-099): lo storico non blocca la modifica della fascia,
        // solo gli impegni ancora da onorare (Attiva/InCorso da oggi in avanti).
        public async Task<bool> HasPrenotazioniFutureAsync(long fasciaId, DateOnly daData)
        {
            return await _context.Set<Prenotazione>()
                                 .AsNoTracking()
                                 .AnyAsync(p => p.FasciaOrariaId == fasciaId &&
                                                p.DataPrenotazione >= daData &&
                                                (p.Stato == StatoPrenotazione.Attiva ||
                                                 p.Stato == StatoPrenotazione.InCorso));
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
                    p.Stato != StatoPrenotazione.Annullata &&
                    p.Stato != StatoPrenotazione.NonPresentata)
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

        public async Task<List<FasciaOraria>> GetAllFasceAsync()
        {
            // FASE 4: nessun ordinamento prima d ora - la pagina mostrava le fasce nell ordine di
            // inserimento. DayOfWeek parte da Domenica (0): (int + 6) % 7 sposta Lunedi in testa
            // mantenendo il valore numerico salvato. ThenBy(Id) rende l ordinamento totale.
            return await _context.Set<FasciaOraria>()
                .AsNoTracking()
                .OrderBy(f => ((int)f.GiornoSettimana + 6) % 7)
                .ThenBy(f => f.OrarioInizio)
                .ThenBy(f => f.Id)
                .ToListAsync();
        }
    }
}