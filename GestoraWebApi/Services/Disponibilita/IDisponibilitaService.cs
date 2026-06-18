using GestoraWebApi.Services.PrenotazioniPostazioni;

namespace GestoraWebApi.Services.Disponibilita
{
    public interface IDisponibilitaService
    {
        Task<DisponibilitaResponseDTO> CheckDisponibilitaAsync(CheckDisponibilitaDTO dto);
    }
}
