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
}
