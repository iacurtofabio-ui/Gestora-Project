using GestoraWebApi.Auth;
using GestoraWebApi.Services.FasciaOrarie;
using GestoraWebApi.Services.FasciaOrarie.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestoraWebApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class FasceOrarieController : ControllerBase
    {
        private readonly IFasciaOrariaService _fasciaOrariaService;
        private readonly ILogger<FasceOrarieController> _logger;

        public FasceOrarieController(IFasciaOrariaService fasciaOrariaService,
                                     ILogger<FasceOrarieController> logger)
        {
            _fasciaOrariaService = fasciaOrariaService;
            _logger = logger;
        }

        /// <summary>Crea una nuova fascia oraria. Valida sovrapposizioni con fasce esistenti nello stesso giorno. Solo Admin.</summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpPost("crea-fascia")]
        public async Task<IActionResult> CreateFasciaOrariaAsync([FromBody] FasciaOrariaDTO dto)
        {
            await _fasciaOrariaService.AddAsync(dto);

            _logger.LogInformation("[{Controller}] - [{Method}]: Fascia oraria creata con successo - {Data}",
                nameof(FasceOrarieController), nameof(CreateFasciaOrariaAsync), DateTime.Now);

            return Created("", new { message = "Fascia oraria creata con successo" });
        }

        /// <summary>Restituisce il dettaglio di una fascia oraria tramite ID. Solo Admin e Staff.</summary>
        [Authorize(Roles = Roles.AdminOrStaff)]
        [HttpGet("get-fascia-id/{id}")]
        public async Task<IActionResult> GetFasciaOrariaByIdAsync(long id)
        {
            var fasciaDto = await _fasciaOrariaService.GetFasciaOrariaDTOByIdAsync(id);

            if (fasciaDto == null)
                return NotFound(new { message = $"Fascia oraria con ID {id} non trovata." });

            return Ok(fasciaDto);
        }

        /// <summary>Restituisce tutte le fasce orarie attive. Accessibile a tutti i ruoli autenticati.</summary>
        [Authorize(Roles = Roles.AdminOrStaffOrCliente)]
        [HttpGet("fasce-attive")]
        public async Task<IActionResult> GetFasceAttive()
        {
            var fasce = await _fasciaOrariaService.GetFasceAttiveAsync();

            _logger.LogInformation("[{Controller}] - [{Method}]: Recuperate {Count} fasce orarie attive - {Data}",
                nameof(FasceOrarieController), nameof(GetFasceAttive), fasce.Count, DateTime.Now);

            return Ok(fasce);
        }

        /// <summary>
        /// Restituisce le fasce orarie attive per un giorno specifico della settimana.
        /// Utile per il calendario prenotazioni nel frontend.
        /// GiornoSettimana: 0=Domenica, 1=Luned�, 2=Marted�, 3=Mercoled�, 4=Gioved�, 5=Venerd�, 6=Sabato
        /// </summary>
        [Authorize(Roles = Roles.AdminOrStaffOrCliente)]
        [HttpGet("fasce-per-giorno")]
        public async Task<IActionResult> GetFasceByGiorno([FromQuery] DayOfWeek giorno)
        {
            var fasce = await _fasciaOrariaService.GetFasceByGiornoAsync(giorno);

            _logger.LogInformation("[{Controller}] - [{Method}]: Recuperate {Count} fasce per giorno {Giorno} - {Data}",
                nameof(FasceOrarieController), nameof(GetFasceByGiorno), fasce.Count, giorno, DateTime.Now);

            return Ok(fasce);
        }

        /// <summary>Verifica disponibilita posti per una fascia oraria in una data specifica (yyyy-MM-dd). La data deve corrispondere al giorno della settimana della fascia.</summary>
        [Authorize(Roles = Roles.AdminOrStaffOrCliente)]
        [HttpGet("fasce-disponibili")]
        public async Task<IActionResult> VerificaDisponibilitaPerFascia(long fasciaId, [FromQuery] string data)
        {
            if (!DateOnly.TryParseExact(data,
                new[] { "yyyy-MM-dd", "dd-MM-yyyy", "dd/MM/yyyy" },
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out var parsedDate))
            {
                return BadRequest(new
                {
                    message = "Formato data non valido. I formati accettati sono: yyyy-MM-dd, dd-MM-yyyy, dd/MM/yyyy",
                    esempio = "2025-08-12 oppure 12-08-2025 oppure 12/08/2025"
                });
            }

            var postiDisponibili = await _fasciaOrariaService.VerificaDisponibilitaPerFasciaAsync(fasciaId, parsedDate);
            return Ok(postiDisponibili);
        }

        /// <summary>Aggiorna i dati di una fascia oraria esistente. Solo Admin.</summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpPut("update-fascia")]
        public async Task<IActionResult> UpdateFasciaOrariaAsync([FromBody] FasciaOrariaDTO dto)
        {
            await _fasciaOrariaService.UpdateAsync(dto);

            _logger.LogInformation("[{Controller}] - [{Method}]: Fascia oraria aggiornata con successo - {Data}",
                nameof(FasceOrarieController), nameof(UpdateFasciaOrariaAsync), DateTime.Now);

            return Ok(new { message = "Fascia oraria aggiornata con successo" });
        }

        /// <summary>Elimina una fascia oraria. Non eliminabile se ha prenotazioni associate. Solo Admin.</summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpDelete("delete-fascia")]
        public async Task<IActionResult> DeleteFasciaAsync(long id)
        {
            await _fasciaOrariaService.DeleteAsync(id);

            _logger.LogInformation("[{Controller}] - [{Method}]: Fascia oraria eliminata con successo - {Data}",
                nameof(FasceOrarieController), nameof(DeleteFasciaAsync), DateTime.Now);

            return Ok(new { message = "Fascia oraria eliminata con successo" });
        }

        /// <summary>Restituisce tutte le fasce orarie (attive e non). Solo Admin e Staff.</summary>
        [Authorize(Roles = Roles.AdminOrStaff)]
        [HttpGet("get-all-fasce")]
        public async Task<IActionResult> GetAllFasce()
        {
            var fasce = await _fasciaOrariaService.GetAllFasceAsync();

            _logger.LogInformation("[{Controller}] - [{Method}]: Recuperate {Count} fasce totali - {Data}",
                nameof(FasceOrarieController), nameof(GetAllFasce), fasce.Count, DateTime.Now);

            return Ok(fasce);
        }

        /// <summary>Attiva o disattiva una fascia oraria. Solo Admin.</summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpPatch("update-stato/{id}")]
        public async Task<IActionResult> UpdateStatoAsync(long id, [FromQuery] bool attiva)
        {
            await _fasciaOrariaService.UpdateStatoAsync(id, attiva);

            _logger.LogInformation("[{Controller}] - [{Method}]: Stato fascia {Id} aggiornato a {Attiva} - {Data}",
                nameof(FasceOrarieController), nameof(UpdateStatoAsync), id, attiva, DateTime.Now);

            return Ok(new { message = $"Stato fascia aggiornato con successo." });
        }
    }
}