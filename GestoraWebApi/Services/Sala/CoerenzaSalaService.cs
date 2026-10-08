using System.Globalization;
using GestoraWebApi.Infrastructure.Exceptions;
using GestoraWebApi.Models;
using GestoraWebApi.Repositories.FasciaOrarie;
using GestoraWebApi.Repositories.Postazioni;
using GestoraWebApi.Repositories.Zone;

namespace GestoraWebApi.Services.Sala
{
    /// <summary>
    /// V2-007: tiene coerenti il tetto delle fasce e i posti della sala. Legge i dati e delega il
    /// conteggio a <see cref="CapienzaSala"/>. Le regole di dominio rifiutano con
    /// <see cref="ConflictException"/> (409), come il resto dei service.
    /// </summary>
    public class CoerenzaSalaService : ICoerenzaSalaService
    {
        private static readonly CultureInfo Italiano = new("it-IT");
        private const int MaxFasceNelMessaggio = 3;

        private readonly IPostazioneRepository _postazioneRepository;
        private readonly IZonaRepository _zonaRepository;
        private readonly IFasciaOrariaRepository _fasciaRepository;

        public CoerenzaSalaService(IPostazioneRepository postazioneRepository,
                                   IZonaRepository zonaRepository,
                                   IFasciaOrariaRepository fasciaRepository)
        {
            _postazioneRepository = postazioneRepository;
            _zonaRepository = zonaRepository;
            _fasciaRepository = fasciaRepository;
        }

        public async Task<int> GetPostiSalaAsync()
        {
            var (tavoli, zone) = await LeggiSalaAsync();
            return CapienzaSala.Posti(tavoli, zone);
        }

        public async Task VerificaTettoFasciaAsync(int maxCoperti)
        {
            var posti = await GetPostiSalaAsync();
            if (maxCoperti <= posti)
                return;

            // Primo avvio: chi parte dalle fasce deve sapere da dove cominciare.
            if (posti == 0)
                throw new ConflictException(
                    "In sala non ci sono ancora tavoli attivi: crea prima le zone e i tavoli, poi le fasce orarie.");

            throw new ConflictException(
                $"Il tetto della fascia ({maxCoperti} coperti) supera i posti della sala ({posti}, " +
                "somma dei tavoli attivi nelle zone attive). Abbassa il tetto oppure aggiungi prima i tavoli.");
        }

        public async Task VerificaModificaSalaAsync(
            Func<IReadOnlyList<TavoloSala>, IEnumerable<TavoloSala>> tavoliDopo,
            Func<IReadOnlySet<long>, IEnumerable<long>>? zoneAttiveDopo = null)
        {
            var (tavoli, zone) = await LeggiSalaAsync();
            var postiPrima = CapienzaSala.Posti(tavoli, zone);

            var zoneDopo = zoneAttiveDopo == null ? zone : zoneAttiveDopo(zone).ToHashSet();
            var postiDopo = CapienzaSala.Posti(tavoliDopo(tavoli).ToList(), zoneDopo);

            // Una modifica che non toglie posti non può rompere niente.
            if (postiDopo >= postiPrima)
                return;

            var fasceOltre = (await _fasciaRepository.GetFasceAttiveAsync())
                .Where(f => f.MaxCoperti > postiDopo)
                .OrderByDescending(f => f.MaxCoperti)
                .ThenBy(f => f.GiornoSettimana)
                .ThenBy(f => f.OrarioInizio)
                .ToList();

            if (fasceOltre.Count == 0)
                return;

            // Si nominano le prime fasce che bloccano (le più alte prima), le altre si contano.
            var elenco = string.Join(", ", fasceOltre
                .Take(MaxFasceNelMessaggio)
                .Select(f => $"{Descrivi(f)} ({f.MaxCoperti} coperti)"));
            var altre = fasceOltre.Count > MaxFasceNelMessaggio
                ? $" e altre {fasceOltre.Count - MaxFasceNelMessaggio}"
                : "";
            throw new ConflictException(
                "Errore: i posti della sala non possono scendere sotto il tetto delle fasce. " +
                $"In questo caso fasce: {elenco}{altre}. " +
                "Modifica prima il tetto delle fasce, poi riprova.");
        }

        private async Task<(IReadOnlyList<TavoloSala> Tavoli, IReadOnlySet<long> Zone)> LeggiSalaAsync()
        {
            var zone = (await _zonaRepository.GetAllZoneAttiveAsync()).Select(z => z.Id).ToHashSet();
            var tavoli = (await _postazioneRepository.GetPostazioniAttiveAsync())
                .Select(p => new TavoloSala(p.Id, p.ZonaId, p.CapienzaMassima))
                .ToList();
            return (tavoli, zone);
        }

        private static string Descrivi(FasciaOraria f) =>
            $"{Italiano.DateTimeFormat.GetDayName(f.GiornoSettimana)} {f.OrarioInizio:HH\\:mm}-{f.OrarioFine:HH\\:mm}";
    }
}
