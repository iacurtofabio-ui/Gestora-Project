using GestoraWebApi.Auth;
using GestoraWebApi.Context;
using GestoraWebApi.Models;
using GestoraWebApi.Repositories.LogActivity;
using GestoraWebApi.Services.LogActivity;
using GestoraWebApi.Services.LogActivity.DTOs;
using Microsoft.EntityFrameworkCore;

namespace GestoraWebApi.Tests.Services;

/// <summary>
/// REV-037: fino alla Fase 7 l'audit trail si poteva solo scrivere. Questi test coprono la
/// lettura: filtri, ordinamento e paginazione.
/// </summary>
public class LogActivityServiceTests
{
    private readonly GestoraContext _context;
    private readonly LogActivityService _service;

    private static readonly DateTime Base = new(2026, 9, 4, 10, 0, 0, DateTimeKind.Utc);

    public LogActivityServiceTests()
    {
        var options = new DbContextOptionsBuilder<GestoraContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new GestoraContext(options);
        _service = new LogActivityService(new LogActivityRepository(_context), _context);
    }

    private void Semina(params Logging[] righe)
    {
        _context.LogActivities.AddRange(righe);
        _context.SaveChanges();
    }

    private static Logging Riga(long id, string userId, string azione, DateTime quando, string? ip = "1.2.3.4") =>
        new() { Id = id, UserId = userId, Action = azione, Timestamp = quando, IPAddress = ip };

    [Fact]
    public async Task GetLogAsync_RestituisceDalPiuRecente()
    {
        Semina(
            Riga(1, "u1", "Login", Base.AddHours(-2)),
            Riga(2, "u1", "Creata prenotazione", Base),
            Riga(3, "u1", "Annullata prenotazione", Base.AddHours(-1)));

        var result = await _service.GetLogAsync(new LogActivityQueryParams());

        Assert.Equal(new long[] { 2, 3, 1 }, result.Items.Select(i => i.Id));
        Assert.Equal(3, result.TotalCount);
    }

    [Fact]
    public async Task GetLogAsync_FiltraPerUtente()
    {
        Semina(
            Riga(1, "u1", "Login", Base),
            Riga(2, "u2", "Login", Base),
            Riga(3, "u1", "Logout", Base.AddMinutes(-5)));

        var result = await _service.GetLogAsync(new LogActivityQueryParams { UserId = "u1" });

        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, i => Assert.Equal("u1", i.UserId));
    }

    [Fact]
    public async Task GetLogAsync_FiltraPerIntervalloDiDate_EstremiInclusi()
    {
        Semina(
            Riga(1, "u1", "Troppo vecchia", Base.AddDays(-3)),
            Riga(2, "u1", "Dentro", Base.AddDays(-1)),
            Riga(3, "u1", "Sull'estremo", Base),
            Riga(4, "u1", "Troppo recente", Base.AddDays(1)));

        var result = await _service.GetLogAsync(new LogActivityQueryParams
        {
            Da = Base.AddDays(-1),
            A = Base
        });

        Assert.Equal(new long[] { 3, 2 }, result.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task GetLogAsync_CercaNelTestoDellAzione()
    {
        Semina(
            Riga(1, "u1", "Creata prenotazione per data 2026-09-10", Base),
            Riga(2, "u1", "Login", Base.AddMinutes(-1)));

        var result = await _service.GetLogAsync(new LogActivityQueryParams { Azione = "prenotazione" });

        Assert.Single(result.Items);
        Assert.Equal(1, result.Items[0].Id);
    }

    [Fact]
    public async Task GetLogAsync_Pagina_SenzaDuplicatiNeBuchi()
    {
        // Tutte nello stesso istante: e' il caso in cui il solo Timestamp non basta a stabilire
        // un ordine, lo stesso problema di REV-020 sulle prenotazioni.
        Semina(
            Riga(1, "u1", "A", Base),
            Riga(2, "u1", "B", Base),
            Riga(3, "u1", "C", Base),
            Riga(4, "u1", "D", Base));

        var pagina1 = await _service.GetLogAsync(new LogActivityQueryParams { Page = 1, PageSize = 2 });
        var pagina2 = await _service.GetLogAsync(new LogActivityQueryParams { Page = 2, PageSize = 2 });

        var visti = pagina1.Items.Concat(pagina2.Items).Select(i => i.Id).ToList();

        Assert.Equal(4, visti.Distinct().Count());
        Assert.Equal(new long[] { 4, 3 }, pagina1.Items.Select(i => i.Id));
        Assert.Equal(new long[] { 2, 1 }, pagina2.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task GetLogAsync_RiportaIlNomeUtente_QuandoLUtenteEsiste()
    {
        _context.Users.Add(new ApplicationUser { Id = "u1", UserName = "mario", Email = "m@x.it" });
        _context.SaveChanges();
        Semina(Riga(1, "u1", "Login", Base));

        var result = await _service.GetLogAsync(new LogActivityQueryParams());

        Assert.Equal("mario", result.Items[0].UserName);
    }

    // La traccia deve sopravvivere all'utente: e' proprio il motivo per cui Logging non ha una
    // chiave esterna verso Utenti. Una riga il cui autore non esiste piu' resta leggibile, con
    // il solo nome mancante.
    [Fact]
    public async Task GetLogAsync_TieneLaRiga_ancheSeLUtenteNonEsistePiu()
    {
        Semina(Riga(1, "utente-cancellato", "Login", Base));

        var result = await _service.GetLogAsync(new LogActivityQueryParams());

        Assert.Single(result.Items);
        Assert.Equal("utente-cancellato", result.Items[0].UserId);
        Assert.Null(result.Items[0].UserName);
    }

    [Fact]
    public async Task GetLogAsync_ListaVuota_NonEUnErrore()
    {
        var result = await _service.GetLogAsync(new LogActivityQueryParams { UserId = "nessuno" });

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(201, 200)]
    public void PageSize_RestaDentroILimiti(int richiesto, int atteso)
    {
        Assert.Equal(atteso, new LogActivityQueryParams { PageSize = richiesto }.PageSize);
    }
}
