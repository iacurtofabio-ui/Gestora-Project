using GestoraWebApi.Common;
using GestoraWebApi.Context;
using GestoraWebApi.Enums;
using GestoraWebApi.Services.Dashboard.DTOs;
using Microsoft.EntityFrameworkCore;

namespace GestoraWebApi.Services.Dashboard
{
    public class DashboardService : IDashboardService
    {
        private readonly GestoraContext _context;
        private readonly ILogger<DashboardService> _logger;
        private readonly IClock _clock;

        // Per la localizzazione italiana dei nomi giorno
        private static readonly System.Globalization.CultureInfo _itCulture =
            new("it-IT");

        public DashboardService(GestoraContext context, ILogger<DashboardService> logger, IClock clock)
        {
            _context = context;
            _logger = logger;
            _clock = clock;
        }

        //Panoramica giornaliera

        public async Task<DashboardGiornalieroDTO> GetDashboardGiornalieroAsync(DateOnly data)
        {
            _logger.LogInformation("[DashboardService] GetDashboardGiornaliero per data {Data}", data);

            // 1. Prenotazioni del giorno con le relazioni necessarie
            var prenotazioniDelGiorno = await _context.Prenotazioni
                .AsNoTracking()
                .Where(p => p.DataPrenotazione == data)
                .Include(p => p.PrenotazioniPostazioni)
                .ToListAsync();

            // 2. Contatori per stato
            var totale = prenotazioniDelGiorno.Count;
            var attive = prenotazioniDelGiorno.Count(p => p.Stato == StatoPrenotazione.Attiva);
            var inCorso = prenotazioniDelGiorno.Count(p => p.Stato == StatoPrenotazione.InCorso);
            var completate = prenotazioniDelGiorno.Count(p => p.Stato == StatoPrenotazione.Completata);
            var annullate = prenotazioniDelGiorno.Count(p => p.Stato == StatoPrenotazione.Annullata);

            // 3. Coperti prenotati (esclude annullate)
            var copertiTotali = prenotazioniDelGiorno
                .Where(p => p.Stato != StatoPrenotazione.Annullata)
                .Sum(p => p.NumeroCoperti);

            // 4. Postazioni attive nel sistema
            var totalePostazioni = await _context.Postazioni
                .AsNoTracking()
                .CountAsync(p => p.Attiva);

            // 5. Postazioni occupate oggi: quelle con almeno una prenotazione
            //    attiva o in corso in questo giorno
            var idPostazioniOccupate = prenotazioniDelGiorno
                .Where(p => p.Stato == StatoPrenotazione.Attiva
                         || p.Stato == StatoPrenotazione.InCorso)
                .SelectMany(p => p.PrenotazioniPostazioni)
                .Select(pp => pp.PostazioneId)
                .Distinct()
                .Count();

            // 6. Coperti per fascia: fasce attive del giorno della settimana
            var giornoSettimana = data.DayOfWeek;

            var fasce = await _context.FasciaOrarie
                .AsNoTracking()
                .Where(f => f.Attiva && f.GiornoSettimana == giornoSettimana)
                .OrderBy(f => f.OrarioInizio)
                .ToListAsync();

            // Raggruppa le prenotazioni valide per fasciaOrariaId
            var prenotazioniValide = prenotazioniDelGiorno
                .Where(p => p.Stato != StatoPrenotazione.Annullata)
                .ToList();

            var copertiPerFascia = fasce.Select(f =>
            {
                var prenotazioniFascia = prenotazioniValide
                    .Where(p => p.FasciaOrariaId == f.Id)
                    .ToList();

                var copertiPrenotati = prenotazioniFascia.Sum(p => p.NumeroCoperti);

                return new CopertiFasciaDTO
                {
                    FasciaOrariaId = f.Id,
                    OraInizio = f.OrarioInizio.ToString("HH:mm"),
                    OraFine = f.OrarioFine.ToString("HH:mm"),
                    MaxCoperti = f.MaxCoperti,
                    CopertiPrenotati = copertiPrenotati,
                    CopertiDisponibili = Math.Max(0, f.MaxCoperti - copertiPrenotati),
                    NumeroPrenotazioni = prenotazioniFascia.Count
                };
            }).ToList();

            return new DashboardGiornalieroDTO
            {
                Data = data,
                TotalePrenotazioni = totale,
                PrenotazioniAttive = attive,
                PrenotazioniInCorso = inCorso,
                PrenotazioniCompletate = completate,
                PrenotazioniAnnullate = annullate,
                TotaleCopertiPrenotati = copertiTotali,
                TotalePostazioniAttive = totalePostazioni,
                PostazioniOccupate = idPostazioniOccupate,
                PostazioniLibere = Math.Max(0, totalePostazioni - idPostazioniOccupate),
                CopertiPerFascia = copertiPerFascia
            };
        }

