using AutoMapper;
using GestoraWebApi.Common;
using GestoraWebApi.Enums;
using GestoraWebApi.Extensions;
using GestoraWebApi.Models;
using GestoraWebApi.Repositories.FasciaOrarie;
using GestoraWebApi.Repositories.Postazioni;
using GestoraWebApi.Repositories.Zone;
using GestoraWebApi.Services.PostazioneAssignment;
using GestoraWebApi.Services.LogActivity;
using GestoraWebApi.Services.Postazioni.DTOs;
using GestoraWebApi.Services.Prenotazioni.DTOs;
using GestoraWebApi.Services.Sala;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics.Eventing.Reader;
using GestoraWebApi.Infrastructure.Exceptions;

namespace GestoraWebApi.Services.Postazioni
{
    public class PostazioneService : IPostazioneService
    {
        private readonly IPostazioneRepository _postazioneRepository;
        private readonly IZonaRepository _zonaRepository;
        private readonly IFasciaOrariaRepository _fasciaOrariaRepository;
        private readonly IMapper _mapper;
        private readonly IMemoryCache _cache;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogActivityService _logActivity;
        private readonly IClock _clock;
        private readonly IEsecutoreTransazione _transazione;
        private readonly ICoerenzaSalaService _coerenzaSala;

        public PostazioneService(IPostazioneRepository postazioneRepository, IMapper mapper, IZonaRepository zonaRepository,
                                  IMemoryCache cache, IHttpContextAccessor httpContextAccessor, ILogActivityService logActivity,
                                  IFasciaOrariaRepository fasciaOrariaRepository, IClock clock,
                                  IEsecutoreTransazione transazione, ICoerenzaSalaService coerenzaSala)
        {
            _coerenzaSala = coerenzaSala;
            _postazioneRepository = postazioneRepository;
            _zonaRepository = zonaRepository;
            _fasciaOrariaRepository = fasciaOrariaRepository;
            _mapper = mapper;
            _cache = cache;
            _httpContextAccessor = httpContextAccessor;
            _logActivity = logActivity;
            _clock = clock;
            _transazione = transazione;
        }
        public async Task AddAsync(PostazioneDTO dto)
        {
            await ValidatePostazioneAsync(dto);

            var postazione = _mapper.Map<Postazione>(dto);

            // REV-032: scrittura e traccia nell'audit trail nella stessa transazione.
            await _transazione.EseguiAsync(async () =>
            {
                await _postazioneRepository.AddAsync(postazione);
                await _logActivity.LogAsync(_httpContextAccessor.HttpContext.GetAuthenticatedUserId(), $"Creata postazione numero {postazione.Numero}", _httpContextAccessor.HttpContext.GetIpAddress());
            });

            // Cache invalidata dopo il commit.
            _cache.Remove(CacheKeys.PostazioniAttive);
        }

        public async Task DeleteAsync(long postazioneId)
        {
            var postazione = await _postazioneRepository.GetByIdAsync(postazioneId);
            if (postazione == null)
                throw new KeyNotFoundException($"Postazione con ID {postazioneId} non trovata.");

            // Solo le prenotazioni ancora vive (Attiva, InCorso) bloccano l'eliminazione: quelle
            // Completate, NonPresentate o Annullate sono storico. Le loro righe di legame col
            // tavolo spariscono a cascata (il database le elimina insieme al tavolo), la
            // prenotazione resta.
            if (await _postazioneRepository.HasPrenotazioniViveAsync(postazioneId))
                throw new ConflictException($"Impossibile eliminare il tavolo {postazione.Numero}: ha prenotazioni attive o in corso.");

            // V2-007: togliere il tavolo non deve portare i posti sotto il tetto di una fascia attiva.
            await _coerenzaSala.VerificaModificaSalaAsync(tavoli => tavoli.Where(t => t.Id != postazioneId));

            await _transazione.EseguiAsync(async () =>
            {
                await _postazioneRepository.DeleteAsync(postazione);
                await _logActivity.LogAsync(_httpContextAccessor.HttpContext.GetAuthenticatedUserId(), $"Eliminata postazione numero {postazione.Numero} (ID {postazioneId})", _httpContextAccessor.HttpContext.GetIpAddress());
            });

            _cache.Remove(CacheKeys.PostazioniAttive);
        }

