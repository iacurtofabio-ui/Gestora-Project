using GestoraWebApi.Enums;
using GestoraWebApi.Models;
using GestoraWebApi.Repositories.Postazioni;
using GestoraWebApi.Repositories.Prenotazioni;
using GestoraWebApi.Repositories.Zone;
using GestoraWebApi.Services.Disponibilita;
using GestoraWebApi.Services.PostazioneAssignment;
using GestoraWebApi.Services.PrenotazioniPostazioni;
using Moq;

namespace GestoraWebApi.Tests.Services;

/// <summary>
/// Test di <see cref="DisponibilitaService"/> — checkpoint 2c: la verifica di disponibilità
/// deve usare lo stesso motore <see cref="AssegnazioneTavoli"/> dell'assegnazione e basare i
/// posti residui sul tetto della fascia (<c>MaxCoperti</c>), non sulla somma dei tavoli.
/// </summary>
public class DisponibilitaServiceTests
{
    private static readonly DateOnly Lunedi = new(2026, 9, 7); // è un lunedì

    private readonly Mock<IPrenotazioniRepository> _prenotazioni = new();
    private readonly Mock<IPostazioneRepository> _postazioni = new();
    private readonly Mock<IZonaRepository> _zone = new();

    private DisponibilitaService CreateService() =>
        new(_prenotazioni.Object, _postazioni.Object, _zone.Object);

    private static Zona Zona(long id, bool attiva = true) => new() { Id = id, Nome = $"Zona {id}", Attiva = attiva };

    private static Postazione Tavolo(long id, int capienza, long zonaId = 1) =>
        new() { Id = id, Numero = (int)id, CapienzaMassima = capienza, Attiva = true, ZonaId = zonaId };

    private static FasciaOraria Fascia(long id, int maxCoperti) => new()
    {
        Id = id,
        OrarioInizio = new TimeOnly(19, 0),
        OrarioFine = new TimeOnly(21, 0),
        GiornoSettimana = DayOfWeek.Monday,
        MaxCoperti = maxCoperti,
        Attiva = true
    };

    private static Prenotazione Prenotazione(long fasciaId, int coperti, params (long postazioneId, int posti)[] tavoli) => new()
    {
        NumeroCoperti = coperti,
        FasciaOrariaId = fasciaId,
        Stato = StatoPrenotazione.Attiva,
        PrenotazioniPostazioni = tavoli
            .Select(t => new PrenotazionePostazione { PostazioneId = t.postazioneId, NumeroPosti = t.posti })
            .ToList()
    };

    private void Setup(
        IEnumerable<FasciaOraria> fasce,
        IEnumerable<Postazione> postazioniAttive,
        IEnumerable<Prenotazione> prenotazioni,
        IEnumerable<Zona>? zoneAttive = null)
    {
        _prenotazioni.Setup(r => r.GetFasceOrarieByDayAsync(It.IsAny<DayOfWeek>())).ReturnsAsync(fasce.ToList());
        _prenotazioni.Setup(r => r.GetPrenotazioniByDataAsync(It.IsAny<DateOnly>())).ReturnsAsync(prenotazioni.ToList());
        _postazioni.Setup(r => r.GetPostazioniAttiveAsync()).ReturnsAsync(postazioniAttive.ToList());
        _zone.Setup(r => r.GetAllZoneAttiveAsync())
             .ReturnsAsync((zoneAttive ?? new[] { Zona(1) }).ToList());
    }

    private static FasciaDisponibilitaDTO Fascia(DisponibilitaResponseDTO r, long id) =>
        r.Fasce.Single(f => f.FasciaOrariaId == id);

    [Fact]
    public async Task PostiResidui_BasatiSulTettoDellaFascia_NonSullaSommaDeiTavoli()
    {
        // Tetto 6, tavoli in sala per 100 posti totali, 4 coperti già prenotati.
        Setup(
            fasce: new[] { Fascia(1, maxCoperti: 6) },
            postazioniAttive: new[] { Tavolo(1, 50), Tavolo(2, 50) },
            prenotazioni: new[] { Prenotazione(fasciaId: 1, coperti: 4, (1, 4)) });

        var res = await CreateService().CheckDisponibilitaAsync(new CheckDisponibilitaDTO
        {
            DataPrenotazione = Lunedi,
            NumeroCoperti = 2
        });

        Assert.Equal(2, Fascia(res, 1).PostiResiduiFascia);
    }

