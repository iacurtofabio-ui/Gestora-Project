using GestoraWebApi.Models;
using GestoraWebApi.Services.FasciaOrarie.DTOs;
using GestoraWebApi.Services.Postazioni.DTOs;

namespace GestoraWebApi.Services.FasciaOrarie
{
    public interface IFasciaOrariaService : IService<FasciaOrariaDTO>
    {
        Task AddAsync(FasciaOrariaDTO dto);
        Task<FasciaOraria> GetByIdAsync(long id);
        Task UpdateAsync(FasciaOrariaDTO dto);
        Task DeleteAsync(long fasciaId);
        Task<FasciaOrariaDTO> GetFasciaOrariaDTOByIdAsync(long id);
        Task<List<FasciaOrariaDTO>> GetFasceAttiveAsync();
        Task<List<FasciaOrariaDTO>> GetFasceByGiornoAsync(DayOfWeek giorno);
        Task<DisponibilitaFasciaDTO> VerificaDisponibilitaPerFasciaAsync(long fasciaId, DateOnly data);

    }
}