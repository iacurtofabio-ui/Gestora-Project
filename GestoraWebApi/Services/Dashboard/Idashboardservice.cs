using GestoraWebApi.Services.Dashboard.DTOs;

namespace GestoraWebApi.Services.Dashboard
{
    public interface IDashboardService
    {
        /// <summary>
        /// Restituisce la panoramica operativa di un giorno specifico.
        /// Include contatori per stato, coperti totali, stato postazioni
        /// e dettaglio per fascia oraria.
        /// </summary>
        Task<DashboardGiornalieroDTO> GetDashboardGiornalieroAsync(DateOnly data);

        /// <summary>
        /// Restituisce la panoramica aggregata della settimana
        /// che inizia il giorno indicato (7 giorni).
        /// Include KPI: tasso annullamento e tasso no-show.
        /// </summary>
        Task<DashboardSettimanaleDTO> GetDashboardSettimanaleAsync(DateOnly dataInizio);
    }
}