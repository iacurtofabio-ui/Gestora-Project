using GestoraWebApi.Models;
using GestoraWebApi.Services.Prenotazioni.DTOs;

namespace GestoraWebApi.Services.PostazioneAssignment
{
    public interface IPostazioneAssignmentService
    {
        Task<List<Postazione>> AssegnaPostazioneDisponibileAsync(PrenotazioneCreateDTO dto, long? excludePrenotazioneId = null);
        List<List<Postazione>> TrovaCombinazioniDisponibili(List<Postazione> postazioniLibere, int numeroCoperti);
    }
}
