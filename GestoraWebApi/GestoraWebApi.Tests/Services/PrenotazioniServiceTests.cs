using AutoMapper;
using GestoraWebApi.Context;
using GestoraWebApi.Enums;
using GestoraWebApi.Models;
using GestoraWebApi.Repositories.FasciaOrarie;
using GestoraWebApi.Repositories.Prenotazioni;
using GestoraWebApi.Repositories.Zone;
using GestoraWebApi.Services.PostazioneAssignment;
using GestoraWebApi.Services.Prenotazioni;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.InMemory.Diagnostics;
using Npgsql;
using Microsoft.Extensions.Logging;
using Moq;
using MockQueryable;
using GestoraWebApi.Services.LogActivity;
using GestoraWebApi.Services.Prenotazioni.DTOs;
using GestoraWebApi.Infrastructure.Exceptions;

namespace GestoraWebApi.Tests.Services;

public class PrenotazioniServiceTests
{
    private readonly Mock<IPrenotazioniRepository> _prenotazioniRepoMock;
    private readonly Mock<IPostazioneAssignmentService> _assignmentServiceMock;
    private readonly Mock<IFasciaOrariaRepository> _fasciaRepoMock;
    private readonly Mock<IZonaRepository> _zonaRepoMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<IHttpContextAccessor> _httpContextMock;
    private readonly Mock<ILogger<PrenotazioniService>> _loggerMock;
    private readonly GestoraContext _context;
    private readonly PrenotazioniService _service;
    private readonly Mock<ILogActivityService> _logActivityMock;

