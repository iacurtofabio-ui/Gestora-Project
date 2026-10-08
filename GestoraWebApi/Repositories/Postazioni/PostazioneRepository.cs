using GestoraWebApi.Context;
using GestoraWebApi.Enums;
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

        // REV-023: questa query sta nel percorso caldo dell'assegnazione del tavolo e del calcolo
        // di disponibilita', che la chiamano a ogni prenotazione. L'Include su
        // PrenotazioniPostazioni tirava dentro l'intero storico di ogni tavolo - un dato che
        // cresce senza limite e che quei percorsi non guardano nemmeno: le postazioni occupate
        // le calcolano con una query mirata sullo slot richiesto.
        public async Task<List<Postazione>> GetPostazioniAttiveAsync()
        {
            return await _dbSet
                .Where(p => p.Attiva)
                .OrderByDescending(p => p.CapienzaMassima)
                .ToListAsync();
        }

        public async Task<Postazione> GetByIdAsync(long id)
        {
            return await _dbSet
                           .Include(p => p.PrenotazioniPostazioni)
                           .FirstOrDefaultAsync(p => p.Id == id);
        }

        // V2-009: sostituisce HasPrenotazioniFutureAsync, che rispondeva solo si'/no e contava
        // anche le prenotazioni di oggi gia' concluse (Completata, NonPresentata): bastava una di
        // queste per bloccare qualsiasi modifica al tavolo. Qui contano solo gli impegni ancora da
        // servire, e tornano con i loro tavoli perche' il service deve poter dire se una
        // prenotazione ci sta ancora dopo una riduzione dei posti, e quale nominare nel messaggio.
        public async Task<List<Prenotazione>> GetImpegniFuturiAsync(long postazioneId, DateOnly daData)
        {
            return await _context.Prenotazioni
                .AsNoTracking()
                .Where(p => p.DataPrenotazione >= daData
                         && (p.Stato == StatoPrenotazione.Attiva || p.Stato == StatoPrenotazione.InCorso)
                         && p.PrenotazioniPostazioni.Any(pp => pp.PostazioneId == postazioneId))
                .Include(p => p.FasciaOraria)
                .Include(p => p.PrenotazioniPostazioni)
                    .ThenInclude(pp => pp.Postazione)
                .OrderBy(p => p.DataPrenotazione)
                .ThenBy(p => p.FasciaOraria.OrarioInizio)
                .ToListAsync();
        }

        // Completata, NonPresentata e Annullata non impegnano piu' il tavolo: chiudono lo storico.
        // Le righe join di una Annullata sono gia' cancellate (REV-003), quelle di Completata e
        // NonPresentata restano, quindi il filtro va sullo stato della prenotazione, non sulla
        // sola presenza della riga.
        public async Task<bool> HasPrenotazioniViveAsync(long postazioneId)
        {
            return await _context.PrenotazioniPostazioni
                .AsNoTracking()
                .AnyAsync(pp => pp.PostazioneId == postazioneId
                             && (pp.Prenotazione.Stato == StatoPrenotazione.Attiva
                                 || pp.Prenotazione.Stato == StatoPrenotazione.InCorso));
        }

        public async Task UpdateAsync(Postazione postazione)
        {
            _dbSet.Update(postazione);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Postazione>> GetPostazioniDisponibiliAsync()
        {
            // DEAD-CODE-001: rimosso lo stesso filtro "mai prenotata" già corretto su
            // GetPostazioniPerZonaAsync — escludeva una postazione se aveva mai avuto una
            // prenotazione, invece di verificare la disponibilità per data/fascia specifica.
            return await _dbSet
                    .Where(p => p.Attiva)
                    .OrderBy(p => p.Numero)
                    .ToListAsync();
        }

        public async Task<List<Postazione>> GetPostazioniPerZonaAsync(long zonaId)
        {
            return await _dbSet
                   .Where(p => p.ZonaId == zonaId && p.Attiva)
                   .OrderBy(p => p.Numero)
                   .ToListAsync();
        }

        public async Task<List<Postazione>> GetTuttePostazioniPerZonaAsync(long zonaId)
        {
            return await _dbSet
                   .Where(p => p.ZonaId == zonaId)
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
