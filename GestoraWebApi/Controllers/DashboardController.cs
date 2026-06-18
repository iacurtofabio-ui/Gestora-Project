using GestoraWebApi.Auth;
using GestoraWebApi.Services.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestoraWebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = Roles.AdminOrStaff)]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(IDashboardService dashboardService,
                                   ILogger<DashboardController> logger)
        {
            _dashboardService = dashboardService;
            _logger = logger;
        }

        /// <summary>
        /// Restituisce la panoramica operativa del giorno indicato.
        /// Se non si passa la data, usa oggi.
        /// </summary>
        [HttpGet("giornaliera")]
        public async Task<IActionResult> GetGiornaliera([FromQuery] DateOnly? data)
        {
            var targetData = data ?? DateOnly.FromDateTime(DateTime.UtcNow);

            _logger.LogInformation(
                "[{Controller}] - [{Method}]: Dashboard giornaliera richiesta per {Data}",
                nameof(DashboardController), nameof(GetGiornaliera), targetData);

            var result = await _dashboardService.GetDashboardGiornalieroAsync(targetData);
            return Ok(result);
        }

        /// <summary>
        /// Restituisce la panoramica aggregata della settimana (7 giorni)
        /// che inizia dalla data indicata.
        /// Se non si passa la data, usa il lunedì della settimana corrente.
        /// </summary>
        [HttpGet("settimanale")]
        public async Task<IActionResult> GetSettimanale([FromQuery] DateOnly? dataInizio)
        {
            // Se non specificata, partiamo dal lunedì della settimana corrente
            var oggi = DateOnly.FromDateTime(DateTime.UtcNow);
            var lunedi = oggi.AddDays(-(int)oggi.DayOfWeek == 0 ? 6 : (int)oggi.DayOfWeek - 1);
            var target = dataInizio ?? lunedi;

            _logger.LogInformation(
                "[{Controller}] - [{Method}]: Dashboard settimanale richiesta da {Data}",
                nameof(DashboardController), nameof(GetSettimanale), target);

            var result = await _dashboardService.GetDashboardSettimanaleAsync(target);
            return Ok(result);
        }
    }
}