    public PrenotazioniServiceTests()
    {
        _prenotazioniRepoMock = new Mock<IPrenotazioniRepository>();
        _assignmentServiceMock = new Mock<IPostazioneAssignmentService>();
        _fasciaRepoMock = new Mock<IFasciaOrariaRepository>();
        _zonaRepoMock = new Mock<IZonaRepository>();
        _mapperMock = new Mock<IMapper>();
        _httpContextMock = new Mock<IHttpContextAccessor>();
        _loggerMock = new Mock<ILogger<PrenotazioniService>>();
        _logActivityMock = new Mock<ILogActivityService>();


        // ↓ BLOCCO DA AGGIUNGERE ↓
        // Ruolo Staff: i test di questa classe esercitano il percorso Admin/Staff (nessun
        // vincolo di ownership/cutoff, vedi RBAC-002) su prenotazioni non necessariamente
        // proprie dell'utente autenticato.
        var claims = new[] {
  new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "user-test-123"),
  new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, GestoraWebApi.Auth.Roles.Staff) };
        var identity = new
    System.Security.Claims.ClaimsIdentity(claims,
    "Test");
        var claimsPrincipal = new System.Security.
    Claims.ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext
        {
            User = claimsPrincipal
        };
        _httpContextMock.Setup(h =>
    h.HttpContext).Returns(httpContext);
        // ↑ FINE BLOCCO ↑

        // Il provider InMemory non supporta le transazioni e di default trasforma il warning
        // in eccezione. Il service ora apre una transazione esplicita (REV-003/REV-032): qui la
        // si ignora, la transazione vera e' verificata solo contro Postgres.
        var options = new DbContextOptionsBuilder<GestoraContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new GestoraContext(options);

        _service = new PrenotazioniService(
            _prenotazioniRepoMock.Object,
            _assignmentServiceMock.Object,
            _fasciaRepoMock.Object,
            _mapperMock.Object,
            _context,
            _httpContextMock.Object,
            _zonaRepoMock.Object,
            _loggerMock.Object,
            _logActivityMock.Object,
            new TestClock());

    }

    // ─── DeleteAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ThrowsKeyNotFoundException_WhenPrenotazioneNonEsiste()
    {
        // Arrange
        _prenotazioniRepoMock.Setup(r => r.GetByIdAsync(99))
                             .ReturnsAsync((Prenotazione?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.DeleteAsync(99));
    }

    [Fact]
    public async Task DeleteAsync_ThrowsConflictException_WhenStatoIsInCorso()
    {
        // Arrange
        var prenotazione = new Prenotazione { Id = 1, NumeroCoperti = 2, Stato = StatoPrenotazione.InCorso };
        _prenotazioniRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(prenotazione);

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() => _service.DeleteAsync(1));
    }

    [Fact]
    public async Task DeleteAsync_ThrowsConflictException_WhenStatoIsCompletata()
    {
        // Arrange
        var prenotazione = new Prenotazione { Id = 1, NumeroCoperti = 2, Stato = StatoPrenotazione.Completata };
        _prenotazioniRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(prenotazione);

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() => _service.DeleteAsync(1));
    }

    // ─── AnnullaPrenotazioneAsync ──────────────────────────────────────────────

    [Fact]
    public async Task AnnullaPrenotazioneAsync_ThrowsKeyNotFoundException_WhenNonEsiste()
    {
        // Arrange
        _prenotazioniRepoMock.Setup(r => r.GetTrackedByIdAsync(99))
                             .ReturnsAsync((Prenotazione?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.AnnullaPrenotazioneAsync(99));
    }

    [Fact]
    public async Task AnnullaPrenotazioneAsync_ThrowsConflictException_WhenGiaCompletata()
    {
        // Arrange
        var prenotazione = new Prenotazione { Id = 1, NumeroCoperti = 2, Stato = StatoPrenotazione.Completata };
        _prenotazioniRepoMock.Setup(r => r.GetTrackedByIdAsync(1)).ReturnsAsync(prenotazione);

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() => _service.AnnullaPrenotazioneAsync(1));
    }

    [Fact]
    public async Task AnnullaPrenotazioneAsync_SetsStatoAnnullata_WhenAttiva()
    {
        // Arrange
        var prenotazione = new Prenotazione { Id = 1, NumeroCoperti = 2, Stato = StatoPrenotazione.Attiva };
        _prenotazioniRepoMock.Setup(r => r.GetTrackedByIdAsync(1)).ReturnsAsync(prenotazione);

        // Act
        await _service.AnnullaPrenotazioneAsync(1);

        // Assert
        Assert.Equal(StatoPrenotazione.Annullata, prenotazione.Stato);
        _prenotazioniRepoMock.Verify(r => r.UpdateAsync(prenotazione), Times.Once);
    }

    // ─── AnnullaPrenotazioneAsync — RBAC-002 (cutoff self-service Cliente) ──────

    private void SetUserAsCliente(string userId)
    {
        var claims = new[] { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId) };
        var identity = new System.Security.Claims.ClaimsIdentity(claims, "Test");
        _httpContextMock.Setup(h => h.HttpContext).Returns(new DefaultHttpContext { User = new System.Security.Claims.ClaimsPrincipal(identity) });
    }

    [Fact]
    public async Task AnnullaPrenotazioneAsync_ThrowsUnauthorized_WhenClienteAnnullaPrenotazioneAltrui()
    {
        // Arrange
        SetUserAsCliente("user-test-123");
        var prenotazione = new Prenotazione { Id = 1, NumeroCoperti = 2, Stato = StatoPrenotazione.Attiva, UserId = "altro-utente" };
        _prenotazioniRepoMock.Setup(r => r.GetTrackedByIdAsync(1)).ReturnsAsync(prenotazione);

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenException>(() => _service.AnnullaPrenotazioneAsync(1));
    }

    [Fact]
    public async Task AnnullaPrenotazioneAsync_ThrowsConflictException_WhenClienteOltreCutoff()
    {
        // Arrange — fascia che inizia "ora" (stesso istante UTC, senza offset): il limite di
        // cutoff (2h prima) è già passato indipendentemente dal fuso orario di GetNowInRome.
        SetUserAsCliente("user-test-123");
        var fasciaAdesso = new FasciaOraria { Id = 1, OrarioInizio = TimeOnly.FromDateTime(DateTime.UtcNow) };
        var prenotazione = new Prenotazione
        {
            Id = 1,
            NumeroCoperti = 2,
            Stato = StatoPrenotazione.Attiva,
            UserId = "user-test-123",
            DataPrenotazione = DateOnly.FromDateTime(DateTime.UtcNow),
            FasciaOraria = fasciaAdesso
        };
        _prenotazioniRepoMock.Setup(r => r.GetTrackedByIdAsync(1)).ReturnsAsync(prenotazione);

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() => _service.AnnullaPrenotazioneAsync(1));
    }

    [Fact]
    public async Task AnnullaPrenotazioneAsync_SetsStatoAnnullata_WhenClienteEntroCutoff()
    {
        // Arrange — prenotazione fra 2 giorni: ampiamente oltre le 2 ore di cutoff, a
        // prescindere dal fuso orario.
        SetUserAsCliente("user-test-123");
        var fasciaFraDueGiorni = new FasciaOraria { Id = 1, OrarioInizio = new TimeOnly(20, 0) };
        var prenotazione = new Prenotazione
        {
            Id = 1,
            NumeroCoperti = 2,
            Stato = StatoPrenotazione.Attiva,
            UserId = "user-test-123",
            DataPrenotazione = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)),
            FasciaOraria = fasciaFraDueGiorni
        };
        _prenotazioniRepoMock.Setup(r => r.GetTrackedByIdAsync(1)).ReturnsAsync(prenotazione);

        // Act
        await _service.AnnullaPrenotazioneAsync(1);

        // Assert
        Assert.Equal(StatoPrenotazione.Annullata, prenotazione.Stato);
    }

    // ─── ConfermaPrenotazioneAsync ─────────────────────────────────────────────

    [Fact]
    public async Task ConfermaPrenotazioneAsync_ThrowsKeyNotFoundException_WhenNonEsiste()
    {
        // Arrange
        _prenotazioniRepoMock.Setup(r => r.GetTrackedByIdAsync(99))
                             .ReturnsAsync((Prenotazione?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.ConfermaPrenotazioneAsync(99));
    }

    [Fact]
    public async Task ConfermaPrenotazioneAsync_ThrowsConflictException_WhenNonAttiva()
    {
        // Arrange
        var prenotazione = new Prenotazione { Id = 1, NumeroCoperti = 2, Stato = StatoPrenotazione.InCorso };
        _prenotazioniRepoMock.Setup(r => r.GetTrackedByIdAsync(1)).ReturnsAsync(prenotazione);

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() => _service.ConfermaPrenotazioneAsync(1));
    }

    [Fact]
    public async Task ConfermaPrenotazioneAsync_SetsStatoInCorso_WhenAttiva()
    {
        // Arrange
        var prenotazione = new Prenotazione { Id = 1, NumeroCoperti = 2, Stato = StatoPrenotazione.Attiva };
        _prenotazioniRepoMock.Setup(r => r.GetTrackedByIdAsync(1)).ReturnsAsync(prenotazione);

        // Act
        await _service.ConfermaPrenotazioneAsync(1);

        // Assert
        Assert.Equal(StatoPrenotazione.InCorso, prenotazione.Stato);
        _prenotazioniRepoMock.Verify(r => r.UpdateAsync(prenotazione), Times.Once);
    }

    // ─── UpdateAsync — REV-002 (Staff/Admin su prenotazione altrui) + REV-006 (audit) ──

    private (Prenotazione prenotazione, PrenotazioneCreateDTO dto) ArrangeUpdateValido(string ownerUserId)
    {
        var data = new DateOnly(2026, 9, 7); // lunedì
        var fascia = new FasciaOraria { Id = 1, Attiva = true, GiornoSettimana = DayOfWeek.Monday, MaxCoperti = 50, OrarioInizio = new TimeOnly(19, 0), OrarioFine = new TimeOnly(21, 0) };
        var prenotazione = new Prenotazione
        {
            Id = 1,
            NumeroCoperti = 2,
            Stato = StatoPrenotazione.Attiva,
            UserId = ownerUserId,
            DataPrenotazione = data,
            FasciaOrariaId = 1,
            FasciaOraria = fascia,
            PrenotazioniPostazioni = new List<PrenotazionePostazione>()
        };
        var dto = new PrenotazioneCreateDTO { DataPrenotazione = data, NumeroCoperti = 2, FasciaOrariaId = 1 };

        _prenotazioniRepoMock.Setup(r => r.GetTrackedByIdAsync(1)).ReturnsAsync(prenotazione);
        _fasciaRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(fascia);
        _prenotazioniRepoMock.Setup(r => r.GetAllQueryableAsync())
                             .Returns(new List<Prenotazione>().AsQueryable().BuildMock());
        _context.Postazioni.Add(new Postazione { Id = 1, Numero = 1, CapienzaMassima = 4, Attiva = true, ZonaId = 1 });
        _context.SaveChanges();
        _assignmentServiceMock.Setup(s => s.AssegnaPostazioneDisponibileAsync(It.IsAny<PrenotazioneCreateDTO>(), 1))
                              .ReturnsAsync(new List<PostazioneAssegnata>
                              {
                                  new(new Postazione { Id = 1, Numero = 1, CapienzaMassima = 4, ZonaId = 1 }, 2)
                              });

        return (prenotazione, dto);
    }

    [Fact]
    public async Task UpdateAsync_ConsenteAStaffDiModificarePrenotazioneDiUnAltroUtente()
    {
        // principal di default = Staff, id "user-test-123"
        var (prenotazione, dto) = ArrangeUpdateValido(ownerUserId: "cliente-diverso");

        await _service.UpdateAsync(1, dto);

        _prenotazioniRepoMock.Verify(r => r.UpdateAsync(prenotazione), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_RegistraLAuditLog()
    {
        var (_, dto) = ArrangeUpdateValido(ownerUserId: "cliente-diverso");

        await _service.UpdateAsync(1, dto);

        _logActivityMock.Verify(l => l.LogAsync("user-test-123", It.Is<string>(m => m.Contains("Modificata prenotazione")), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ClienteNonPuoModificarePrenotazioneAltrui()
    {
        SetUserAsCliente("user-test-123");
        var (_, dto) = ArrangeUpdateValido(ownerUserId: "altro-utente");

        await Assert.ThrowsAsync<ForbiddenException>(() => _service.UpdateAsync(1, dto));
    }

    // ─── GetByIdAsync — REV-034 (dettaglio al Cliente proprietario) ────────────

    [Fact]
    public async Task GetByIdAsync_ClienteNonPuoLeggerePrenotazioneAltrui()
    {
        SetUserAsCliente("user-test-123");
        _prenotazioniRepoMock.Setup(r => r.GetByIdAsync(1))
                             .ReturnsAsync(new Prenotazione { Id = 1, NumeroCoperti = 2, UserId = "altro-utente" });

        await Assert.ThrowsAsync<ForbiddenException>(() => _service.GetByIdAsync(1));
    }

    [Fact]
    public async Task GetByIdAsync_ClienteLeggeLaPropriaPrenotazione()
    {
        SetUserAsCliente("user-test-123");
        var prenotazione = new Prenotazione { Id = 1, NumeroCoperti = 2, UserId = "user-test-123" };
        _prenotazioniRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(prenotazione);
        _mapperMock.Setup(m => m.Map<GestoraWebApi.Services.Prenotazioni.DTOs.PrenotazioneDTO>(prenotazione))
                   .Returns(new GestoraWebApi.Services.Prenotazioni.DTOs.PrenotazioneDTO { Id = 1 });

        var dto = await _service.GetByIdAsync(1);

        Assert.Equal(1, dto.Id);
    }

    // --- REV-003 - slot denormalizzato, conflitto sul tavolo, atomicita' ---

    private PrenotazioneCreateDTO ArrangeAddValido()
    {
        var data = new DateOnly(2026, 9, 7); // lunedì
        var fascia = new FasciaOraria { Id = 1, Attiva = true, GiornoSettimana = DayOfWeek.Monday, MaxCoperti = 50, OrarioInizio = new TimeOnly(19, 0), OrarioFine = new TimeOnly(21, 0) };

        _fasciaRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(fascia);
        _prenotazioniRepoMock.Setup(r => r.GetAllQueryableAsync())
                             .Returns(new List<Prenotazione>().AsQueryable().BuildMock());
        _context.Postazioni.Add(new Postazione { Id = 1, Numero = 1, CapienzaMassima = 4, Attiva = true, ZonaId = 1 });
        _context.SaveChanges();
        _assignmentServiceMock.Setup(s => s.AssegnaPostazioneDisponibileAsync(It.IsAny<PrenotazioneCreateDTO>(), null))
                              .ReturnsAsync(new List<PostazioneAssegnata>
                              {
                                  new(new Postazione { Id = 1, Numero = 1, CapienzaMassima = 4, ZonaId = 1 }, 2)
                              });

        return new PrenotazioneCreateDTO { DataPrenotazione = data, NumeroCoperti = 2, FasciaOrariaId = 1 };
    }

    /// <summary>
    /// Le due colonne denormalizzate sono la base dell'unique index: se non vengono valorizzate
    /// in scrittura, il vincolo a database non protegge nulla.
    /// </summary>
    [Fact]
    public async Task AddAsync_ValorizzaLoSlotSulleRigheJoin()
    {
        var dto = ArrangeAddValido();
        Prenotazione? salvata = null;
        _prenotazioniRepoMock.Setup(r => r.AddAsync(It.IsAny<Prenotazione>()))
                             .Callback<Prenotazione>(p => salvata = p)
                             .Returns(Task.CompletedTask);

        await _service.AddAsync(dto);

        Assert.NotNull(salvata);
        var riga = Assert.Single(salvata!.PrenotazioniPostazioni);
        Assert.Equal(dto.DataPrenotazione, riga.DataPrenotazione);
        Assert.Equal(dto.FasciaOrariaId, riga.FasciaOrariaId);
    }

    [Fact]
    public async Task UpdateAsync_ValorizzaLoSlotSulleRigheJoin()
    {
        var (prenotazione, dto) = ArrangeUpdateValido(ownerUserId: "cliente-diverso");

        await _service.UpdateAsync(1, dto);

        var riga = Assert.Single(prenotazione.PrenotazioniPostazioni);
        Assert.Equal(dto.DataPrenotazione, riga.DataPrenotazione);
        Assert.Equal(dto.FasciaOrariaId, riga.FasciaOrariaId);
    }

    private static DbUpdateException ViolazioneSlot() => new(
        "errore",
        new PostgresException(
            messageText: "duplicate key value violates unique constraint",
            severity: "ERROR",
            invariantSeverity: "ERROR",
            sqlState: "23505",
            constraintName: "UX_PrenotazionePostazione_Slot"));

    /// <summary>
    /// L'altro utente ha vinto la corsa sullo stesso tavolo: deve uscire un 409 leggibile
    /// (ConflictException), non un 500 con dentro il messaggio del driver.
    /// </summary>
    [Fact]
    public async Task AddAsync_ThrowsConflictException_QuandoIlTavoloVieneOccupatoNelFrattempo()
    {
        var dto = ArrangeAddValido();
        _prenotazioniRepoMock.Setup(r => r.AddAsync(It.IsAny<Prenotazione>()))
                             .ThrowsAsync(ViolazioneSlot());

        await Assert.ThrowsAsync<ConflictException>(() => _service.AddAsync(dto));
    }

    [Fact]
    public async Task UpdateAsync_ThrowsConflictException_QuandoIlTavoloVieneOccupatoNelFrattempo()
    {
        var (_, dto) = ArrangeUpdateValido(ownerUserId: "cliente-diverso");
        _prenotazioniRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Prenotazione>()))
                             .ThrowsAsync(ViolazioneSlot());

        await Assert.ThrowsAsync<ConflictException>(() => _service.UpdateAsync(1, dto));
    }

    /// <summary>
    /// Un errore di unicita' che non riguarda lo slot non e' un conflitto di tavolo: deve restare
    /// un errore interno (500), non travestirsi da 409.
    /// </summary>
    [Fact]
    public async Task AddAsync_NonTraduceUnaViolazioneDiAltriVincoli()
    {
        var dto = ArrangeAddValido();
        var altraViolazione = new DbUpdateException("errore",
            new PostgresException("duplicate", "ERROR", "ERROR", "23505", constraintName: "UX_QualcosAltro"));
        _prenotazioniRepoMock.Setup(r => r.AddAsync(It.IsAny<Prenotazione>()))
                             .ThrowsAsync(altraViolazione);

        await Assert.ThrowsAsync<DbUpdateException>(() => _service.AddAsync(dto));
    }

    /// <summary>
    /// REV-003: una prenotazione annullata libera il tavolo - le righe join vengono cancellate,
    /// non lasciate a occupare lo slot (con l'indice pieno bloccherebbero le prenotazioni future).
    /// </summary>
    [Fact]
    public async Task AnnullaPrenotazioneAsync_CancellaLeRigheJoin()
    {
        var prenotazione = new Prenotazione
        {
            Id = 1,
            NumeroCoperti = 2,
            Stato = StatoPrenotazione.Attiva,
            DataPrenotazione = new DateOnly(2026, 9, 7),
            FasciaOrariaId = 1,
            PrenotazioniPostazioni = new List<PrenotazionePostazione>
            {
                new() { PostazioneId = 1, PrenotazioneId = 1, NumeroPosti = 2, DataPrenotazione = new DateOnly(2026, 9, 7), FasciaOrariaId = 1 }
            }
        };
        _context.Prenotazioni.Attach(prenotazione);
        _prenotazioniRepoMock.Setup(r => r.GetTrackedByIdAsync(1)).ReturnsAsync(prenotazione);

        await _service.AnnullaPrenotazioneAsync(1);

        var riga = _context.ChangeTracker.Entries<PrenotazionePostazione>().Single();
        Assert.Equal(EntityState.Deleted, riga.State);
    }

    /// <summary>
    /// REV-032 (parziale): l'audit log sta dentro la stessa transazione della scrittura. Se il
    /// log fallisce l'operazione deve fallire, non restare scritta e non tracciata.
    /// </summary>
    [Fact]
    public async Task AddAsync_FallisceSeLAuditLogFallisce()
    {
        var dto = ArrangeAddValido();
        _prenotazioniRepoMock.Setup(r => r.AddAsync(It.IsAny<Prenotazione>())).Returns(Task.CompletedTask);
        _logActivityMock.Setup(l => l.LogAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
                        .ThrowsAsync(new Exception("log ko"));

        await Assert.ThrowsAsync<Exception>(() => _service.AddAsync(dto));
    }

    // --- REV-033 - NomeCliente e' un campo di Staff/Admin ---

    [Fact]
    public async Task AddAsync_IgnoraNomeCliente_QuandoAScrivereEIlCliente()
    {
        SetUserAsCliente("user-test-123");
        var dto = ArrangeAddValido();
        dto.NomeCliente = "Nome iniettato";
        Prenotazione? salvata = null;
        _prenotazioniRepoMock.Setup(r => r.AddAsync(It.IsAny<Prenotazione>()))
                             .Callback<Prenotazione>(p => salvata = p)
                             .Returns(Task.CompletedTask);

        await _service.AddAsync(dto);

        Assert.Null(salvata!.NomeCliente);
    }

    [Fact]
    public async Task AddAsync_ScriveNomeCliente_QuandoAScrivereEStaff()
    {
        // principal di default = Staff
        var dto = ArrangeAddValido();
        dto.NomeCliente = "Rossi (al telefono)";
        Prenotazione? salvata = null;
        _prenotazioniRepoMock.Setup(r => r.AddAsync(It.IsAny<Prenotazione>()))
                             .Callback<Prenotazione>(p => salvata = p)
                             .Returns(Task.CompletedTask);

        await _service.AddAsync(dto);

        Assert.Equal("Rossi (al telefono)", salvata!.NomeCliente);
    }

    /// <summary>
    /// Il Cliente non deve poter nemmeno cancellare il nome annotato dallo Staff modificando
    /// la propria prenotazione: il campo resta esattamente com'era.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_NonToccaNomeCliente_QuandoAModificareEIlCliente()
    {
        SetUserAsCliente("proprietario");
        var (prenotazione, dto) = ArrangeUpdateValido(ownerUserId: "proprietario");
        prenotazione.NomeCliente = "Annotato dallo Staff";
        dto.NomeCliente = "Nome iniettato";

        await _service.UpdateAsync(1, dto);

        Assert.Equal("Annotato dallo Staff", prenotazione.NomeCliente);
    }

    [Fact]
    public async Task UpdateAsync_ScriveNomeCliente_QuandoAModificareEStaff()
    {
        var (prenotazione, dto) = ArrangeUpdateValido(ownerUserId: "cliente-diverso");
        dto.NomeCliente = "Bianchi";

        await _service.UpdateAsync(1, dto);

        Assert.Equal("Bianchi", prenotazione.NomeCliente);
    }
}
