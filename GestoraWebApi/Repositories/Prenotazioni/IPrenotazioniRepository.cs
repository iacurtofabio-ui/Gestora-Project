using GestoraWebApi.Models;

namespace GestoraWebApi.Repositories.Prenotazioni
{
    public interface IPrenotazioniRepository : IRepository<Prenotazione>
    {
        Task<Prenotazione?> GetTrackedByIdAsync(long id);
        Task<List<Prenotazione>> GetPrenotazioniByDataAsync(DateOnly data);
        Task<List<Prenotazione>> GetAllPrenotazioniAsync();
        Task<List<FasciaOraria>> GetFasceOrarieByDayAsync(DayOfWeek giorno);
    }
}
