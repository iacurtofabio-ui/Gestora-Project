using GestoraWebApi.Models;

namespace GestoraWebApi.Repositories.Postazioni
{
    public interface IPostazioneRepository
    {
        IQueryable<Postazione> GetAllQueryable();
        Task AddAsync(Postazione entity);
        Task<List<Postazione>> GetPostazioniAttiveAsync();
        Task<List<Postazione>> GetPostazioniDisponibiliAsync();
        Task<Postazione> GetByIdAsync(long id);
        Task<List<Postazione>> GetPostazioniPerZonaAsync(long zonaId);
        Task<bool> HasPrenotazioniAsync(long postazioneId);
        Task UpdateAsync(Postazione postazione);
        Task DeleteAsync(Postazione postazione);


    }
}
