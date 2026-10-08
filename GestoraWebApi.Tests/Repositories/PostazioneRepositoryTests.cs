using GestoraWebApi.Context;
using GestoraWebApi.Enums;
using GestoraWebApi.Models;
using GestoraWebApi.Repositories.Postazioni;
using Microsoft.EntityFrameworkCore;

namespace GestoraWebApi.Tests.Repositories;

/// <summary>
/// V2-009: quali prenotazioni bloccano una modifica al tavolo lo decide la query del repository
/// (stato e data), non il service. Con i mock nessun test la tocca: qui gira su un database
/// InMemory vero, come in <see cref="PrenotazioniRepositoryTests"/>.
/// </summary>
public class PostazioneRepositoryTests
{
    private static readonly DateOnly Oggi = new(2026, 9, 4);

    private static GestoraContext NewContext() =>
        new(new DbContextOptionsBuilder<GestoraContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    /// <summary>Sala minima: zona 1, tavoli 1 e 2, una fascia serale.</summary>
    private static void Sala(GestoraContext ctx)
    {
        ctx.Zone.Add(new Zona { Id = 1, Nome = "Sala" });
        ctx.Postazioni.AddRange(
            new Postazione { Id = 1, Numero = 1, CapienzaMassima = 4, ZonaId = 1, Attiva = true },
            new Postazione { Id = 2, Numero = 2, CapienzaMassima = 4, ZonaId = 1, Attiva = true });
        ctx.FasciaOrarie.Add(new FasciaOraria
        {
            Id = 1, GiornoSettimana = DayOfWeek.Friday, OrarioInizio = new TimeOnly(20, 0),
            OrarioFine = new TimeOnly(22, 0), MaxCoperti = 8, Attiva = true
        });
    }

    private static Prenotazione Prenotazione(long id, StatoPrenotazione stato, DateOnly data, long tavolo) => new()
    {
        Id = id,
        NumeroCoperti = 2,
        UserId = "u",
        Stato = stato,
        DataPrenotazione = data,
        FasciaOrariaId = 1,
        PrenotazioniPostazioni = new List<PrenotazionePostazione>
        {
            new() { PostazioneId = tavolo, NumeroPosti = 2, DataPrenotazione = data, FasciaOrariaId = 1 }
        }
    };

    [Fact]
    public async Task GetImpegniFuturiAsync_SoloPrenotazioniDaServire_DaOggiInPoi_SulTavolo()
    {
        using var ctx = NewContext();
        Sala(ctx);
        ctx.Prenotazioni.AddRange(
            Prenotazione(1, StatoPrenotazione.Attiva, Oggi, tavolo: 1),               // si'
            Prenotazione(2, StatoPrenotazione.InCorso, Oggi, tavolo: 1),              // si'
            Prenotazione(3, StatoPrenotazione.Completata, Oggi, tavolo: 1),           // gia' servita
            Prenotazione(4, StatoPrenotazione.NonPresentata, Oggi, tavolo: 1),        // chiusa
            Prenotazione(5, StatoPrenotazione.Attiva, Oggi.AddDays(-1), tavolo: 1),   // passata
            Prenotazione(6, StatoPrenotazione.Attiva, Oggi.AddDays(7), tavolo: 1),    // si'
            Prenotazione(7, StatoPrenotazione.Attiva, Oggi, tavolo: 2));              // altro tavolo
        await ctx.SaveChangesAsync();

        var impegni = await new PostazioneRepository(ctx).GetImpegniFuturiAsync(1, Oggi);

        Assert.Equal(new long[] { 1, 2, 6 }, impegni.Select(p => p.Id).OrderBy(id => id));
    }

    // Il service ricalcola la capienza dell'unione e scrive data e fascia nel messaggio: senza
    // tavoli e fascia caricati il controllo sulla riduzione dei posti lavorerebbe a vuoto.
    [Fact]
    public async Task GetImpegniFuturiAsync_PortaFasciaETavoli_DallaPiuVicina()
    {
        using var ctx = NewContext();
        Sala(ctx);
        ctx.Prenotazioni.AddRange(
            Prenotazione(1, StatoPrenotazione.Attiva, Oggi.AddDays(7), tavolo: 1),
            Prenotazione(2, StatoPrenotazione.Attiva, Oggi, tavolo: 1));
        await ctx.SaveChangesAsync();

        var impegni = await new PostazioneRepository(ctx).GetImpegniFuturiAsync(1, Oggi);

        Assert.Equal(new long[] { 2, 1 }, impegni.Select(p => p.Id));
        Assert.All(impegni, p =>
        {
            Assert.NotNull(p.FasciaOraria);
            Assert.All(p.PrenotazioniPostazioni, pp => Assert.Equal(4, pp.Postazione.CapienzaMassima));
        });
    }
}