        public async Task<Postazione> GetByIdAsync(long id)
        {
            return await _postazioneRepository
               .GetAllQueryable()
               .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<PostazioneDTO> GetPostazioneDTOByIdAsync(long id)
        {
            var postazione = await _postazioneRepository
                .GetAllQueryable()
                .Include(p => p.PrenotazioniPostazioni)
                .FirstOrDefaultAsync(p => p.Id == id);

            // Id inesistente: il controller risponde 404. Mappare null darebbe un dto nullo e
            // la riga sotto un NullReferenceException, cioe' un 500.
            if (postazione == null)
                return null;

            var dto = _mapper.Map<PostazioneDTO>(postazione);

            // Prende le PrenotazioneId associate
            dto.PrenotazioneId = postazione.PrenotazioniPostazioni?
                                .Select(pp => pp.PrenotazioneId)
                                .ToList() ?? new List<long>();

            return dto;

        }

        private async Task ValidatePostazioneAsync(PostazioneDTO dto)
        {
            if (!dto.Attiva)
                throw new ConflictException("La postazione selezionata non è attiva.");

            var existingPostazione = await _postazioneRepository
                .GetAllQueryable()
                .FirstOrDefaultAsync(p => p.Numero == dto.Numero);

            if (existingPostazione != null)
                throw new ConflictException($"Esiste già una postazione con il numero '{dto.Numero}'.");

            var zona = await _zonaRepository.GetByIdAsync(dto.ZonaId);
            if (zona == null)
                throw new ArgumentException("La zona specificata per la postazione non esiste.");
        }

        public async Task UpdateAsync(PostazioneUpdateDTO dto)
        {
            // Recupero postazione dal repository
            var postazione = await _postazioneRepository.GetByIdAsync(dto.Id);
            if (postazione == null)
                throw new KeyNotFoundException($"Postazione con ID {dto.Id} non trovata.");

            await GuardImpegniFuturiAsync(postazione, dto.CapienzaMassima, dto.ZonaId, dto.Attiva);

            // Controllo numero duplicato
            var existingPostazione = await _postazioneRepository
                .GetAllQueryable()
                .FirstOrDefaultAsync(p => p.Numero == dto.Numero && p.Id != dto.Id);

            if (existingPostazione != null)
                throw new ConflictException($"Esiste già una postazione con il numero '{dto.Numero}'.");

            // Controllo esistenza zona (FIX-001: evita il messaggio tecnico EF su violazione FK)
            var zona = await _zonaRepository.GetByIdAsync(dto.ZonaId);
            if (zona == null)
                throw new ArgumentException("La zona specificata per la postazione non esiste.");

            // V2-007: capienza ridotta, tavolo disattivato o spostato in una zona spenta non devono
            // portare i posti sotto il tetto di una fascia attiva. Si confronta la sala com'è con
            // quella dopo la modifica, prima di toccare l'entità.
            await _coerenzaSala.VerificaModificaSalaAsync(tavoli =>
            {
                var dopo = tavoli.Where(t => t.Id != dto.Id);
                return dto.Attiva ? dopo.Append(new TavoloSala(dto.Id, dto.ZonaId, dto.CapienzaMassima)) : dopo;
            });

            // Mappaggio dei campi consentiti con AutoMapper
            _mapper.Map(dto, postazione);

            await _transazione.EseguiAsync(async () =>
            {
                await _postazioneRepository.UpdateAsync(postazione);
                await _logActivity.LogAsync(_httpContextAccessor.HttpContext.GetAuthenticatedUserId(), $"Modificata postazione numero {postazione.Numero} (ID {postazione.Id})", _httpContextAccessor.HttpContext.GetIpAddress());
            });

            _cache.Remove(CacheKeys.PostazioniAttive);
        }

        public async Task<List<PostazioneDTO>> GetPostazioniAttiveAsync()
        {
            if (_cache.TryGetValue(CacheKeys.PostazioniAttive, out List<PostazioneDTO>? cached))
                return cached!;

            // AUD-M8: l'elenco esponeva in PrenotazioneId tutte le prenotazioni mai collegate a ogni
            // tavolo. Era in cache per 30 minuti senza che le prenotazioni la svuotassero (dato
            // vecchio), cresceva senza limite e l'endpoint e' aperto anche al Cliente, che riceveva
            // gli Id delle prenotazioni altrui. Nessuna schermata lo usa: l'elenco ora contiene
            // solo i dati del tavolo, che cambiano solo con le scritture su tavoli (gia' invalidate).
            var postazioni = await _postazioneRepository.GetPostazioniAttiveAsync();

            var result = postazioni.Select(p => new PostazioneDTO
            {
                Id = p.Id,
                Numero = p.Numero,
                CapienzaMassima = p.CapienzaMassima,
                Attiva = p.Attiva,
                ZonaId = p.ZonaId
            }).ToList();

            _cache.Set(CacheKeys.PostazioniAttive, result, CacheKeys.Durata);

            return result;
        }

        public async Task<List<PostazioniDisponibiliDTO>> GetPostazioniDisponibiliAsync()
        {
            var postazioni = await _postazioneRepository.GetPostazioniDisponibiliAsync();

            var dto = postazioni.Select(p => new PostazioniDisponibiliDTO
            {
                Id = p.Id,
                Numero = p.Numero,
                CapienzaMassima = p.CapienzaMassima,
                Attiva = p.Attiva,
                ZonaId = p.ZonaId,
                
            }).ToList();

            return dto;
        }

        public async Task<List<PostazioneDTO>> GetPostazioniPerZonaAsync(long zonaId)
        {
            // Verifica zona
            var zona = await _zonaRepository.GetByIdAsync(zonaId);
            if (zona == null)
                throw new ArgumentException($"La zona con ID {zonaId} non esiste.");
            if (!zona.Attiva)
                throw new ConflictException($"La zona con ID {zonaId} non è attiva.");

            // Recupera postazioni libere per zona
            var postazioni = await _postazioneRepository.GetPostazioniPerZonaAsync(zonaId);

            // Mapping manuale
            return postazioni.Select(p => new PostazioneDTO
            {
                Id = p.Id,
                Numero = p.Numero,
                CapienzaMassima = p.CapienzaMassima,
                Attiva = p.Attiva,
                ZonaId = p.ZonaId,
                PrenotazioneId = p.PrenotazioniPostazioni?
                    .Select(pp => pp.PrenotazioneId)
                    .ToList() ?? new List<long>()
            }).ToList();
        }

        /// <summary>
        /// V2-007: l'elenco della pagina Tavoli. A differenza di <see cref="GetPostazioniPerZonaAsync"/>
        /// mostra anche i tavoli disattivati e funziona anche su una zona spenta: altrimenti un
        /// tavolo disattivato spariva e non si poteva più riattivare dall'interfaccia.
        /// Senza PrenotazioneId: alla pagina non serve.
        /// </summary>
        public async Task<List<PostazioneDTO>> GetTavoliZonaPerGestioneAsync(long zonaId)
        {
            if (await _zonaRepository.GetByIdAsync(zonaId) == null)
                throw new NotFoundException($"La zona con ID {zonaId} non esiste.");

            var postazioni = await _postazioneRepository.GetTuttePostazioniPerZonaAsync(zonaId);

            return postazioni.Select(p => new PostazioneDTO
            {
                Id = p.Id,
                Numero = p.Numero,
                CapienzaMassima = p.CapienzaMassima,
                Attiva = p.Attiva,
                ZonaId = p.ZonaId
            }).ToList();
        }

        public async Task<RiepilogoSalaDTO> GetRiepilogoSalaAsync()
        {
            var zoneAttiveIds = (await _zonaRepository.GetAllZoneAttiveAsync()).Select(z => z.Id).ToHashSet();

            var tavoli = (await _postazioneRepository.GetPostazioniAttiveAsync())
                .Where(p => zoneAttiveIds.Contains(p.ZonaId))
                .ToList();

            // V2-007: stesso conteggio dei controlli di coerenza su fasce, tavoli e zone.
            var postiTotali = CapienzaSala.Posti(
                tavoli.Select(t => new TavoloSala(t.Id, t.ZonaId, t.CapienzaMassima)), zoneAttiveIds);

            var fasce = await _fasciaOrariaRepository.GetFasceAttiveAsync();
            var cultureIt = new System.Globalization.CultureInfo("it-IT");

            return new RiepilogoSalaDTO
            {
                TavoliAttivi = tavoli.Count,
                PostiTotali = postiTotali,
                Fasce = fasce
                    .OrderBy(f => ((int)f.GiornoSettimana + 6) % 7) // lunedì primo
                    .ThenBy(f => f.OrarioInizio)
                    .Select(f => new RiepilogoFasciaDTO
                    {
                        FasciaOrariaId = f.Id,
                        GiornoSettimana = cultureIt.DateTimeFormat.GetDayName(f.GiornoSettimana),
                        OrarioInizio = f.OrarioInizio,
                        OrarioFine = f.OrarioFine,
                        MaxCoperti = f.MaxCoperti,
                        PostiTavoli = postiTotali,
                        TettoCoperto = postiTotali >= f.MaxCoperti
                    })
                    .ToList()
            };
        }

        private static readonly System.Globalization.CultureInfo Italiano = new("it-IT");

        /// <summary>
        /// V2-009: una modifica al tavolo si blocca solo se danneggia una prenotazione ancora da
        /// servire. Numero e aumento dei posti passano sempre. Disattivarlo o spostarlo di zona no,
        /// se qualcuno lo ha prenotato: chi ha prenotato si aspetta quel tavolo, in quella zona.
        /// Ridurre i posti si', se ogni prenotazione ci sta ancora.
        /// </summary>
        private async Task GuardImpegniFuturiAsync(Postazione postazione, int nuovaCapienza, long nuovaZonaId, bool attiva)
        {
            var spegne = postazione.Attiva && !attiva;
            var sposta = nuovaZonaId != postazione.ZonaId;
            var riduce = nuovaCapienza < postazione.CapienzaMassima;

            if (!spegne && !sposta && !riduce)
                return;

            var impegni = await _postazioneRepository.GetImpegniFuturiAsync(postazione.Id, _clock.TodayInRome);
            if (impegni.Count == 0)
                return;

            if (spegne || sposta)
            {
                var azione = spegne ? "disattivare" : "spostare di zona";
                throw new ConflictException(
                    $"Impossibile {azione} il tavolo {postazione.Numero}: è assegnato " +
                    $"{DescriviImpegni(impegni)}. Sposta o annulla prima quelle prenotazioni.");
            }

            var nonCiStanno = impegni.Where(p => !CiStaAncora(p, postazione.Id, nuovaCapienza)).ToList();
            if (nonCiStanno.Count > 0)
                throw new ConflictException(
                    $"Impossibile ridurre i posti del tavolo {postazione.Numero} a {nuovaCapienza}: non ci " +
                    $"starebbe più {DescriviImpegni(nonCiStanno)}. Sposta o annulla prima quelle prenotazioni.");
        }

        /// <summary>
        /// Due condizioni: il tavolo tiene ancora le persone che gli sono state assegnate, e
        /// l'unione intera (con le regole delle testate, che si perdono se un tavolo non e' piu'
        /// da 2) tiene ancora tutto il gruppo.
        /// </summary>
        private static bool CiStaAncora(Prenotazione prenotazione, long postazioneId, int nuovaCapienza)
        {
            var righe = prenotazione.PrenotazioniPostazioni;
            if (righe.Any(pp => pp.PostazioneId == postazioneId && pp.NumeroPosti > nuovaCapienza))
                return false;

            var tavoliDopo = righe
                .Select(pp => pp.PostazioneId == postazioneId
                    ? new Postazione { Id = postazioneId, CapienzaMassima = nuovaCapienza }
                    : pp.Postazione)
                .ToList();

            return AssegnazioneTavoli.CalcolaCapienza(tavoliDopo) >= prenotazione.NumeroCoperti;
        }

        /// <summary>"alla prenotazione di sabato 10/10/2026, 20:00-22:00 (4 persone) e ad altre 2".</summary>
        private static string DescriviImpegni(IReadOnlyList<Prenotazione> impegni)
        {
            var prima = impegni[0];
            var giorno = Italiano.DateTimeFormat.GetDayName(prima.DataPrenotazione.DayOfWeek);
            var fascia = prima.FasciaOraria == null
                ? ""
                : $", {prima.FasciaOraria.OrarioInizio:HH\\:mm}-{prima.FasciaOraria.OrarioFine:HH\\:mm}";
            var altre = impegni.Count > 1 ? $" e ad altre {impegni.Count - 1}" : "";

            return $"alla prenotazione di {giorno} {prima.DataPrenotazione.ToString("dd/MM/yyyy", Italiano)}{fascia} " +
                   $"({prima.NumeroCoperti} persone){altre}";
        }

        public async Task AssociaPostazioneAZonaAsync(long postazioneId, long zonaId)
        {
            #region Validazioni
            var zona = await _zonaRepository.GetByIdAsync(zonaId);
            if (zona == null)
                throw new ArgumentException($"La zona con ID {zonaId} non esiste.");

            if (!zona.Attiva)
                throw new ConflictException($"Impossibile associare: la zona con ID {zonaId} non è attiva.");

            var postazione = await _postazioneRepository.GetByIdAsync(postazioneId);
            if (postazione == null)
                throw new KeyNotFoundException($"Postazione con ID {postazioneId} non trovata.");

            if (!postazione.Attiva)
                throw new ConflictException($"Impossibile associare: il tavolo {postazione.Numero} non è attivo.");

            await GuardImpegniFuturiAsync(postazione, postazione.CapienzaMassima, zonaId, postazione.Attiva);
            #endregion

            postazione.ZonaId = zonaId;

            await _transazione.EseguiAsync(async () =>
            {
                await _postazioneRepository.UpdateAsync(postazione);
                await _logActivity.LogAsync(_httpContextAccessor.HttpContext.GetAuthenticatedUserId(),
                    $"Postazione numero {postazione.Numero} (ID {postazioneId}) associata alla zona ID {zonaId}", _httpContextAccessor.HttpContext.GetIpAddress());
            });

            _cache.Remove(CacheKeys.PostazioniAttive);
        }

    }
}
