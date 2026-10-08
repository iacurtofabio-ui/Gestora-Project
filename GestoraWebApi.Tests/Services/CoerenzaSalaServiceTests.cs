using GestoraWebApi.Infrastructure.Exceptions;
using GestoraWebApi.Models;
using GestoraWebApi.Repositories.FasciaOrarie;
using GestoraWebApi.Repositories.Postazioni;
using GestoraWebApi.Repositories.Zone;
using GestoraWebApi.Services.Sala;
using Moq;

namespace GestoraWebApi.Tests.Services;

/// <summary>
/// V2-007 — il tetto di una fascia attiva non può superare i posti della sala (tavoli attivi in
/// zone attive, somma semplice). Si controlla quando si configura: salvando una fascia e
/// modificando tavoli o zone.
/// </summary>
public class CoerenzaSalaServiceTests
{
    private readonly Mock<IPostazioneRepository> _postazioni = new();
    private readonly Mock<IZonaRepository> _zone = new();
    private readonly Mock<IFasciaOrariaRepository> _fasce = new();

    private CoerenzaSalaService CreateService() => new(_postazioni.Object, _zone.Object, _fasce.Object);

    private static Postazione Tavolo(long id, int capienza, long zonaId = 1) =>
        new() { Id = id, Numero = (int)id, CapienzaMassima = capienza, Attiva = true, ZonaId = zonaId };

    private static FasciaOraria Fascia(long id, int maxCoperti, DayOfWeek giorno = DayOfWeek.Saturday) => new()
    {
        Id = id,
        GiornoSettimana = giorno,
        OrarioInizio = new TimeOnly(19, 0),
        OrarioFine = new TimeOnly(23, 0),
        MaxCoperti = maxCoperti,
        Attiva = true
    };

    private void Sala(IEnumerable<Postazione> tavoliAttivi, IEnumerable<long>? zoneAttive = null, IEnumerable<FasciaOraria>? fasceAttive = null)
    {
        _postazioni.Setup(r => r.GetPostazioniAttiveAsync()).ReturnsAsync(tavoliAttivi.ToList());
        _zone.Setup(r => r.GetAllZoneAttiveAsync())
             .ReturnsAsync((zoneAttive ?? new long[] { 1 }).Select(id => new Zona { Id = id, Nome = $"Zona {id}", Attiva = true }).ToList());
        _fasce.Setup(r => r.GetFasceAttiveAsync()).ReturnsAsync((fasceAttive ?? Array.Empty<FasciaOraria>()).ToList());
    }

    // ── Conteggio ───────────────────────────────────────────────────────────

    [Fact]
    public void Posti_SommaSemplice_SenzaBonusTestate()
    {
        // Due tavoli da 2: 4 posti, non 6 (il bonus vale solo per un unico gruppo che li unisce).
        var posti = CapienzaSala.Posti(new[] { new TavoloSala(1, 1, 2), new TavoloSala(2, 1, 2) }, new HashSet<long> { 1 });

        Assert.Equal(4, posti);
    }

    [Fact]
    public void Posti_IgnoraITavoliDelleZoneNonAttive()
    {
        var posti = CapienzaSala.Posti(new[] { new TavoloSala(1, 1, 4), new TavoloSala(2, 2, 6) }, new HashSet<long> { 1 });

        Assert.Equal(4, posti);
    }

    // ── Salvataggio di una fascia ───────────────────────────────────────────

    [Fact]
    public async Task VerificaTetto_EntroIPosti_Passa()
    {
        Sala(new[] { Tavolo(1, 4), Tavolo(2, 4) });

        await CreateService().VerificaTettoFasciaAsync(8);
    }

    [Fact]
    public async Task VerificaTetto_OltreIPosti_RifiutaConIPostiNelMessaggio()
    {
        // Il caso di V2-007: in sala 2 tavoli da 2, fascia con tetto 60.
        Sala(new[] { Tavolo(1, 2), Tavolo(2, 2) });

        var ex = await Assert.ThrowsAsync<ConflictException>(() => CreateService().VerificaTettoFasciaAsync(60));

        Assert.Contains("60 coperti", ex.Message);
        Assert.Contains("(4,", ex.Message);
    }

