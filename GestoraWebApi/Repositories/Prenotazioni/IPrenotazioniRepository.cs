using GestoraWebApi.Enums;
using GestoraWebApi.Models;

namespace GestoraWebApi.Repositories.Prenotazioni
{
    public interface IPrenotazioniRepository : IRepository<Prenotazione>
    {
        Task<Prenotazione?> GetTrackedByIdAsync(long id);

        /// <summary>
        /// REV-022: porta allo stato indicato tutte le prenotazioni elencate, con una sola
        /// scrittura. Serve ai job notturni, che prima aggiornavano una riga per volta.
        /// Restituisce il numero di righe effettivamente modificate.
        /// </summary>
        Task<int> AggiornaStatoAsync(IReadOnlyCollection<long> ids, StatoPrenotazione nuovoStato);

        /// <summary>
        /// REV-022: elimina in un colpo solo le prenotazioni elencate.
        /// Restituisce il numero di righe eliminate.
        /// </summary>
        Task<int> EliminaPerIdAsync(IReadOnlyCollection<long> ids);
        Task<List<Prenotazione>> GetPrenotazioniByDataAsync(DateOnly data);
        Task<List<Prenotazione>> GetAllPrenotazioniAsync();
        Task<List<FasciaOraria>> GetFasceOrarieByDayAsync(DayOfWeek giorno);
    }
}