        // Panoramica settimanale

        public async Task<DashboardSettimanaleDTO> GetDashboardSettimanaleAsync(DateOnly dataInizio)
        {
            var dataFine = dataInizio.AddDays(6);

            _logger.LogInformation(
                "[DashboardService] GetDashboardSettimanale da {Inizio} a {Fine}",
                dataInizio, dataFine);

            // Tutte le prenotazioni del periodo
            var prenotazioni = await _context.Prenotazioni
                .AsNoTracking()
                .Where(p => p.DataPrenotazione >= dataInizio && p.DataPrenotazione <= dataFine)
                .ToListAsync();

            var totale = prenotazioni.Count;
            var annullate = prenotazioni.Count(p => p.Stato == StatoPrenotazione.Annullata);

            // Coperti totali (escluse annullate)
            var copertiTotali = prenotazioni
                .Where(p => p.Stato != StatoPrenotazione.Annullata)
                .Sum(p => p.NumeroCoperti);

            // Tasso annullamento (% su totale)
            var tassoAnnullamento = totale > 0
                ? Math.Round((double)annullate / totale * 100, 1)
                : 0;

            // Tasso no-show: prenotazioni rimaste Attiva su date già passate
            // (il cliente non si è presentato, lo staff non ha mai confermato)
            var oggi = _clock.TodayInRome;
            var noShow = prenotazioni.Count(p =>
                p.Stato == StatoPrenotazione.Attiva && p.DataPrenotazione < oggi);
            var prenotazioniConcluse = prenotazioni.Count(p =>
                p.DataPrenotazione < oggi && p.Stato != StatoPrenotazione.Annullata);
            var tassoNoShow = prenotazioniConcluse > 0
                ? Math.Round((double)noShow / prenotazioniConcluse * 100, 1)
                : 0;

            // Dettaglio per giorno
            var giorni = Enumerable.Range(0, 7)
                .Select(i =>
                {
                    var giorno = dataInizio.AddDays(i);
                    var delGiorno = prenotazioni.Where(p => p.DataPrenotazione == giorno).ToList();

                    return new GiornoSettimanaleDTO
                    {
                        Data = giorno,
                        GiornoNome = giorno.DayOfWeek.ToString("G") switch
                        {
                            // Traduciamo in italiano
                            "Monday" => "Lunedì",
                            "Tuesday" => "Martedì",
                            "Wednesday" => "Mercoledì",
                            "Thursday" => "Giovedì",
                            "Friday" => "Venerdì",
                            "Saturday" => "Sabato",
                            "Sunday" => "Domenica",
                            _ => giorno.DayOfWeek.ToString()
                        },
                        NumeroPrenotazioni = delGiorno.Count,
                        NumeroCoperti = delGiorno
                            .Where(p => p.Stato != StatoPrenotazione.Annullata)
                            .Sum(p => p.NumeroCoperti),
                        Annullate = delGiorno.Count(p => p.Stato == StatoPrenotazione.Annullata)
                    };
                })
                .ToList();

            return new DashboardSettimanaleDTO
            {
                DataInizio = dataInizio,
                DataFine = dataFine,
                TotalePrenotazioni = totale,
                TotaleCoperti = copertiTotali,
                TassoAnnullamento = tassoAnnullamento,
                TassoNoShow = tassoNoShow,
                Giorni = giorni
            };
        }
    }
}