using GestoraWebApi.Auth;
using GestoraWebApi.Services.Disponibilita;
using GestoraWebApi.Services.Prenotazioni;
using GestoraWebApi.Services.Prenotazioni.DTOs;
using GestoraWebApi.Services.PrenotazioniPostazioni;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GestoraWebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PrenotazioneController : ControllerBase
    {
        private readonly IPrenotazioniService _prenotazioniService;
        private readonly IDisponibilitaService _disponibilitaService;
        private readonly ILogger<PrenotazioneController> _logger;

        public PrenotazioneController(IPrenotazioniService prenotazioneService,
                                      IDisponibilitaService disponibilitaService,
                                      ILogger<PrenotazioneController> logger)
        {
            _prenotazioniService = prenotazioneService;
            _disponibilitaService = disponibilitaService;
            _logger = logger;
        }

        /// <summary>Crea una nuova prenotazione per l'utente autenticato. Assegna automaticamente le postazioni disponibili.</summary>
        [Authorize(Roles = Roles.AdminOrStaffOrCliente)]
        [HttpPost("crea-prenotazione")]
        public async Task<IActionResult> CreatePrenotazioniAsync([FromBody] PrenotazioneCreateDTO dto)
        {
            await _prenotazioniService.AddAsync(dto);

            _logger.LogInformation("[{Controller}] - [{Method}]: Prenotazione creata con successo - {Data}",
                nameof(PrenotazioneController), nameof(CreatePrenotazioniAsync), DateTime.Now);

            return Created("", new { Message = "Prenotazione creata con successo" });
        }

        /// <summary>Restituisce il dettaglio di una prenotazione tramite ID.</summary>
        [Authorize(Roles = Roles.AdminOrStaff)]
        [HttpGet("get-prenotazione")]
        public async Task<IActionResult> GetById(long id)
        {
            var prenotazione = await _prenotazioniService.GetByIdAsync(id);

            if (prenotazione == null)
                return NotFound(new { message = $"Prenotazione con ID {id} non trovata." });

            return Ok(prenotazione);
        }

        /// <summary>Restituisce la lista paginata di tutte le prenotazioni con filtri opzionali per data e stato.</summary>
        [Authorize(Roles = Roles.AdminOrStaff)]
        [HttpGet("get-all-prenotazioni")]
        public async Task<IActionResult> GetAllPrenotazioni([FromQuery] PrenotazioniQueryParams query)
        {
            var result = await _prenotazioniService.GetAllPrenotazioniAsync(query);
            return Ok(result);
        }

        /// <summary>Restituisce tutte le prenotazioni per una data specifica (formato yyyy-MM-dd).</summary>
        [Authorize(Roles = Roles.AdminOrStaff)]
        [HttpGet("get-prenotazioni-by-data")]
        public async Task<IActionResult> GetPrenotazioniByData(DateOnly data)
        {
            var prenotazioni = await _prenotazioniService.GetPrenotazioniByDataAsync(data);

            if (prenotazioni == null || !prenotazioni.Any())
                return NotFound(new { message = $"Nessuna prenotazione trovata per la data {data}." });

            return Ok(prenotazioni);
        }

        /// <summary>Restituisce tutte le prenotazioni dell'utente autenticato.</summary>
        [Authorize(Roles = Roles.Cliente)]
        [HttpGet("get-mie-prenotazioni")]
        public async Task<IActionResult> GetMiePrenotazioni()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();

            var prenotazioni = await _prenotazioniService.GetMiePrenotazioniAsync(userId);

            return Ok(prenotazioni);
        }

        /// <summary>Aggiorna i dati di una prenotazione esistente. Solo se in stato Attiva e di proprietà dell'utente.</summary>
        [Authorize(Roles = Roles.AdminOrStaffOrCliente)]
        [HttpPut("update-prenotazione")]
        public async Task<IActionResult> UpdatePrenotazione(long id, [FromBody] PrenotazioneCreateDTO dto)
        {
            await _prenotazioniService.UpdateAsync(id, dto);

            _logger.LogInformation("[{Controller}] - [{Method}]: Prenotazione {Id} aggiornata con successo - {Data}",
                nameof(PrenotazioneController), nameof(UpdatePrenotazione), id, DateTime.Now);

            return Ok(new { message = "Prenotazione aggiornata con successo." });
        }

        /// <summary>Elimina una prenotazione. Solo se in stato Attiva o Annullata.</summary>
        [Authorize(Roles = Roles.AdminOrStaff)]
        [HttpDelete("delete-prenotazione")]
        public async Task<IActionResult> DeletePrenotazione(long id)
        {
            await _prenotazioniService.DeleteAsync(id);

            _logger.LogInformation("[{Controller}] - [{Method}]: Prenotazione {Id} eliminata con successo - {Data}",
                nameof(PrenotazioneController), nameof(DeletePrenotazione), id, DateTime.Now);

            return Ok(new { message = "Prenotazione eliminata con successo." });
        }

        /// <summary>Conferma una prenotazione: transizione stato Attiva → InCorso. Solo Admin e Staff.</summary>
        [Authorize(Roles = Roles.AdminOrStaff)]
        [HttpPatch("conferma-prenotazione")]
        public async Task<IActionResult> ConfermaPrenotazione(long id)
        {
            await _prenotazioniService.ConfermaPrenotazioneAsync(id);

            _logger.LogInformation("[{Controller}] - [{Method}]: Prenotazione {Id} confermata con successo - {Data}",
                nameof(PrenotazioneController), nameof(ConfermaPrenotazione), id, DateTime.Now);

            return Ok(new { message = "Prenotazione confermata con successo." });
        }

        /// <summary>Completa una prenotazione: transizione InCorso → Completata. Verificabile solo dopo la fine della fascia oraria.</summary>
        [Authorize(Roles = Roles.AdminOrStaff)]
        [HttpPatch("completa-prenotazione")]
        public async Task<IActionResult> CompletePrenotazione(long id)
        {
            await _prenotazioniService.CompletePrenotazioneAsync(id);

            _logger.LogInformation("[{Controller}] - [{Method}]: Prenotazione {Id} completata manualmente - {Data}",
                nameof(PrenotazioneController), nameof(CompletePrenotazione), id, DateTime.Now);

            return Ok(new { message = "Prenotazione completata con successo." });
        }

        /// <summary>Annulla una prenotazione. Non annullabile se già Completata.</summary>
        [Authorize(Roles = Roles.AdminOrStaffOrCliente)]
        [HttpPatch("annulla-prenotazione")]
        public async Task<IActionResult> AnnullaPrenotazione(long id)
        {
            await _prenotazioniService.AnnullaPrenotazioneAsync(id);

            _logger.LogInformation("[{Controller}] - [{Method}]: Prenotazione {Id} annullata con successo - {Data}",
                nameof(PrenotazioneController), nameof(AnnullaPrenotazione), id, DateTime.Now);

            return Ok(new { message = "Prenotazione annullata con successo." });
        }

        /// <summary>Endpoint pubblico (no login richiesto). Verifica disponibilità fasce e postazioni per data e numero coperti.</summary>
        [AllowAnonymous]
        [HttpPost("check-disponibilita")]
        public async Task<IActionResult> CheckDisponibilita([FromBody] CheckDisponibilitaDTO dto)
        {
            var disponibilita = await _disponibilitaService.CheckDisponibilitaAsync(dto);

            if (disponibilita == null || !disponibilita.Fasce.Any())
                return NotFound(new { message = $"Nessuna fascia trovata per la data {dto.DataPrenotazione}." });

            return Ok(disponibilita);
        }
    }
}