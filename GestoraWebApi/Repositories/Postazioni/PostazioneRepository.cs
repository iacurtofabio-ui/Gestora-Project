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

        // L'unico chiamante che ha bisogno anche delle righe join e' l'elenco per l'interfaccia,
        // che espone PostazioneDTO.PrenotazioneId. Resta un metodo separato per non far pagare
        // quel carico a chi non lo usa.
        public async Task<List<Postazione>> GetPostazioniAttiveConPrenotazioniAsync()
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

        // REV-099: prima questo metodo si chiamava HasPrenotazioniAsync e rispondeva "si'" alla
        // presenza di una qualsiasi riga in PrenotazioniPostazioni, storico compreso. Il risultato
        // era che un tavolo, dopo la sua prima prenotazione conclusa, non era piu' rinominabile,
        // spostabile di zona ne' disattivabile: un locale reale ci arriva in pochi giorni. Cio'
        // che va protetto sono solo gli impegni ancora da onorare, quindi il filtro e' sulla data.
        //
        // Basta la data perche' annullare una prenotazione cancella le sue righe join (REV-003):
        // le righe presenti appartengono sempre a prenotazioni vive. Si usa la copia
        // denormalizzata DataPrenotazione sulla riga join, la stessa che regge l'unique index
        // sullo slot, cosi' il controllo non deve risalire alla prenotazione.
        public async Task<bool> HasPrenotazioniFutureAsync(long postazioneId, DateOnly daData)
        {
            return await _dbSet
                .Where(p => p.Id == postazioneId)
                .SelectMany(p => p.PrenotazioniPostazioni)
                .AnyAsync(pp => pp.DataPrenotazione >= daData);
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

        public async Task DeleteAsync(Postazione postazione)
        {
            _context.Remove(postazione);
            await _context.SaveChangesAsync();
        }
    }
}