    [Fact]
    public async Task TettoEsaurito_MarcaNonDisponibile_ConMessaggioSullaCapienza()
    {
        Setup(
            fasce: new[] { Fascia(1, maxCoperti: 10) },
            postazioniAttive: new[] { Tavolo(1, 4), Tavolo(2, 4), Tavolo(3, 4) },
            prenotazioni: new[] { Prenotazione(fasciaId: 1, coperti: 10, (1, 4), (2, 4), (3, 2)) });

        var res = await CreateService().CheckDisponibilitaAsync(new CheckDisponibilitaDTO
        {
            DataPrenotazione = Lunedi,
            NumeroCoperti = 2
        });

        var fascia = Fascia(res, 1);
        Assert.False(fascia.DisponibilePerRichiesta);
        Assert.Equal(0, fascia.PostiResiduiFascia);
        Assert.Contains("capienza", fascia.Messaggio, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TettoLibero_MaTavoliInsufficienti_MessaggioDistingueILDueCasi()
    {
        // Tetto ampio (50), pochi coperti prenotati, ma resta libero solo un tavolo da 2
        // e la richiesta è per 8 persone.
        Setup(
            fasce: new[] { Fascia(1, maxCoperti: 50) },
            postazioniAttive: new[] { Tavolo(1, 8), Tavolo(2, 2) },
            prenotazioni: new[] { Prenotazione(fasciaId: 1, coperti: 6, (1, 6)) });

        var res = await CreateService().CheckDisponibilitaAsync(new CheckDisponibilitaDTO
        {
            DataPrenotazione = Lunedi,
            NumeroCoperti = 8
        });

        var fascia = Fascia(res, 1);
        Assert.False(fascia.DisponibilePerRichiesta);
        Assert.True(fascia.PostiResiduiFascia >= 8); // il tetto lascerebbe spazio
        Assert.Contains("tavoli", fascia.Messaggio, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task StessaLogicaDellAssegnazione_LeCombinazioniCoincidonoConIlMotore()
    {
        var libere = new List<Postazione> { Tavolo(1, 8), Tavolo(2, 2), Tavolo(3, 2) };
        Setup(
            fasce: new[] { Fascia(1, maxCoperti: 50) },
            postazioniAttive: libere,
            prenotazioni: Array.Empty<Prenotazione>());

        var res = await CreateService().CheckDisponibilitaAsync(new CheckDisponibilitaDTO
        {
            DataPrenotazione = Lunedi,
            NumeroCoperti = 6
        });

        var attesa = AssegnazioneTavoli.TrovaMigliorCombinazione(libere, 6)!.Select(p => p.Id).OrderBy(x => x);
        var effettiva = Fascia(res, 1).Postazioni.Select(p => p.PostazioneId).OrderBy(x => x);
        Assert.Equal(attesa, effettiva);
    }

    [Fact]
    public async Task EscludeTavoliInZonaDisattivata()
    {
        Setup(
            fasce: new[] { Fascia(1, maxCoperti: 50) },
            postazioniAttive: new[] { Tavolo(1, 4, zonaId: 1), Tavolo(2, 4, zonaId: 2) },
            prenotazioni: Array.Empty<Prenotazione>(),
            zoneAttive: new[] { Zona(1) }); // zona 2 disattivata

        var res = await CreateService().CheckDisponibilitaAsync(new CheckDisponibilitaDTO
        {
            DataPrenotazione = Lunedi,
            NumeroCoperti = 4
        });

        Assert.All(Fascia(res, 1).Postazioni, p => Assert.NotEqual(2, p.PostazioneId));
    }

    [Fact]
    public async Task EscludePostazioniGiaOccupateNellaFascia()
    {
        Setup(
            fasce: new[] { Fascia(1, maxCoperti: 50) },
            postazioniAttive: new[] { Tavolo(1, 4), Tavolo(2, 4) },
            prenotazioni: new[] { Prenotazione(fasciaId: 1, coperti: 4, (1, 4)) });

        var res = await CreateService().CheckDisponibilitaAsync(new CheckDisponibilitaDTO
        {
            DataPrenotazione = Lunedi,
            NumeroCoperti = 4
        });

        var fascia = Fascia(res, 1);
        Assert.True(fascia.DisponibilePerRichiesta);
        Assert.All(fascia.Postazioni, p => Assert.NotEqual(1, p.PostazioneId));
    }
}
