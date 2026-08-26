using AutoMapper;
using GestoraWebApi.Auth;
using GestoraWebApi.Common;
using GestoraWebApi.Context;
using GestoraWebApi.Enums;
using GestoraWebApi.Extensions;
using GestoraWebApi.Models;
using GestoraWebApi.Repositories.FasciaOrarie;
using GestoraWebApi.Repositories.Prenotazioni;
using GestoraWebApi.Repositories.Zone;
using GestoraWebApi.Services.LogActivity;
using GestoraWebApi.Services.PostazioneAssignment;
using GestoraWebApi.Services.Prenotazioni.DTOs;
using GestoraWebApi.Services.PrenotazioniPostazioni;
using Microsoft.EntityFrameworkCore;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace GestoraWebApi.Services.Prenotazioni
{
    public class PrenotazioniService : IPrenotazioniService
    {
        private readonly IPrenotazioniRepository _prenotazioniRepository;
        private readonly IPostazioneAssignmentService _postazioneAssignmentService;
        private readonly IFasciaOrariaRepository _fasciaOrariaRepository;
        private readonly IZonaRepository _zonaRepository;
        private readonly IMapper _mapper;
        private readonly GestoraContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<PrenotazioniService> _logger;
        private readonly ILogActivityService _logActivity;
        private IPrenotazioniRepository object1;
        private IPostazioneAssignmentService object2;
        private IFasciaOrariaRepository object3;
        private IMapper object4;
        private GestoraContext context;
        private IHttpContextAccessor object5;
        private IZonaRepository object6;
        private ILogger<PrenotazioniService> object7;

        public PrenotazioniService(IPrenotazioniRepository prenotazioniRepository,
                                   IPostazioneAssignmentService postazioneAssignmentService,
                                   IFasciaOrariaRepository fasciaOrariaRepository,
                                   IMapper mapper,
                                   GestoraContext context,
                                   IHttpContextAccessor httpContextAccessor,
                                   IZonaRepository zonaRepository,
                                   ILogger<PrenotazioniService> logger,
                                   ILogActivityService logActivity)
        {
            _prenotazioniRepository = prenotazioniRepository;
            _postazioneAssignmentService = postazioneAssignmentService;
            _fasciaOrariaRepository = fasciaOrariaRepository;
            _mapper = mapper;
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _zonaRepository = zonaRepository;
            _logger = logger;
            _logActivity = logActivity;
        }

        public PrenotazioniService(IPrenotazioniRepository object1, IPostazioneAssignmentService object2, IFasciaOrariaRepository object3, IMapper object4, GestoraContext context, IHttpContextAccessor object5, IZonaRepository object6, ILogger<PrenotazioniService> object7)
        {
            this.object1 = object1;
            this.object2 = object2;
            this.object3 = object3;
            this.object4 = object4;
            this.context = context;
            this.object5 = object5;
            this.object6 = object6;
            this.object7 = object7;
        }

        public async Task AddAsync(PrenotazioneCreateDTO dto)
        {
            var userId = GetAuthenticatedUserId();

            await ValidatePrenotazioneAsync(dto);

            if (IsSelfServiceCliente())
                await GuardUnaPrenotazioneAlGiornoAsync(userId, dto.DataPrenotazione);

            var postazioniAssegnate = await _postazioneAssignmentService.AssegnaPostazioneDisponibileAsync(dto);

            var prenotazione = new Prenotazione
            {
                UserId = userId,
                DataPrenotazione = (DateOnly)dto.DataPrenotazione,
                NumeroCoperti = dto.NumeroCoperti,
                FasciaOrariaId = dto.FasciaOrariaId,
                NomeCliente = dto.NomeCliente,
                Note = dto.Note,
                Stato = StatoPrenotazione.Attiva,
            };

            prenotazione.PrenotazioniPostazioni = postazioniAssegnate
                .Select(p => new PrenotazionePostazione
                {
                    PostazioneId = p.Id,
                    Prenotazione = prenotazione
                })
                .ToList();

            await _prenotazioniRepository.AddAsync(prenotazione);
            await _logActivity.LogAsync(userId, $"Creata prenotazione per data {dto.DataPrenotazione:yyyy-MM-dd}, {dto.NumeroCoperti} coperti", GetIpAddress());
        }

        public async Task DeleteAsync(long id)
        {
            var prenotazione = await _prenotazioniRepository.GetByIdAsync(id);

            if (prenotazione == null)
                throw new KeyNotFoundException($"Prenotazione con Id {id} non trovata.");

            if (prenotazione.Stato != StatoPrenotazione.Attiva && prenotazione.Stato != StatoPrenotazione.Annullata)
                throw new InvalidOperationException($"Non è possibile eliminare una prenotazione nello stato {prenotazione.Stato}.");

            await _prenotazioniRepository.DeleteAsync(prenotazione);
            await _logActivity.LogAsync(GetAuthenticatedUserId(), $"Eliminata prenotazione ID {id}", GetIpAddress());
        }

        public async Task<PrenotazioneDTO> GetByIdAsync(long id)
        {
            var prenotazione = await _prenotazioniRepository.GetByIdAsync(id);

            if (prenotazione == null)
                throw new KeyNotFoundException("Prenotazione non trovata.");

            return _mapper.Map<PrenotazioneDTO>(prenotazione);
        }

        public async Task UpdateAsync(long id, PrenotazioneCreateDTO dto)
        {
            var prenotazione = await _prenotazioniRepository.GetTrackedByIdAsync(id);

            if (prenotazione == null)
                throw new KeyNotFoundException("Prenotazione non trovata.");

            if (prenotazione.Stato == StatoPrenotazione.InCorso
                || prenotazione.Stato == StatoPrenotazione.Annullata
                || prenotazione.Stato == StatoPrenotazione.Completata)
                throw new InvalidOperationException($"Non è possibile modificare una prenotazione nello stato {prenotazione.Stato}.");

            var userId = GetAuthenticatedUserId();
            if (!string.Equals(prenotazione.UserId, userId, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("Non hai i permessi per modificare questa prenotazione.");

            await ValidatePrenotazioneAsync(dto, prenotazione.Id);

            if (IsSelfServiceCliente() && dto.DataPrenotazione != prenotazione.DataPrenotazione)
                await GuardUnaPrenotazioneAlGiornoAsync(userId, dto.DataPrenotazione, excludePrenotazioneId: prenotazione.Id);

            var postazioniAssegnate = await _postazioneAssignmentService.AssegnaPostazioneDisponibileAsync(dto, prenotazione.Id);

            prenotazione.DataPrenotazione = (DateOnly)dto.DataPrenotazione;
            prenotazione.NumeroCoperti = dto.NumeroCoperti;
            prenotazione.FasciaOrariaId = dto.FasciaOrariaId;
            prenotazione.NomeCliente = dto.NomeCliente;
            prenotazione.Note = dto.Note;

            if (prenotazione.PrenotazioniPostazioni != null && prenotazione.PrenotazioniPostazioni.Any())
                _context.PrenotazioniPostazioni.RemoveRange(prenotazione.PrenotazioniPostazioni);

            prenotazione.PrenotazioniPostazioni = postazioniAssegnate
                .Select(p => new PrenotazionePostazione
                {
                    PostazioneId = p.Id,
                    Prenotazione = prenotazione
                })
                .ToList();

            await _prenotazioniRepository.UpdateAsync(prenotazione);
        }

        public async Task ConfermaPrenotazioneAsync(long id)
        {
            var prenotazione = await _prenotazioniRepository.GetTrackedByIdAsync(id);

            if (prenotazione == null)
                throw new KeyNotFoundException("Prenotazione non trovata.");

            if (prenotazione.Stato != StatoPrenotazione.Attiva)
                throw new InvalidOperationException("Solo prenotazioni con stato 'Attiva' possono essere confermate.");

            prenotazione.Stato = StatoPrenotazione.InCorso;
            await _prenotazioniRepository.UpdateAsync(prenotazione);
            await _logActivity.LogAsync(GetAuthenticatedUserId(), $"Confermata prenotazione ID {id}", GetIpAddress());
        }

        public async Task CompletePrenotazioneAsync(long id)
        {
            var prenotazione = await _prenotazioniRepository.GetTrackedByIdAsync(id);

            if (prenotazione == null)
                throw new KeyNotFoundException("Prenotazione non trovata.");

            if (prenotazione.FasciaOraria == null)
                throw new InvalidOperationException("La fascia oraria associata alla prenotazione non è disponibile.");

            if (prenotazione.Stato != StatoPrenotazione.InCorso)
                throw new InvalidOperationException("Solo prenotazioni 'In corso' possono essere completate.");

            var now = GetNowInRome();
            var endDateTime = prenotazione.DataPrenotazione.ToDateTime(TimeOnly.MinValue)
                                          .Add(prenotazione.FasciaOraria.OrarioFine.ToTimeSpan());

            if (now < endDateTime)
                throw new InvalidOperationException("Non è possibile completare: la prenotazione non è ancora terminata.");

            prenotazione.Stato = StatoPrenotazione.Completata;
            await _prenotazioniRepository.UpdateAsync(prenotazione);
            await _logActivity.LogAsync(GetAuthenticatedUserId(), $"Completata prenotazione ID {id}", GetIpAddress());
        }

        public async Task AnnullaPrenotazioneAsync(long id)
        {
            var prenotazione = await _prenotazioniRepository.GetTrackedByIdAsync(id);

            if (prenotazione == null)
                throw new KeyNotFoundException("Prenotazione non trovata nel sistema.");

            if (prenotazione.Stato == StatoPrenotazione.Completata)
                throw new InvalidOperationException("Non è possibile annullare una prenotazione già completata.");

            prenotazione.Stato = StatoPrenotazione.Annullata;
            await _prenotazioniRepository.UpdateAsync(prenotazione);
            await _logActivity.LogAsync(GetAuthenticatedUserId(), $"Annullata prenotazione ID {id}", GetIpAddress());
        }

        public async Task<List<PrenotazioneDTO>> GetPrenotazioniByDataAsync(DateOnly data)
        {
            var prenotazioni = await _prenotazioniRepository.GetAllQueryableAsync()
                .Where(p => p.DataPrenotazione == data)
                .Include(p => p.User)
                .Include(p => p.FasciaOraria)
                .Include(p => p.PrenotazioniPostazioni)
                    .ThenInclude(pp => pp.Postazione)
                        .ThenInclude(po => po.Zona)
                .AsNoTracking()
                .ToListAsync();

            if (!prenotazioni.Any())
                throw new KeyNotFoundException("Nessuna prenotazione trovata per la data specificata.");

            return _mapper.Map<List<PrenotazioneDTO>>(prenotazioni);
        }

        public async Task<List<PrenotazioneDTO>> GetMiePrenotazioniAsync(string userId)
        {
            var prenotazioni = await _prenotazioniRepository.GetAllQueryableAsync()
                .Where(p => p.UserId == userId)
                .Include(p => p.User)
                .Include(p => p.FasciaOraria)
                .Include(p => p.PrenotazioniPostazioni)
                    .ThenInclude(pp => pp.Postazione)
                        .ThenInclude(po => po.Zona)
                .AsNoTracking()
                .ToListAsync();

            return _mapper.Map<List<PrenotazioneDTO>>(prenotazioni);
        }


        public async Task<PagedResult<PrenotazioneDTO>> GetAllPrenotazioniAsync(PrenotazioniQueryParams query)
        {
            var queryable = _prenotazioniRepository.GetAllQueryableAsync()
                .OrderBy(p => p.DataPrenotazione);

            if (query.Data.HasValue)
                queryable = queryable.Where(p => p.DataPrenotazione == query.Data.Value)
                                     .OrderBy(p => p.DataPrenotazione);

            if (query.Stato.HasValue)
                queryable = queryable.Where(p => p.Stato == query.Stato.Value)
                                     .OrderBy(p => p.DataPrenotazione);

            var totalCount = await queryable.CountAsync();

            var items = await queryable
                .Include(p => p.User)
                .Include(p => p.FasciaOraria)
                .Include(p => p.PrenotazioniPostazioni)
                    .ThenInclude(pp => pp.Postazione)
                        .ThenInclude(po => po.Zona)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            return new PagedResult<PrenotazioneDTO>
            {
                Items = _mapper.Map<List<PrenotazioneDTO>>(items),
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }

        public async Task AutomaticCompletPrenotazioniAsync()
        {
            var now = GetNowInRome();
            var today = DateOnly.FromDateTime(now);
            var oraAttuale = TimeOnly.FromTimeSpan(now.TimeOfDay);

            var prenotazioniInCorso = await _prenotazioniRepository
                .GetAllQueryableAsync()
                .Include(p => p.FasciaOraria)
                .Where(p => p.Stato == StatoPrenotazione.InCorso &&
                      (p.DataPrenotazione < today ||
                      (p.DataPrenotazione == today && oraAttuale > p.FasciaOraria.OrarioFine)))
                .ToListAsync();

            foreach (var prenotazione in prenotazioniInCorso)
            {
                var trackedPrenotazione = await _prenotazioniRepository.GetTrackedByIdAsync(prenotazione.Id);
                trackedPrenotazione.Stato = StatoPrenotazione.Completata;
                await _prenotazioniRepository.UpdateAsync(trackedPrenotazione);

                _logger.LogInformation("[PrenotazioniService] Prenotazione {Id} completata automaticamente.", prenotazione.Id);
            }
        }

        public async Task AutomaticDeletePrenotazioniAsync()
        {
            var now = GetNowInRome();
            var cutoffDate = DateOnly.FromDateTime(now).AddMonths(-6);

            var prenotazioniToDelete = await _prenotazioniRepository
                .GetAllQueryableAsync()
                .Where(p => p.Stato == StatoPrenotazione.Completata && p.DataPrenotazione <= cutoffDate)
                .ToListAsync();

            foreach (var prenotazione in prenotazioniToDelete)
            {
                await _prenotazioniRepository.DeleteAsync(prenotazione);
                _logger.LogInformation("[PrenotazioniService] Prenotazione {Id} eliminata automaticamente.", prenotazione.Id);
            }

            if (!prenotazioniToDelete.Any())
                _logger.LogInformation("[PrenotazioniService] Nessuna prenotazione da eliminare.");
        }

        private string GetAuthenticatedUserId()
            => _httpContextAccessor.HttpContext?.User.GetAuthenticatedUserId()
               ?? throw new UnauthorizedAccessException("Utente non autenticato.");

        // Il vincolo "una prenotazione attiva al giorno" ha senso solo per il self-service:
        // Staff/Admin creano prenotazioni per conto di clienti diversi (es. telefonate) sotto
        // il proprio UserId, quindi per loro il vincolo non deve valere.
        private bool IsSelfServiceCliente()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            return user != null && !user.IsInRole(Roles.Admin) && !user.IsInRole(Roles.Staff);
        }

        private async Task GuardUnaPrenotazioneAlGiornoAsync(string userId, DateOnly data, long? excludePrenotazioneId = null)
        {
            bool esisteGiaAttiva = await _prenotazioniRepository.GetAllQueryableAsync()
                .AnyAsync(p =>
                    p.UserId == userId &&
                    p.DataPrenotazione == data &&
                    p.Stato != StatoPrenotazione.Annullata &&
                    (!excludePrenotazioneId.HasValue || p.Id != excludePrenotazioneId.Value));

            if (esisteGiaAttiva)
                throw new InvalidOperationException("Hai già una prenotazione attiva per questo giorno. Annullala prima di crearne una nuova, oppure modificala.");
        }

        private string? GetIpAddress()
            => _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

        private async Task ValidatePrenotazioneAsync(PrenotazioneCreateDTO dto, long? excludePrenotazioneId = null)
        {
            var fasciaOraria = await _fasciaOrariaRepository.GetByIdAsync(dto.FasciaOrariaId);

            if (fasciaOraria == null)
                throw new ArgumentException("La fascia oraria specificata non esiste.");

            if (!fasciaOraria.Attiva)
                throw new InvalidOperationException("La fascia oraria selezionata non è attiva.");

            var giornoIt = new System.Globalization.CultureInfo("it-IT")
                .DateTimeFormat.GetDayName(fasciaOraria.GiornoSettimana);

            if (fasciaOraria.GiornoSettimana != dto.DataPrenotazione.DayOfWeek)
                throw new InvalidOperationException($"La fascia oraria selezionata è valida solo per il giorno {giornoIt}.");

            int copertiGiaPrenotati = await _prenotazioniRepository.GetAllQueryableAsync()
                .Where(p =>
                    p.DataPrenotazione == dto.DataPrenotazione &&
                    p.FasciaOrariaId == dto.FasciaOrariaId &&
                    p.Stato != StatoPrenotazione.Annullata &&
                    (!excludePrenotazioneId.HasValue || p.Id != excludePrenotazioneId.Value))
                .SumAsync(p => p.NumeroCoperti);

            int copertiDisponibili = fasciaOraria.MaxPrenotazioni - copertiGiaPrenotati;
            if (dto.NumeroCoperti > copertiDisponibili)
                throw new InvalidOperationException(
                    copertiDisponibili <= 0
                        ? "La fascia oraria ha raggiunto la capienza massima. Non ci sono coperti disponibili."
                        : $"Coperti richiesti ({dto.NumeroCoperti}) superiori a quelli disponibili ({copertiDisponibili}) per questa fascia oraria.");

            if (dto.ZonaId.HasValue)
            {
                var zona = await _zonaRepository.GetByIdAsync(dto.ZonaId.Value);
                if (zona == null || !zona.Attiva)
                    throw new InvalidOperationException("La zona selezionata non è attiva o non esiste.");

                bool zonaHaPostazioni = await _context.Postazioni
                    .AsNoTracking()
                    .AnyAsync(p => p.ZonaId == dto.ZonaId.Value && p.Attiva);

                if (!zonaHaPostazioni)
                    throw new InvalidOperationException("La zona preferita selezionata non ha postazioni attive.");
            }

            bool almenoUnaPostazioneAttiva = await _context.Postazioni
                .AsNoTracking()
                .AnyAsync(p => p.Attiva);

            if (!almenoUnaPostazioneAttiva)
                throw new ArgumentException("Non ci sono postazioni attive nel sistema.");
        }

        private DateTime GetNowInRome()
        {
            try
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById("Europe/Rome");
                return TimeZoneInfo.ConvertTime(DateTime.UtcNow, tz);
            }
            catch (TimeZoneNotFoundException)
            {
                return DateTime.Now;
            }
            catch (InvalidTimeZoneException)
            {
                return DateTime.Now;
            }
        }
    }
}