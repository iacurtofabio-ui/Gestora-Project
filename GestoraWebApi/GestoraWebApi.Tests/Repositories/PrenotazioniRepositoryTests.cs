using GestoraWebApi.Context;
using GestoraWebApi.Enums;
using GestoraWebApi.Models;
using GestoraWebApi.Repositories.Prenotazioni;
using Microsoft.EntityFrameworkCore;

namespace GestoraWebApi.Tests.Repositories;

/// <summary>
/// REV-053: due regole di dominio vivono dentro le query del repository, non nei service, e
/// con i mock nessun test le tocca — è il mock a decidere cosa torna. Qui il repository gira
/// su un database InMemory vero.
/// </summary>
public class PrenotazioniRepositoryTests
{
    private static readonly DateOnly Lunedi = new(2026, 9, 7);

    private static GestoraContext NewContext() =>
        new(new DbContextOptionsBuilder<GestoraContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static Prenotazione Prenotazione(long id, StatoPrenotazione stato, DateOnly data) => new()
    {
        Id = id,
        NumeroCoperti = 2,
        UserId = "u",
        Stato = stato,
        DataPrenotazione = data,
        FasciaOrariaId = 1,
        PrenotazioniPostazioni = new List<PrenotazionePostazione>()
    };

    /// <summary>
    /// È da qui che passa la disponibilità: se le annullate arrivassero al service, un annullo
    /// continuerebbe a occupare tavolo e coperti fino a fine giornata.
    /// </summary>
    [Fact]
    public async Task GetPrenotazioniByDataAsync_EscludeLeAnnullate()
    {
        using var ctx = NewContext();
        ctx.Prenotazioni.AddRange(
            Prenotazione(1, StatoPrenotazione.Attiva, Lunedi),
            Prenotazione(2, StatoPrenotazione.Annullata, Lunedi),
            Prenotazione(3, StatoPrenotazione.Completata, Lunedi));
        await ctx.SaveChangesAsync();

        var risultato = await new PrenotazioniRepository(ctx).GetPrenotazioniByDataAsync(Lunedi);

        Assert.Equal(new long[] { 1, 3 }, risultato.Select(p => p.Id).OrderBy(id => id));
    }

    [Fact]
    public async Task GetPrenotazioniByDataAsync_PrendeSoloLaDataRichiesta()
    {
        using var ctx = NewContext();
        ctx.Prenotazioni.AddRange(
            Prenotazione(1, StatoPrenotazione.Attiva, Lunedi),
            Prenotazione(2, StatoPrenotazione.Attiva, Lunedi.AddDays(1)));
        await ctx.SaveChangesAsync();

        var risultato = await new PrenotazioniRepository(ctx).GetPrenotazioniByDataAsync(Lunedi);

        Assert.Equal(1, Assert.Single(risultato).Id);
    }

    /// <summary>
    /// Le fasce proposte in creazione prenotazione arrivano da qui: una fascia disattivata o di
    /// un altro giorno non deve comparire, altrimenti il service la rifiuta dopo averla offerta.
    /// </summary>
    [Fact]
    public async Task GetFasceOrarieByDayAsync_SoloFasceAttiveDelGiorno_OrdinatePerOrarioInizio()
    {
        using var ctx = NewContext();
        ctx.FasciaOrarie.AddRange(
            new FasciaOraria { Id = 1, Attiva = true, GiornoSettimana = DayOfWeek.Monday, OrarioInizio = new TimeOnly(20, 0), OrarioFine = new TimeOnly(22, 0), MaxCoperti = 40 },
            new FasciaOraria { Id = 2, Attiva = true, GiornoSettimana = DayOfWeek.Monday, OrarioInizio = new TimeOnly(12, 0), OrarioFine = new TimeOnly(14, 0), MaxCoperti = 40 },
            new FasciaOraria { Id = 3, Attiva = false, GiornoSettimana = DayOfWeek.Monday, OrarioInizio = new TimeOnly(19, 0), OrarioFine = new TimeOnly(21, 0), MaxCoperti = 40 },
            new FasciaOraria { Id = 4, Attiva = true, GiornoSettimana = DayOfWeek.Tuesday, OrarioInizio = new TimeOnly(19, 0), OrarioFine = new TimeOnly(21, 0), MaxCoperti = 40 });
        await ctx.SaveChangesAsync();

        var risultato = await new PrenotazioniRepository(ctx).GetFasceOrarieByDayAsync(DayOfWeek.Monday);

        Assert.Equal(new long[] { 2, 1 }, risultato.Select(f => f.Id));
    }
}
