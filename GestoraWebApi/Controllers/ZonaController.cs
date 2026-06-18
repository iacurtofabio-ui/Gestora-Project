using GestoraWebApi.Auth;
using GestoraWebApi.Services.Zone;
using GestoraWebApi.Services.Zone.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestoraWebApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ZonaController : ControllerBase
    {
        private readonly IZonaService _zonaService;
        private readonly ILogger<ZonaController> _logger;

        public ZonaController(ILogger<ZonaController> logger, IZonaService zonaService)
        {
            _logger = logger;
            _zonaService = zonaService;
        }

        /// <summary>Restituisce il dettaglio di una zona tramite ID. Solo Admin e Staff.</summary>
        [Authorize(Roles = Roles.AdminOrStaff)]
        [HttpGet("get-zona/{id}")]
        public async Task<IActionResult> GetZonaByIdAsync(long id)
        {
            var zona = await _zonaService.GetByIdAsync(id);

            if (zona == null)
                return NotFound(new { message = $"Zona con ID {id} non trovata." });

            return Ok(zona);
        }

        /// <summary>Crea una nuova zona. Il nome deve essere univoco. Solo Admin.</summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpPost("crea-zona")]
        public async Task<IActionResult> CreateZonaAsync([FromBody] ZonaDTO dto)
        {
            await _zonaService.AddAsync(dto);

            _logger.LogInformation("[{Controller}] - [{Method}]: Zona creata con successo - {Data}",
                nameof(ZonaController), nameof(CreateZonaAsync), DateTime.Now);

            return Created("", new { message = "Zona creata con successo" });
        }

        /// <summary>Aggiorna nome e stato di una zona esistente. Solo Admin.</summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpPut("update-zona")]
        public async Task<IActionResult> UpdateZonaAsync([FromBody] ZonaDTO dto)
        {
            await _zonaService.UpdateAsync(dto);

            _logger.LogInformation("[{Controller}] - [{Method}]: Zona aggiornata con successo - {Data}",
                nameof(ZonaController), nameof(UpdateZonaAsync), DateTime.Now);

            return Ok(new { message = "Zona aggiornata con successo" });
        }

        /// <summary>Restituisce tutte le zone attive. Accessibile a tutti i ruoli autenticati.</summary>
        [Authorize(Roles = Roles.AdminOrStaffOrCliente)]
        [HttpGet("get-zone-attive")]
        public async Task<IActionResult> GetAllZoneAttiveAsync()
        {
            var zoneAttive = await _zonaService.GetAllZoneAttiveAsync();

            if (zoneAttive == null || !zoneAttive.Any())
                return NotFound(new { message = "Nessuna zona attiva trovata." });

            return Ok(zoneAttive);
        }

        /// <summary>Restituisce tutte le zone (attive e non). Solo Admin e Staff.</summary>
        [Authorize(Roles = Roles.AdminOrStaff)]
        [HttpGet("get-all-zone")]
        public async Task<IActionResult> GetAllZoneAsync()
        {
            var zone = await _zonaService.GetAllZoneAsync();

            if (zone == null || !zone.Any())
                return NotFound(new { message = "Nessuna zona trovata." });

            return Ok(zone);
        }

        /// <summary>Verifica se una zona è assegnata ad almeno una postazione. Usato prima di eliminarla. Solo Admin e Staff.</summary>
        [Authorize(Roles = Roles.AdminOrStaff)]
        [HttpGet("is-zona-usata")]
        public async Task<IActionResult> VerificaZonaUsataAsync(long id)
        {
            var zona = await _zonaService.GetByIdAsync(id);

            if (zona == null)
                return NotFound(new { message = $"Zona con ID {id} non trovata." });

            var isUsata = await _zonaService.IsZonaUsataAsync(id);

            return Ok(new { id, isUsata });
        }

        /// <summary>Attiva o disattiva una zona tramite il parametro 'attiva'. Solo Admin.</summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpPatch("update-stato/{id}")]
        public async Task<IActionResult> UpdateStatoZonaAsync(long id, [FromQuery] bool attiva)
        {
            await _zonaService.UpdateStatoZonaAsync(id, attiva);

            _logger.LogInformation("[{Controller}] - [{Method}]: Stato della zona {Id} aggiornato a {Attiva} - {Data}",
                nameof(ZonaController), nameof(UpdateStatoZonaAsync), id, attiva, DateTime.Now);

            return Ok(new { message = $"Zona {(attiva ? "attivata" : "disattivata")} con successo" });
        }

        /// <summary>Elimina una zona. Non eliminabile se ha postazioni associate. Solo Admin.</summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpDelete("delete-zona/{id}")]
        public async Task<IActionResult> DeleteZonaAsync(long id)
        {
            await _zonaService.DeleteAsync(id);

            _logger.LogInformation("[{Controller}] - [{Method}]: Zona eliminata con successo - {Data}",
                nameof(ZonaController), nameof(DeleteZonaAsync), DateTime.Now);

            return Ok(new { message = "Zona eliminata con successo" });
        }
    }
}