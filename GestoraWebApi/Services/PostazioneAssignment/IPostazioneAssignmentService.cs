using GestoraWebApi.Models;
using GestoraWebApi.Services.Prenotazioni.DTOs;

namespace GestoraWebApi.Services.PostazioneAssignment
{
    public interface IPostazioneAssignmentService
    {
        Task<List<PostazioneAssegnata>> AssegnaPostazioneDisponibileAsync(PrenotazioneCreateDTO dto, long? excludePrenotazioneId = null);
    }
}
