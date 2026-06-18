using GestoraWebApi.Common;
using GestoraWebApi.Services.Prenotazioni.DTOs;

namespace GestoraWebApi.Services.Prenotazioni
{
    public interface IPrenotazioniService
    {
        Task<PrenotazioneDTO> GetByIdAsync(long id);
        Task<List<PrenotazioneDTO>> GetPrenotazioniByDataAsync(DateOnly data);
        Task<PagedResult<PrenotazioneDTO>> GetAllPrenotazioniAsync(PrenotazioniQueryParams query);
        Task AddAsync(PrenotazioneCreateDTO dto);
        Task UpdateAsync(long id, PrenotazioneCreateDTO dto);
        Task DeleteAsync(long id);
        Task ConfermaPrenotazioneAsync(long id);
        Task CompletePrenotazioneAsync(long id);
        Task AnnullaPrenotazioneAsync(long id);
        Task AutomaticCompletPrenotazioniAsync();
        Task AutomaticDeletePrenotazioniAsync();
        Task<List<PrenotazioneDTO>> GetMiePrenotazioniAsync(string userId);
    }
}
