using GestoraWebApi.Models;

namespace GestoraWebApi.Repositories.Postazioni
{
    public interface IPostazioneRepository
    {
        IQueryable<Postazione> GetAllQueryable();
        Task AddAsync(Postazione entity);
        /// <summary>Tavoli attivi, senza lo storico delle prenotazioni (REV-023).</summary>
        Task<List<Postazione>> GetPostazioniAttiveAsync();

        /// <summary>
        /// Tavoli attivi con le righe di PrenotazioniPostazioni caricate. Da usare solo dove
        /// quel dato serve davvero: e' pesante e cresce con lo storico.
        /// </summary>
        Task<List<Postazione>> GetPostazioniAttiveConPrenotazioniAsync();
        Task<List<Postazione>> GetPostazioniDisponibiliAsync();
        Task<Postazione> GetByIdAsync(long id);
        Task<List<Postazione>> GetPostazioniPerZonaAsync(long zonaId);
        /// <summary>
        /// REV-099: dice se la postazione e' impegnata da <paramref name="daData"/> in poi.
        /// Sostituisce il vecchio HasPrenotazioniAsync, che guardava l'intero storico.
        /// </summary>
        Task<bool> HasPrenotazioniFutureAsync(long postazioneId, DateOnly daData);
        Task UpdateAsync(Postazione postazione);
        Task DeleteAsync(Postazione postazione);


    }
}