    [Fact]
    public async Task VerificaTetto_SalaVuota_SpiegaDiCrearePrimaITavoli()
    {
        // Primo avvio del locale: si parte dalle fasce, ma i tavoli non ci sono ancora.
        Sala(Array.Empty<Postazione>());

        var ex = await Assert.ThrowsAsync<ConflictException>(() => CreateService().VerificaTettoFasciaAsync(10));

        Assert.Contains("crea prima le zone e i tavoli", ex.Message);
    }

    // ── Modifica di tavoli o zone ───────────────────────────────────────────

    [Fact]
    public async Task VerificaModifica_PostiSottoIlTettoDiUnaFascia_RifiutaNominandolaFascia()
    {
        // 12 posti, fascia del sabato con tetto 10: togliere il tavolo da 4 porta a 8.
        Sala(new[] { Tavolo(1, 4), Tavolo(2, 8) }, fasceAttive: new[] { Fascia(1, 10) });

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            CreateService().VerificaModificaSalaAsync(t => t.Where(x => x.Id != 1)));

        Assert.Equal(
            "Errore: i posti della sala non possono scendere sotto il tetto delle fasce. " +
            "In questo caso fasce: sabato 19:00-23:00 (10 coperti). " +
            "Modifica prima il tetto delle fasce, poi riprova.",
            ex.Message);
    }

    [Fact]
    public async Task VerificaModifica_PostiRestanoSopraITetti_Passa()
    {
        Sala(new[] { Tavolo(1, 4), Tavolo(2, 8) }, fasceAttive: new[] { Fascia(1, 8) });

        await CreateService().VerificaModificaSalaAsync(t => t.Where(x => x.Id != 1));
    }

    [Fact]
    public async Task VerificaModifica_ZonaDisattivata_ContaComeTavoliTolti()
    {
        Sala(new[] { Tavolo(1, 6, zonaId: 1), Tavolo(2, 6, zonaId: 2) },
             zoneAttive: new long[] { 1, 2 },
             fasceAttive: new[] { Fascia(1, 10) });

        await Assert.ThrowsAsync<ConflictException>(() =>
            CreateService().VerificaModificaSalaAsync(t => t, zone => zone.Where(id => id != 2)));
    }

    [Fact]
    public async Task VerificaModifica_CheNonTogliePosti_PassaAncheSeLaSalaEraGiaIncoerente()
    {
        // Sala da 4 con una fascia da 60 (dati nati prima della regola): aggiungere un tavolo
        // deve restare possibile, anzi è proprio il modo di sistemare le cose.
        Sala(new[] { Tavolo(1, 4) }, fasceAttive: new[] { Fascia(1, 60) });

        await CreateService().VerificaModificaSalaAsync(t => t.Append(new TavoloSala(2, 1, 6)));
    }

    [Fact]
    public async Task VerificaModifica_PiuFasceOltre_LeElencaTutteDallaPiuAlta()
    {
        Sala(new[] { Tavolo(1, 10), Tavolo(2, 10) },
             fasceAttive: new[] { Fascia(1, 15, DayOfWeek.Friday), Fascia(2, 18, DayOfWeek.Saturday), Fascia(3, 5) });

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            CreateService().VerificaModificaSalaAsync(t => t.Where(x => x.Id != 1)));

        // La fascia da 5 sta nei 10 posti rimasti: non blocca e non si nomina.
        Assert.Contains("fasce: sabato 19:00-23:00 (18 coperti), venerdì 19:00-23:00 (15 coperti). ", ex.Message);
        Assert.DoesNotContain("(5 coperti)", ex.Message);
    }

    [Fact]
    public async Task VerificaModifica_PiuDiTreFasceOltre_NeNominaTreEContaLeAltre()
    {
        Sala(new[] { Tavolo(1, 10), Tavolo(2, 10) },
             fasceAttive: new[]
             {
                 Fascia(1, 11, DayOfWeek.Monday), Fascia(2, 12, DayOfWeek.Tuesday),
                 Fascia(3, 13, DayOfWeek.Wednesday), Fascia(4, 14, DayOfWeek.Thursday)
             });

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            CreateService().VerificaModificaSalaAsync(t => t.Where(x => x.Id != 1)));

        Assert.Contains("(12 coperti) e altre 1. ", ex.Message);
        Assert.DoesNotContain("lunedì", ex.Message);
    }
}
