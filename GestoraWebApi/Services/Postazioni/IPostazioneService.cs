using GestoraWebApi.Models;
using GestoraWebApi.Services.Postazioni.DTOs;

namespace GestoraWebApi.Services.Postazioni
{
    public interface IPostazioneService : IService<PostazioneDTO>
    {
        Task AddAsync(PostazioneDTO dto);
        Task<Postazione> GetByIdAsync(long id);
        Task<PostazioneDTO> GetPostazioneDTOByIdAsync(long id);
        Task UpdateAsync(PostazioneUpdateDTO dto);
        Task<List<PostazioneDTO>> GetPostazioniAttiveAsync();
        Task<List<PostazioniDisponibiliDTO>> GetPostazioniDisponibiliAsync();
        Task<List<PostazioneDTO>> GetPostazioniPerZonaAsync(long zonaId);
        Task<RiepilogoSalaDTO> GetRiepilogoSalaAsync();
        Task AssociaPostazioneAZonaAsync(long postazioneId, long zonaId);
        Task DeleteAsync(long postazioneId);

    }
}
