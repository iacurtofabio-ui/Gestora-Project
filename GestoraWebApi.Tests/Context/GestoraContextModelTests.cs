using GestoraWebApi.Auth;
using GestoraWebApi.Context;
using GestoraWebApi.Models;
using Microsoft.EntityFrameworkCore;

namespace GestoraWebApi.Tests.Context;

/// <summary>
/// Verifiche sulla configurazione del modello. Sono regole che vivono solo nel mapping EF:
/// nessun test di servizio le sfiorerebbe, e una modifica distratta al context passerebbe
/// inosservata fino a produzione.
/// </summary>
public class GestoraContextModelTests
{
    private static GestoraContext NuovoContext() =>
        new(new DbContextOptionsBuilder<GestoraContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    // REV-038: era Cascade, cioe' eliminare un utente cancellava tutto il suo storico di
    // prenotazioni - il dato su cui si contano coperti e presenze.
    [Fact]
    public void EliminareUnUtente_NonDeveCancellareLeSuePrenotazioni()
    {
        using var context = NuovoContext();

        var fk = context.Model
            .FindEntityType(typeof(Prenotazione))!
            .GetForeignKeys()
            .Single(f => f.PrincipalEntityType.ClrType == typeof(ApplicationUser));

        Assert.Equal(DeleteBehavior.Restrict, fk.DeleteBehavior);
    }

    // REV-037: la tabella dell'audit trail cresce a ogni operazione e non viene mai ripulita.
    // Senza indici, ogni lettura era una scansione completa.
    [Fact]
    public void AuditTrail_HaGliIndiciPerLeDueRicercheReali()
    {
        using var context = NuovoContext();

        var indici = context.Model.FindEntityType(typeof(Logging))!.GetIndexes().ToList();

        Assert.Contains(indici, i => i.Properties.Count == 1
                                     && i.Properties[0].Name == nameof(Logging.Timestamp));

        // Composto e in quest'ordine: serve sia il filtro sull'utente sia l'ordinamento per data.
        Assert.Contains(indici, i => i.Properties.Count == 2
                                     && i.Properties[0].Name == nameof(Logging.UserId)
                                     && i.Properties[1].Name == nameof(Logging.Timestamp));
    }

    [Fact]
    public void AuditTrail_HaLimitiDiLunghezzaSulleColonneDiTesto()
    {
        using var context = NuovoContext();

        var logging = context.Model.FindEntityType(typeof(Logging))!;

        Assert.Equal(450, logging.FindProperty(nameof(Logging.UserId))!.GetMaxLength());
        Assert.Equal(500, logging.FindProperty(nameof(Logging.Action))!.GetMaxLength());
        Assert.Equal(45, logging.FindProperty(nameof(Logging.IPAddress))!.GetMaxLength());
    }
}
