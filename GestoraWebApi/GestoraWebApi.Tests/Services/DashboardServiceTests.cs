using GestoraWebApi.Context;
using GestoraWebApi.Enums;
using GestoraWebApi.Models;
using GestoraWebApi.Services.Dashboard;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace GestoraWebApi.Tests.Services;

/// <summary>
/// REV-016: la dashboard deve ragionare sulla data italiana, non su quella UTC. Fra mezzanotte
/// e le due (ora legale) l'orologio UTC è ancora al giorno prima.
/// </summary>
public class DashboardServiceTests
{
    private static GestoraContext NewContext() =>
        new(new DbContextOptionsBuilder<GestoraContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public async Task Settimanale_NoShow_UsaLaDataItaliana_NonQuellaUtc()
    {
        // 14/06/2026 23:30 UTC == 15/06/2026 01:30 a Roma (CEST). Data italiana = 15, UTC = 14.
        var clock = new TestClock(new DateTime(2026, 6, 14, 23, 30, 0, DateTimeKind.Utc));

        using var ctx = NewContext();
        ctx.Prenotazioni.Add(new Prenotazione
        {
            Id = 1,
            NumeroCoperti = 2,
            UserId = "u",
            Stato = StatoPrenotazione.Attiva,
            DataPrenotazione = new DateOnly(2026, 6, 14) // "ieri" in ora italiana
        });
        await ctx.SaveChangesAsync();

        var service = new DashboardService(ctx, new Mock<ILogger<DashboardService>>().Object, clock);

        var res = await service.GetDashboardSettimanaleAsync(new DateOnly(2026, 6, 8));

        // Con la data italiana (15) la prenotazione del 14 è "conclusa e mai confermata" -> no-show 100%.
        // Con la vecchia logica UTC (14) la prenotazione non sarebbe stata contata (14 non è < 14).
        Assert.Equal(100, res.TassoNoShow);
    }

    // ══ REV-053 — gli aggregati della dashboard ═══════════════════════════════

    private static readonly DateOnly Lunedi = new(2026, 9, 7);

    /// <summary>Orologio fermo a un istante ampiamente successivo alle date usate nei test.</summary>
    private static TestClock ClockDopoLunedi() => new(new DateTime(2026, 9, 30, 12, 0, 0, DateTimeKind.Utc));

    private static DashboardService NewService(GestoraContext ctx, TestClock? clock = null) =>
        new(ctx, new Mock<ILogger<DashboardService>>().Object, clock ?? ClockDopoLunedi());

    private static Prenotazione Prenotazione(long id,
                                             StatoPrenotazione stato,
                                             int coperti,
                                             DateOnly data,
                                             long fasciaId = 1,
                                             params long[] postazioniIds) => new()
                                             {
                                                 Id = id,
                                                 NumeroCoperti = coperti,
                                                 UserId = "u",
                                                 Stato = stato,
                                                 DataPrenotazione = data,
                                                 FasciaOrariaId = fasciaId,
                                                 PrenotazioniPostazioni = postazioniIds
                                                     .Select(pid => new PrenotazionePostazione
                                                     {
                                                         PostazioneId = pid,
                                                         PrenotazioneId = id,
                                                         NumeroPosti = coperti,
                                                         DataPrenotazione = data,
                                                         FasciaOrariaId = fasciaId
                                                     })
                                                     .ToList()
                                             };

    private static FasciaOraria Fascia(long id, int maxCoperti, DayOfWeek giorno = DayOfWeek.Monday, bool attiva = true, int oraInizio = 19) => new()
    {
        Id = id,
        Attiva = attiva,
        GiornoSettimana = giorno,
        MaxCoperti = maxCoperti,
        OrarioInizio = new TimeOnly(oraInizio, 0),
        OrarioFine = new TimeOnly(oraInizio + 2, 0)
    };

    // ─── Panoramica giornaliera ───────────────────────────────────────────────

    [Fact]
    public async Task Giornaliera_ContaLePrenotazioniPerStato()
    {
        using var ctx = NewContext();
        ctx.Prenotazioni.AddRange(
            Prenotazione(1, StatoPrenotazione.Attiva, 2, Lunedi),
            Prenotazione(2, StatoPrenotazione.Attiva, 2, Lunedi),
            Prenotazione(3, StatoPrenotazione.InCorso, 4, Lunedi),
            Prenotazione(4, StatoPrenotazione.Completata, 3, Lunedi),
            Prenotazione(5, StatoPrenotazione.Annullata, 8, Lunedi),
            Prenotazione(6, StatoPrenotazione.Attiva, 2, Lunedi.AddDays(1))); // altro giorno
        await ctx.SaveChangesAsync();

        var res = await NewService(ctx).GetDashboardGiornalieroAsync(Lunedi);

        Assert.Equal(5, res.TotalePrenotazioni);
        Assert.Equal(2, res.PrenotazioniAttive);
        Assert.Equal(1, res.PrenotazioniInCorso);
        Assert.Equal(1, res.PrenotazioniCompletate);
        Assert.Equal(1, res.PrenotazioniAnnullate);
    }

    /// <summary>
    /// I coperti annullati sono tornati disponibili: contarli gonfierebbe il dato su cui il
    /// gestore decide se accettare un'altra prenotazione.
    /// </summary>
    [Fact]
    public async Task Giornaliera_ICopertiEscludonoLeAnnullate()
    {
        using var ctx = NewContext();
        ctx.Prenotazioni.AddRange(
            Prenotazione(1, StatoPrenotazione.Attiva, 4, Lunedi),
            Prenotazione(2, StatoPrenotazione.Annullata, 8, Lunedi));
        await ctx.SaveChangesAsync();

        var res = await NewService(ctx).GetDashboardGiornalieroAsync(Lunedi);

        Assert.Equal(4, res.TotaleCopertiPrenotati);
    }

    /// <summary>
    /// Occupato = c'è gente al tavolo (Attiva o In corso). Completata e Annullata hanno
    /// liberato il tavolo, e un tavolo dell'unione conta una volta sola.
    /// </summary>
    [Fact]
    public async Task Giornaliera_PostazioniOccupate_SoloAttiveEInCorso_SenzaDuplicati()
    {
        using var ctx = NewContext();
        ctx.Postazioni.AddRange(
            new Postazione { Id = 1, Numero = 1, CapienzaMassima = 4, Attiva = true, ZonaId = 1 },
            new Postazione { Id = 2, Numero = 2, CapienzaMassima = 4, Attiva = true, ZonaId = 1 },
            new Postazione { Id = 3, Numero = 3, CapienzaMassima = 4, Attiva = true, ZonaId = 1 },
            new Postazione { Id = 4, Numero = 4, CapienzaMassima = 4, Attiva = false, ZonaId = 1 }); // disattivato
        ctx.Prenotazioni.AddRange(
            Prenotazione(1, StatoPrenotazione.Attiva, 6, Lunedi, 1, 1, 2),      // unione: due tavoli
            Prenotazione(2, StatoPrenotazione.Annullata, 2, Lunedi, 1, 3),      // ha liberato il 3
            Prenotazione(3, StatoPrenotazione.Completata, 2, Lunedi, 1, 3));    // idem
        await ctx.SaveChangesAsync();

        var res = await NewService(ctx).GetDashboardGiornalieroAsync(Lunedi);

        Assert.Equal(3, res.TotalePostazioniAttive); // il 4 è disattivato
        Assert.Equal(2, res.PostazioniOccupate);
        Assert.Equal(1, res.PostazioniLibere);
    }

    [Fact]
    public async Task Giornaliera_CopertiPerFascia_SoloFasceAttiveDelGiornoDellaSettimana()
    {
        using var ctx = NewContext();
        ctx.FasciaOrarie.AddRange(
            Fascia(1, maxCoperti: 40, oraInizio: 19),
            Fascia(2, maxCoperti: 30, oraInizio: 12),
            Fascia(3, maxCoperti: 40, attiva: false, oraInizio: 15),          // disattivata
            Fascia(4, maxCoperti: 40, giorno: DayOfWeek.Tuesday, oraInizio: 19)); // altro giorno
        ctx.Prenotazioni.AddRange(
            Prenotazione(1, StatoPrenotazione.Attiva, 10, Lunedi, fasciaId: 1),
            Prenotazione(2, StatoPrenotazione.Annullata, 8, Lunedi, fasciaId: 1));
        await ctx.SaveChangesAsync();

        var res = await NewService(ctx).GetDashboardGiornalieroAsync(Lunedi);

        // Ordinate per orario di inizio: prima il pranzo (12), poi la cena (19).
        Assert.Equal(new long[] { 2, 1 }, res.CopertiPerFascia.Select(f => f.FasciaOrariaId));

        var cena = res.CopertiPerFascia.Single(f => f.FasciaOrariaId == 1);
        Assert.Equal(10, cena.CopertiPrenotati);   // l'annullata non conta
        Assert.Equal(30, cena.CopertiDisponibili);
        Assert.Equal(1, cena.NumeroPrenotazioni);
    }

    /// <summary>
    /// Il tetto può essere stato abbassato dopo le prenotazioni: i coperti disponibili non
    /// devono diventare negativi, il frontend li mostra così come sono.
    /// </summary>
    [Fact]
    public async Task Giornaliera_CopertiDisponibiliNonVannoSottoZero()
    {
        using var ctx = NewContext();
        ctx.FasciaOrarie.Add(Fascia(1, maxCoperti: 4));
        ctx.Prenotazioni.Add(Prenotazione(1, StatoPrenotazione.Attiva, 10, Lunedi));
        await ctx.SaveChangesAsync();

        var res = await NewService(ctx).GetDashboardGiornalieroAsync(Lunedi);

        Assert.Equal(0, res.CopertiPerFascia.Single().CopertiDisponibili);
    }

    [Fact]
    public async Task Giornaliera_GiornoVuoto_RestituisceContatoriAZeroSenzaErrori()
    {
        using var ctx = NewContext();

        var res = await NewService(ctx).GetDashboardGiornalieroAsync(Lunedi);

        Assert.Equal(0, res.TotalePrenotazioni);
        Assert.Equal(0, res.TotaleCopertiPrenotati);
        Assert.Equal(0, res.PostazioniOccupate);
        Assert.Empty(res.CopertiPerFascia);
    }

    // ─── Panoramica settimanale ───────────────────────────────────────────────

    [Fact]
    public async Task Settimanale_CopreSetteGiorniConINomiInItaliano()
    {
        using var ctx = NewContext();

        var res = await NewService(ctx).GetDashboardSettimanaleAsync(Lunedi);

        Assert.Equal(Lunedi, res.DataInizio);
        Assert.Equal(Lunedi.AddDays(6), res.DataFine);
        Assert.Equal(7, res.Giorni.Count);
        Assert.Equal("Lunedì", res.Giorni.First().GiornoNome);
        Assert.Equal("Domenica", res.Giorni.Last().GiornoNome);
    }

    [Fact]
    public async Task Settimanale_ConteggiaSoloLeDateDellaSettimanaRichiesta()
    {
        using var ctx = NewContext();
        ctx.Prenotazioni.AddRange(
            Prenotazione(1, StatoPrenotazione.Completata, 2, Lunedi),
            Prenotazione(2, StatoPrenotazione.Completata, 2, Lunedi.AddDays(6)),
            Prenotazione(3, StatoPrenotazione.Completata, 2, Lunedi.AddDays(7)),   // settimana dopo
            Prenotazione(4, StatoPrenotazione.Completata, 2, Lunedi.AddDays(-1))); // settimana prima
        await ctx.SaveChangesAsync();

        var res = await NewService(ctx).GetDashboardSettimanaleAsync(Lunedi);

        Assert.Equal(2, res.TotalePrenotazioni);
        Assert.Equal(4, res.TotaleCoperti);
    }

    [Fact]
    public async Task Settimanale_TassoAnnullamentoSulTotaleDellaSettimana()
    {
        using var ctx = NewContext();
        ctx.Prenotazioni.AddRange(
            Prenotazione(1, StatoPrenotazione.Completata, 2, Lunedi),
            Prenotazione(2, StatoPrenotazione.Completata, 2, Lunedi),
            Prenotazione(3, StatoPrenotazione.Completata, 2, Lunedi),
            Prenotazione(4, StatoPrenotazione.Annullata, 2, Lunedi));
        await ctx.SaveChangesAsync();

        var res = await NewService(ctx).GetDashboardSettimanaleAsync(Lunedi);

        Assert.Equal(25, res.TassoAnnullamento);
        Assert.Equal(6, res.TotaleCoperti); // l'annullata non porta coperti
    }

    /// <summary>
    /// Il no-show è la percentuale di prenotazioni concluse rimaste "Attiva": lo staff non ha
    /// mai confermato l'arrivo. Le annullate non fanno parte del denominatore.
    /// </summary>
    [Fact]
    public async Task Settimanale_TassoNoShow_SoloSulleConcluseNonAnnullate()
    {
        using var ctx = NewContext();
        ctx.Prenotazioni.AddRange(
            Prenotazione(1, StatoPrenotazione.Attiva, 2, Lunedi),      // conclusa e mai confermata
            Prenotazione(2, StatoPrenotazione.Completata, 2, Lunedi),  // conclusa regolarmente
            Prenotazione(3, StatoPrenotazione.Annullata, 2, Lunedi));  // fuori dal calcolo
        await ctx.SaveChangesAsync();

        var res = await NewService(ctx).GetDashboardSettimanaleAsync(Lunedi);

        Assert.Equal(50, res.TassoNoShow);
    }

    /// <summary>Una settimana ancora da venire non ha prenotazioni concluse: nessuna divisione per zero.</summary>
    [Fact]
    public async Task Settimanale_TassoNoShowAZero_QuandoLaSettimanaNonEAncoraConclusa()
    {
        using var ctx = NewContext();
        ctx.Prenotazioni.Add(Prenotazione(1, StatoPrenotazione.Attiva, 2, Lunedi));
        await ctx.SaveChangesAsync();

        // Orologio fermo al giorno prima: nessuna data della settimana è ancora passata.
        var clock = new TestClock(new DateTime(2026, 9, 6, 12, 0, 0, DateTimeKind.Utc));
        var res = await NewService(ctx, clock).GetDashboardSettimanaleAsync(Lunedi);

        Assert.Equal(0, res.TassoNoShow);
        Assert.Equal(0, res.TassoAnnullamento);
    }

    [Fact]
    public async Task Settimanale_DettaglioPerGiorno()
    {
        using var ctx = NewContext();
        ctx.Prenotazioni.AddRange(
            Prenotazione(1, StatoPrenotazione.Completata, 4, Lunedi),
            Prenotazione(2, StatoPrenotazione.Annullata, 8, Lunedi),
            Prenotazione(3, StatoPrenotazione.Completata, 2, Lunedi.AddDays(2)));
        await ctx.SaveChangesAsync();

        var res = await NewService(ctx).GetDashboardSettimanaleAsync(Lunedi);

        var lunedi = res.Giorni.First();
        Assert.Equal(2, lunedi.NumeroPrenotazioni);
        Assert.Equal(4, lunedi.NumeroCoperti); // l'annullata non porta coperti
        Assert.Equal(1, lunedi.Annullate);

        var mercoledi = res.Giorni.Single(g => g.Data == Lunedi.AddDays(2));
        Assert.Equal("Mercoledì", mercoledi.GiornoNome);
        Assert.Equal(2, mercoledi.NumeroCoperti);
    }
}
