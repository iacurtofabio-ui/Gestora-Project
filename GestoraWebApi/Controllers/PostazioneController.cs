using GestoraWebApi.Auth;
using GestoraWebApi.Services.Postazioni;
using GestoraWebApi.Services.Postazioni.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestoraWebApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class PostazioneController : ControllerBase
    {
        private readonly IPostazioneService _postazioneService;
        private readonly ILogger<PostazioneController> _logger;

        public PostazioneController(IPostazioneService postazioneService,
                                    ILogger<PostazioneController> logger)
        {
            _postazioneService = postazioneService;
            _logger = logger;
        }

        /// <summary>Crea una nuova postazione. Capienza valida: 2, 4 o 8 posti. Solo Admin.</summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpPost("crea-postazione")]
        public async Task<IActionResult> CreatePostazioniAsync([FromBody] PostazioneDTO dto)
        {
            await _postazioneService.AddAsync(dto);

            _logger.LogInformation("[{Controller}] - [{Method}]: Postazione creata con successo - {Data}",
                nameof(PostazioneController), nameof(CreatePostazioniAsync), DateTime.Now);

            return Created("", new { Message = "Postazione creata con successo" });
        }

        /// <summary>Restituisce il dettaglio di una postazione tramite ID. Solo Admin e Staff.</summary>
        [Authorize(Roles = Roles.AdminOrStaff)]
        [HttpGet("get-postazione-id")]
        public async Task<IActionResult> GetPostazioneByIdAsync(long id)
        {
            var postazioneDto = await _postazioneService.GetPostazioneDTOByIdAsync(id);

            if (postazioneDto == null)
                return NotFound(new { message = $"Postazione con ID {id} non trovata." });

            return Ok(postazioneDto);
        }

        /// <summary>Restituisce tutte le postazioni attive nel sistema.</summary>
        [Authorize(Roles = Roles.AdminOrStaffOrCliente)]
        [HttpGet("get-postazioni-attive")]
        public async Task<IActionResult> GetAllPostazioniAttiveAsync()
        {
            var postazioni = await _postazioneService.GetPostazioniAttiveAsync();

            if (postazioni == null || !postazioni.Any())
                return NotFound(new { Message = "Nessuna postazione attiva trovata." });

            return Ok(postazioni);
        }

        /// <summary>Restituisce le postazioni disponibili (attive e senza prenotazioni attive in corso).</summary>
        [Authorize(Roles = Roles.AdminOrStaffOrCliente)]
        [HttpGet("get-postazioni-disponibili")]
        public async Task<IActionResult> GetPostazioniDisponibiliAsync()
        {
            var postazioni = await _postazioneService.GetPostazioniDisponibiliAsync();

            if (postazioni == null || !postazioni.Any())
                return NotFound(new { Message = "Nessuna postazione disponibile trovata." });

            return Ok(postazioni);
        }

        /// <summary>Restituisce tutte le postazioni associate a una zona specifica.</summary>
        [Authorize(Roles = Roles.AdminOrStaffOrCliente)]
        [HttpGet("get-postazioni-per-zona")]
        public async Task<IActionResult> GetPostazioniPerZonaAsync(long zonaId)
        {
            var postazioni = await _postazioneService.GetPostazioniPerZonaAsync(zonaId);

            if (postazioni == null || !postazioni.Any())
                return NotFound(new { Message = $"Nessuna postazione trovata per la zona {zonaId}." });

            return Ok(postazioni);
        }

        /// <summary>Aggiorna i dati di una postazione esistente. Solo Admin.</summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpPut("update-postazione")]
        public async Task<IActionResult> UpdatePostazioneAsync([FromBody] PostazioneUpdateDTO dto)
        {
            await _postazioneService.UpdateAsync(dto);

            _logger.LogInformation("[{Controller}] - [{Method}]: Postazione aggiornata con successo - {Data}",
                nameof(PostazioneController), nameof(UpdatePostazioneAsync), DateTime.Now);

            return Ok(new { Message = "Postazione aggiornata con successo" });
        }

        /// <summary>Associa una postazione a una zona. Entrambe devono essere attive e senza prenotazioni in corso. Solo Admin.</summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpPut("associa-postazione-a-zona")]
        public async Task<IActionResult> AssociaPostazioneAZonaAsync([FromBody] AssociaPostazioniDTO dto)
        {
            await _postazioneService.AssociaPostazioneAZonaAsync(dto.PostazioneId, dto.ZonaId);

            _logger.LogInformation(
                "[{Controller}] - [{Method}]: Postazione {PostazioneId} associata alla zona {ZonaId} - {Data}",
                nameof(PostazioneController), nameof(AssociaPostazioneAZonaAsync), dto.PostazioneId, dto.ZonaId, DateTime.Now);

            return Ok(new { Message = "Postazione associata correttamente alla zona." });
        }

        /// <summary>Elimina una postazione. Non eliminabile se ha prenotazioni attive o in corso. Solo Admin.</summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpDelete("delete-postazione")]
        public async Task<IActionResult> DeletePostazioneAsync(long id)
        {
            await _postazioneService.DeleteAsync(id);

            _logger.LogInformation("[{Controller}] - [{Method}] Postazione {Id} eliminata con successo - {Data}",
                nameof(PostazioneController), nameof(DeletePostazioneAsync), id, DateTime.Now);

            return Ok(new { Message = $"Postazione {id} eliminata con successo." });
        }
    }
}