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

    /// <summary>
    /// Un lunedì abbondantemente futuro. Il service usa l'orologio reale (<see cref="TestClock"/>
    /// senza istante fisso), quindi una data scritta a mano prima o poi diventa passata e fa
    /// fallire da sola il controllo di cutoff: va calcolata a ogni esecuzione.
    /// </summary>
    private static readonly DateOnly DataLunediFuturo = ProssimoLunedi();

    private static DateOnly ProssimoLunedi()
    {
        var data = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7);
        while (data.DayOfWeek != DayOfWeek.Monday)
            data = data.AddDays(1);
        return data;
    }

    private (Prenotazione prenotazione, PrenotazioneCreateDTO dto) ArrangeUpdateValido(string ownerUserId, int maxCoperti = 50, params Prenotazione[] prenotazioniEsistenti)
    {
        var data = DataLunediFuturo;
        var fascia = new FasciaOraria { Id = 1, Attiva = true, GiornoSettimana = DayOfWeek.Monday, MaxCoperti = maxCoperti, OrarioInizio = new TimeOnly(19, 0), OrarioFine = new TimeOnly(21, 0) };
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
                             .Returns(prenotazioniEsistenti.ToList().AsQueryable().BuildMock());
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

    private PrenotazioneCreateDTO ArrangeAddValido(int maxCoperti = 50, params Prenotazione[] prenotazioniEsistenti)
    {
        var data = DataLunediFuturo;
        var fascia = new FasciaOraria { Id = 1, Attiva = true, GiornoSettimana = DayOfWeek.Monday, MaxCoperti = maxCoperti, OrarioInizio = new TimeOnly(19, 0), OrarioFine = new TimeOnly(21, 0) };

        _fasciaRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(fascia);
        _prenotazioniRepoMock.Setup(r => r.GetAllQueryableAsync())
                             .Returns(prenotazioniEsistenti.ToList().AsQueryable().BuildMock());
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

    // ══ REV-051 — il flusso di prenotazione: creazione e modifica ═════════════

    private static Prenotazione PrenotazioneEsistente(long id,
                                                      int coperti,
                                                      StatoPrenotazione stato = StatoPrenotazione.Attiva,
                                                      string userId = "utente-terzo",
                                                      DateOnly? data = null) =>
        new()
        {
            Id = id,
            NumeroCoperti = coperti,
            Stato = stato,
            UserId = userId,
            DataPrenotazione = data ?? DataLunediFuturo,
            FasciaOrariaId = 1,
            PrenotazioniPostazioni = new List<PrenotazionePostazione>()
        };

    private static FasciaOraria Fascia(bool attiva = true, DayOfWeek giorno = DayOfWeek.Monday, int maxCoperti = 50) =>
        new() { Id = 1, Attiva = attiva, GiornoSettimana = giorno, MaxCoperti = maxCoperti, OrarioInizio = new TimeOnly(19, 0), OrarioFine = new TimeOnly(21, 0) };

    // ─── Capienza della fascia (tetto MaxCoperti) ─────────────────────────────

    /// <summary>
    /// Il tetto è sui coperti già impegnati, non sul numero di prenotazioni: il messaggio deve
    /// dire quanti posti restano, perché è l'informazione con cui il cliente riprova.
    /// </summary>
    [Fact]
    public async Task AddAsync_ThrowsConflictException_QuandoICopertiSuperanoIlTettoDellaFascia()
    {
        var dto = ArrangeAddValido(maxCoperti: 10, PrenotazioneEsistente(50, coperti: 8));
        dto.NumeroCoperti = 4;

        var ex = await Assert.ThrowsAsync<ConflictException>(() => _service.AddAsync(dto));

        Assert.Contains("(2)", ex.Message);
    }

    [Fact]
    public async Task AddAsync_MessaggioDedicato_QuandoIlTettoDellaFasciaEEsaurito()
    {
        var dto = ArrangeAddValido(maxCoperti: 10, PrenotazioneEsistente(50, coperti: 10));
        dto.NumeroCoperti = 2;

        var ex = await Assert.ThrowsAsync<ConflictException>(() => _service.AddAsync(dto));

        Assert.Contains("capienza massima", ex.Message);
    }

    [Fact]
    public async Task AddAsync_AccettaICopertiCheRiempionoEsattamenteLaFascia()
    {
        var dto = ArrangeAddValido(maxCoperti: 10, PrenotazioneEsistente(50, coperti: 6));
        dto.NumeroCoperti = 4;
        _prenotazioniRepoMock.Setup(r => r.AddAsync(It.IsAny<Prenotazione>())).Returns(Task.CompletedTask);

        await _service.AddAsync(dto);

        _prenotazioniRepoMock.Verify(r => r.AddAsync(It.IsAny<Prenotazione>()), Times.Once);
    }

    /// <summary>Una prenotazione annullata ha restituito i suoi coperti al tetto della fascia.</summary>
    [Fact]
    public async Task AddAsync_NonConteggiaLeAnnullate_NelTettoDellaFascia()
    {
        var dto = ArrangeAddValido(maxCoperti: 10, PrenotazioneEsistente(50, coperti: 10, stato: StatoPrenotazione.Annullata));
        dto.NumeroCoperti = 10;
        _prenotazioniRepoMock.Setup(r => r.AddAsync(It.IsAny<Prenotazione>())).Returns(Task.CompletedTask);

        await _service.AddAsync(dto);

        _prenotazioniRepoMock.Verify(r => r.AddAsync(It.IsAny<Prenotazione>()), Times.Once);
    }

    /// <summary>
    /// In modifica la prenotazione stessa non deve consumare due volte il tetto: altrimenti
    /// cambiare le note di una prenotazione che riempie la fascia risulterebbe impossibile.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_NonConteggiaSeStessa_NelTettoDellaFascia()
    {
        var (prenotazione, dto) = ArrangeUpdateValido("cliente-diverso", maxCoperti: 10, PrenotazioneEsistente(1, coperti: 8));
        dto.NumeroCoperti = 10;

        await _service.UpdateAsync(1, dto);

        Assert.Equal(10, prenotazione.NumeroCoperti);
    }

    // ─── Giorno e stato della fascia ──────────────────────────────────────────

    /// <summary>
    /// Una fascia del martedì su una data di lunedì è la trappola più facile del form: il
    /// messaggio deve nominare il giorno giusto, in italiano.
    /// </summary>
    [Fact]
    public async Task AddAsync_ThrowsConflictException_QuandoLaFasciaNonEDelGiornoScelto()
    {
        var dto = ArrangeAddValido();
        _fasciaRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(Fascia(giorno: DayOfWeek.Tuesday));

        var ex = await Assert.ThrowsAsync<ConflictException>(() => _service.AddAsync(dto));

        Assert.Contains("martedì", ex.Message);
    }

    [Fact]
    public async Task AddAsync_ThrowsConflictException_QuandoLaFasciaNonEAttiva()
    {
        var dto = ArrangeAddValido();
        _fasciaRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(Fascia(attiva: false));

        await Assert.ThrowsAsync<ConflictException>(() => _service.AddAsync(dto));
    }

    [Fact]
    public async Task AddAsync_ThrowsArgumentException_QuandoLaFasciaNonEsiste()
    {
        var dto = ArrangeAddValido();
        _fasciaRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((FasciaOraria)null!);

        await Assert.ThrowsAsync<ArgumentException>(() => _service.AddAsync(dto));
    }

    // ─── Zona preferita e sala ────────────────────────────────────────────────

    [Fact]
    public async Task AddAsync_ThrowsConflictException_QuandoLaZonaPreferitaNonEAttivaONonEsiste()
    {
        var dto = ArrangeAddValido();
        dto.ZonaId = 5; // _zonaRepoMock non conosce questa zona: restituisce null

        await Assert.ThrowsAsync<ConflictException>(() => _service.AddAsync(dto));
    }

    [Fact]
    public async Task AddAsync_ThrowsConflictException_QuandoLaZonaPreferitaNonHaPostazioniAttive()
    {
        var dto = ArrangeAddValido();
        dto.ZonaId = 2; // in _context c'è solo una postazione, e sta in zona 1
        _zonaRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(new Zona { Id = 2, Nome = "Dehors", Attiva = true });

        var ex = await Assert.ThrowsAsync<ConflictException>(() => _service.AddAsync(dto));

        Assert.Contains("postazioni attive", ex.Message);
    }

    [Fact]
    public async Task AddAsync_ThrowsArgumentException_QuandoNonEsisteNessunaPostazioneAttiva()
    {
        // Arrange senza postazioni: non si passa dal solito arranger, che ne aggiunge una.
        _fasciaRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(Fascia());
        _prenotazioniRepoMock.Setup(r => r.GetAllQueryableAsync())
                             .Returns(new List<Prenotazione>().AsQueryable().BuildMock());
        var dto = new PrenotazioneCreateDTO { DataPrenotazione = DataLunediFuturo, NumeroCoperti = 2, FasciaOrariaId = 1 };

        await Assert.ThrowsAsync<ArgumentException>(() => _service.AddAsync(dto));
    }

    // ─── Limite di una prenotazione al giorno (solo self-service Cliente) ─────

    [Fact]
    public async Task AddAsync_ThrowsConflictException_QuandoIlClienteHaGiaUnaPrenotazioneQuelGiorno()
    {
        SetUserAsCliente("cliente-1");
        var dto = ArrangeAddValido(maxCoperti: 50, PrenotazioneEsistente(50, coperti: 2, userId: "cliente-1"));

        var ex = await Assert.ThrowsAsync<ConflictException>(() => _service.AddAsync(dto));

        Assert.Contains("già una prenotazione attiva", ex.Message);
    }

    /// <summary>
    /// Staff e Admin prenotano per conto di clienti diversi sotto il proprio UserId: per loro
    /// il limite giornaliero non deve valere, altrimenti la seconda telefonata della serata
    /// sarebbe rifiutata.
    /// </summary>
    [Fact]
    public async Task AddAsync_NonApplicaIlLimiteGiornalieroAStaff()
    {
        // principal di default = Staff, id "user-test-123"
        var dto = ArrangeAddValido(maxCoperti: 50, PrenotazioneEsistente(50, coperti: 2, userId: "user-test-123"));
        _prenotazioniRepoMock.Setup(r => r.AddAsync(It.IsAny<Prenotazione>())).Returns(Task.CompletedTask);

        await _service.AddAsync(dto);

        _prenotazioniRepoMock.Verify(r => r.AddAsync(It.IsAny<Prenotazione>()), Times.Once);
    }

    [Fact]
    public async Task AddAsync_IlLimiteGiornalieroIgnoraLePrenotazioniAnnullate()
    {
        SetUserAsCliente("cliente-1");
        var dto = ArrangeAddValido(maxCoperti: 50,
                                   PrenotazioneEsistente(50, coperti: 2, stato: StatoPrenotazione.Annullata, userId: "cliente-1"));
        _prenotazioniRepoMock.Setup(r => r.AddAsync(It.IsAny<Prenotazione>())).Returns(Task.CompletedTask);

        await _service.AddAsync(dto);

        _prenotazioniRepoMock.Verify(r => r.AddAsync(It.IsAny<Prenotazione>()), Times.Once);
    }

    [Fact]
    public async Task AddAsync_IlLimiteGiornalieroGuardaSoloLaStessaData()
    {
        SetUserAsCliente("cliente-1");
        var dto = ArrangeAddValido(maxCoperti: 50,
                                   PrenotazioneEsistente(50, coperti: 2, userId: "cliente-1", data: DataLunediFuturo.AddDays(1)));
        _prenotazioniRepoMock.Setup(r => r.AddAsync(It.IsAny<Prenotazione>())).Returns(Task.CompletedTask);

        await _service.AddAsync(dto);

        _prenotazioniRepoMock.Verify(r => r.AddAsync(It.IsAny<Prenotazione>()), Times.Once);
    }

    // ─── Preavviso minimo del Cliente (cutoff 2h) sulla modifica ──────────────

    /// <summary>
    /// Prenotazione che inizia "adesso" prendendo l'ora UTC come orario di fascia: il limite di
    /// cutoff (2h prima) è già passato qualunque sia il fuso di NowInRome.
    /// </summary>
    private (Prenotazione prenotazione, PrenotazioneCreateDTO dto) ArrangeUpdateImminente(string ownerUserId)
    {
        var oggi = DateOnly.FromDateTime(DateTime.UtcNow);
        var fascia = new FasciaOraria
        {
            Id = 1,
            Attiva = true,
            GiornoSettimana = oggi.DayOfWeek,
            MaxCoperti = 50,
            OrarioInizio = TimeOnly.FromDateTime(DateTime.UtcNow),
            OrarioFine = new TimeOnly(23, 59)
        };
        var prenotazione = new Prenotazione
        {
            Id = 1,
            NumeroCoperti = 2,
            Stato = StatoPrenotazione.Attiva,
            UserId = ownerUserId,
            DataPrenotazione = oggi,
            FasciaOrariaId = 1,
            FasciaOraria = fascia,
            PrenotazioniPostazioni = new List<PrenotazionePostazione>()
        };
        var dto = new PrenotazioneCreateDTO { DataPrenotazione = oggi, NumeroCoperti = 2, FasciaOrariaId = 1 };

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
    public async Task UpdateAsync_ThrowsConflictException_QuandoIlClienteEOltreIlCutoff()
    {
        SetUserAsCliente("proprietario");
        var (_, dto) = ArrangeUpdateImminente(ownerUserId: "proprietario");

        var ex = await Assert.ThrowsAsync<ConflictException>(() => _service.UpdateAsync(1, dto));

        Assert.Contains("Contatta il locale", ex.Message);
    }

    /// <summary>Il cutoff è un vincolo del self-service: allo Staff non si applica.</summary>
    [Fact]
    public async Task UpdateAsync_StaffModificaAncheOltreIlCutoff()
    {
        // principal di default = Staff
        var (prenotazione, dto) = ArrangeUpdateImminente(ownerUserId: "un-cliente");
        dto.NumeroCoperti = 6;

        await _service.UpdateAsync(1, dto);

        Assert.Equal(6, prenotazione.NumeroCoperti);
    }

    [Fact]
    public async Task UpdateAsync_ClienteEntroIlCutoff_ModificaLaPropriaPrenotazione()
    {
        SetUserAsCliente("proprietario");
        var (prenotazione, dto) = ArrangeUpdateValido(ownerUserId: "proprietario");
        dto.NumeroCoperti = 4;

        await _service.UpdateAsync(1, dto);

        Assert.Equal(4, prenotazione.NumeroCoperti);
    }

    // ─── Stati da cui non si può modificare ───────────────────────────────────

    [Theory]
    [InlineData(StatoPrenotazione.InCorso)]
    [InlineData(StatoPrenotazione.Annullata)]
    [InlineData(StatoPrenotazione.Completata)]
    public async Task UpdateAsync_ThrowsConflictException_QuandoLoStatoNonEAttiva(StatoPrenotazione stato)
    {
        var (prenotazione, dto) = ArrangeUpdateValido(ownerUserId: "cliente-diverso");
        prenotazione.Stato = stato;

        await Assert.ThrowsAsync<ConflictException>(() => _service.UpdateAsync(1, dto));
    }

    [Fact]
    public async Task UpdateAsync_ThrowsKeyNotFoundException_QuandoLaPrenotazioneNonEsiste()
    {
        _prenotazioniRepoMock.Setup(r => r.GetTrackedByIdAsync(99)).ReturnsAsync((Prenotazione?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.UpdateAsync(99, new PrenotazioneCreateDTO { DataPrenotazione = DataLunediFuturo, NumeroCoperti = 2, FasciaOrariaId = 1 }));
    }

    // ─── Cosa viene effettivamente scritto ────────────────────────────────────

    [Fact]
    public async Task AddAsync_CreaLaPrenotazioneAttivaIntestataAllUtenteAutenticato()
    {
        var dto = ArrangeAddValido();
        dto.NumeroCoperti = 3;
        dto.Note = "Tavolo tranquillo";
        Prenotazione? salvata = null;
        _prenotazioniRepoMock.Setup(r => r.AddAsync(It.IsAny<Prenotazione>()))
                             .Callback<Prenotazione>(p => salvata = p)
                             .Returns(Task.CompletedTask);

        await _service.AddAsync(dto);

        Assert.NotNull(salvata);
        Assert.Equal("user-test-123", salvata!.UserId);
        Assert.Equal(StatoPrenotazione.Attiva, salvata.Stato);
        Assert.Equal(3, salvata.NumeroCoperti);
        Assert.Equal(DataLunediFuturo, salvata.DataPrenotazione);
        Assert.Equal(1, salvata.FasciaOrariaId);
        Assert.Equal("Tavolo tranquillo", salvata.Note);
    }

    /// <summary>
    /// REV-001: NumeroPosti è il dato su cui si appoggiano disponibilità e riepilogo sala.
    /// Restava a 0 prima del checkpoint 2b, e nessun test lo copriva.
    /// </summary>
    [Fact]
    public async Task AddAsync_ScriveINumeroPostiDecisiDallAssegnazione()
    {
        var dto = ArrangeAddValido();
        Prenotazione? salvata = null;
        _prenotazioniRepoMock.Setup(r => r.AddAsync(It.IsAny<Prenotazione>()))
                             .Callback<Prenotazione>(p => salvata = p)
                             .Returns(Task.CompletedTask);

        await _service.AddAsync(dto);

        var riga = Assert.Single(salvata!.PrenotazioniPostazioni);
        Assert.Equal(1, riga.PostazioneId);
        Assert.Equal(2, riga.NumeroPosti);
    }

    [Fact]
    public async Task AddAsync_RegistraLAuditLog()
    {
        var dto = ArrangeAddValido();
        _prenotazioniRepoMock.Setup(r => r.AddAsync(It.IsAny<Prenotazione>())).Returns(Task.CompletedTask);

        await _service.AddAsync(dto);

        _logActivityMock.Verify(l => l.LogAsync("user-test-123",
                                                It.Is<string>(m => m.Contains("Creata prenotazione")),
                                                It.IsAny<string?>()), Times.Once);
    }

    /// <summary>
    /// La modifica riassegna i tavoli: le righe join precedenti vanno cancellate, altrimenti
    /// resterebbero a occupare lo slot del vecchio tavolo (unique index pieno, REV-003).
    /// </summary>
    [Fact]
    public async Task UpdateAsync_CancellaLeRigheJoinPrecedenti_PrimaDiRiassegnare()
    {
        var (prenotazione, dto) = ArrangeUpdateValido(ownerUserId: "cliente-diverso");
        prenotazione.PrenotazioniPostazioni = new List<PrenotazionePostazione>
        {
            new() { PostazioneId = 9, PrenotazioneId = 1, NumeroPosti = 2, DataPrenotazione = DataLunediFuturo, FasciaOrariaId = 1 }
        };
        _context.Prenotazioni.Add(prenotazione);
        _context.SaveChanges();

        await _service.UpdateAsync(1, dto);

        Assert.DoesNotContain(_context.PrenotazioniPostazioni.ToList(), r => r.PostazioneId == 9);
        var riga = Assert.Single(prenotazione.PrenotazioniPostazioni);
        Assert.Equal(1, riga.PostazioneId);
    }

    // ══ REV-053 — chi può fare cosa ═══════════════════════════════════════════

    private void SetUser(string userId, string? ruolo)
    {
        var claims = new List<System.Security.Claims.Claim>
        {
            new(System.Security.Claims.ClaimTypes.NameIdentifier, userId)
        };
        if (ruolo != null)
            claims.Add(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, ruolo));

        var identity = new System.Security.Claims.ClaimsIdentity(claims, "Test");
        _httpContextMock.Setup(h => h.HttpContext)
                        .Returns(new DefaultHttpContext { User = new System.Security.Claims.ClaimsPrincipal(identity) });
    }

    /// <summary>
    /// I vincoli del self-service (ownership, cutoff, una prenotazione al giorno) non valgono
    /// per Admin e Staff. Finora solo Staff era esercitato: Admin passa dallo stesso ramo, ma
    /// era una convinzione, non un fatto verificato.
    /// </summary>
    [Theory]
    [InlineData(GestoraWebApi.Auth.Roles.Admin)]
    [InlineData(GestoraWebApi.Auth.Roles.Staff)]
    public async Task UpdateAsync_AdminEStaffModificanoLaPrenotazioneDiUnCliente(string ruolo)
    {
        SetUser("operatore", ruolo);
        var (prenotazione, dto) = ArrangeUpdateValido(ownerUserId: "un-cliente");
        dto.NumeroCoperti = 4;

        await _service.UpdateAsync(1, dto);

        Assert.Equal(4, prenotazione.NumeroCoperti);
    }

    [Theory]
    [InlineData(GestoraWebApi.Auth.Roles.Admin)]
    [InlineData(GestoraWebApi.Auth.Roles.Staff)]
    public async Task AddAsync_AdminEStaffNonSubisconoIlLimiteGiornaliero(string ruolo)
    {
        SetUser("operatore", ruolo);
        var dto = ArrangeAddValido(maxCoperti: 50, PrenotazioneEsistente(50, coperti: 2, userId: "operatore"));
        _prenotazioniRepoMock.Setup(r => r.AddAsync(It.IsAny<Prenotazione>())).Returns(Task.CompletedTask);

        await _service.AddAsync(dto);

        _prenotazioniRepoMock.Verify(r => r.AddAsync(It.IsAny<Prenotazione>()), Times.Once);
    }

    [Theory]
    [InlineData(GestoraWebApi.Auth.Roles.Admin)]
    [InlineData(GestoraWebApi.Auth.Roles.Staff)]
    public async Task AnnullaPrenotazioneAsync_AdminEStaffAnnullanoAncheOltreIlCutoff(string ruolo)
    {
        SetUser("operatore", ruolo);
        var (prenotazione, _) = ArrangeUpdateImminente(ownerUserId: "un-cliente");

        await _service.AnnullaPrenotazioneAsync(1);

        Assert.Equal(StatoPrenotazione.Annullata, prenotazione.Stato);
    }

    /// <summary>
    /// Un utente con il ruolo Cliente e uno senza alcun ruolo devono ricadere entrambi nel
    /// self-service: il ramo si decide per esclusione (non Admin, non Staff), non per presenza
    /// del ruolo Cliente.
    /// </summary>
    [Theory]
    [InlineData(GestoraWebApi.Auth.Roles.Cliente)]
    [InlineData(null)]
    public async Task UpdateAsync_ClienteESenzaRuolo_NonModificanoPrenotazioniAltrui(string? ruolo)
    {
        SetUser("tizio", ruolo);
        var (_, dto) = ArrangeUpdateValido(ownerUserId: "un-altro");

        await Assert.ThrowsAsync<ForbiddenException>(() => _service.UpdateAsync(1, dto));
    }

    /// <summary>
    /// Senza HttpContext non c'è nessuno a cui intestare la prenotazione: deve uscire un 401
    /// (UnauthorizedAccessException), non un NullReferenceException.
    /// </summary>
    [Fact]
    public async Task AddAsync_ThrowsUnauthorized_QuandoNonCEUtenteAutenticato()
    {
        _httpContextMock.Setup(h => h.HttpContext).Returns((HttpContext?)null);
        var dto = ArrangeAddValido();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.AddAsync(dto));
    }

    // ══ REV-054 — logica dei due processi notturni ════════════════════════════

    /// <summary>
    /// I job Quartz sono gusci sottili: la logica sta in questi due metodi del service. Qui
    /// serve un orologio fermo, altrimenti "oggi" e "fascia scaduta" dipendono dall'ora in cui
    /// girano i test.
    /// </summary>
    private PrenotazioniService ServiceConOrologioFermo(DateTime utcNow) =>
        new(_prenotazioniRepoMock.Object,
            _assignmentServiceMock.Object,
            _fasciaRepoMock.Object,
            _mapperMock.Object,
            _context,
            _httpContextMock.Object,
            _zonaRepoMock.Object,
            _loggerMock.Object,
            _logActivityMock.Object,
            new TestClock(utcNow));

    // 15/06/2026 12:00 UTC == 14:00 a Roma (CEST). "Oggi" = 15/06/2026, ora attuale 14:00.
    private static readonly DateTime IstanteFermo = new(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly OggiFermo = new(2026, 6, 15);

    private static Prenotazione PrenotazioneConFascia(long id, StatoPrenotazione stato, DateOnly data, TimeOnly orarioFine) =>
        new()
        {
            Id = id,
            NumeroCoperti = 2,
            UserId = "u",
            Stato = stato,
            DataPrenotazione = data,
            FasciaOrariaId = 1,
            FasciaOraria = new FasciaOraria { Id = 1, Attiva = true, OrarioInizio = orarioFine.AddHours(-2), OrarioFine = orarioFine },
            PrenotazioniPostazioni = new List<PrenotazionePostazione>()
        };

    private void ArrangeQueryable(params Prenotazione[] prenotazioni)
    {
        _prenotazioniRepoMock.Setup(r => r.GetAllQueryableAsync())
                             .Returns(prenotazioni.ToList().AsQueryable().BuildMock());
        foreach (var p in prenotazioni)
            _prenotazioniRepoMock.Setup(r => r.GetTrackedByIdAsync(p.Id)).ReturnsAsync(p);
    }

    [Fact]
    public async Task AutomaticCompletPrenotazioni_CompletaLeInCorsoDeiGiorniPassati()
    {
        var vecchia = PrenotazioneConFascia(1, StatoPrenotazione.InCorso, OggiFermo.AddDays(-1), new TimeOnly(23, 0));
        ArrangeQueryable(vecchia);

        await ServiceConOrologioFermo(IstanteFermo).AutomaticCompletPrenotazioniAsync();

        Assert.Equal(StatoPrenotazione.Completata, vecchia.Stato);
        _prenotazioniRepoMock.Verify(r => r.UpdateAsync(vecchia), Times.Once);
    }

    [Fact]
    public async Task AutomaticCompletPrenotazioni_CompletaQuellaDiOggiConFasciaGiaFinita()
    {
        // Fascia finita alle 13:00, sono le 14:00 a Roma.
        var finita = PrenotazioneConFascia(1, StatoPrenotazione.InCorso, OggiFermo, new TimeOnly(13, 0));
        ArrangeQueryable(finita);

        await ServiceConOrologioFermo(IstanteFermo).AutomaticCompletPrenotazioniAsync();

        Assert.Equal(StatoPrenotazione.Completata, finita.Stato);
    }

    /// <summary>Il tavolo è ancora occupato: chiuderlo in anticipo lo libererebbe per errore.</summary>
    [Fact]
    public async Task AutomaticCompletPrenotazioni_NonToccaQuellaDiOggiAncoraInCorso()
    {
        // Fascia che finisce alle 23:00, sono le 14:00 a Roma.
        var inCorso = PrenotazioneConFascia(1, StatoPrenotazione.InCorso, OggiFermo, new TimeOnly(23, 0));
        ArrangeQueryable(inCorso);

        await ServiceConOrologioFermo(IstanteFermo).AutomaticCompletPrenotazioniAsync();

        Assert.Equal(StatoPrenotazione.InCorso, inCorso.Stato);
        _prenotazioniRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Prenotazione>()), Times.Never);
    }

    /// <summary>
    /// Solo le "In corso" si completano da sole. Una "Attiva" su data passata è un no-show
    /// (lo staff non ha mai confermato) e resta tale: è il dato su cui la dashboard calcola il KPI.
    /// </summary>
    [Theory]
    [InlineData(StatoPrenotazione.Attiva)]
    [InlineData(StatoPrenotazione.Annullata)]
    [InlineData(StatoPrenotazione.Completata)]
    public async Task AutomaticCompletPrenotazioni_NonToccaGliAltriStati(StatoPrenotazione stato)
    {
        var prenotazione = PrenotazioneConFascia(1, stato, OggiFermo.AddDays(-1), new TimeOnly(23, 0));
        ArrangeQueryable(prenotazione);

        await ServiceConOrologioFermo(IstanteFermo).AutomaticCompletPrenotazioniAsync();

        Assert.Equal(stato, prenotazione.Stato);
        _prenotazioniRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Prenotazione>()), Times.Never);
    }

    [Fact]
    public async Task AutomaticDeletePrenotazioni_EliminaLeCompletatePiuVecchieDiSeiMesi()
    {
        var vecchia = PrenotazioneConFascia(1, StatoPrenotazione.Completata, OggiFermo.AddMonths(-6).AddDays(-1), new TimeOnly(23, 0));
        ArrangeQueryable(vecchia);

        await ServiceConOrologioFermo(IstanteFermo).AutomaticDeletePrenotazioniAsync();

        _prenotazioniRepoMock.Verify(r => r.DeleteAsync(vecchia), Times.Once);
    }

    [Fact]
    public async Task AutomaticDeletePrenotazioni_NonEliminaLeCompletateRecenti()
    {
        var recente = PrenotazioneConFascia(1, StatoPrenotazione.Completata, OggiFermo.AddMonths(-6).AddDays(1), new TimeOnly(23, 0));
        ArrangeQueryable(recente);

        await ServiceConOrologioFermo(IstanteFermo).AutomaticDeletePrenotazioniAsync();

        _prenotazioniRepoMock.Verify(r => r.DeleteAsync(It.IsAny<Prenotazione>()), Times.Never);
    }

    /// <summary>
    /// La pulizia tocca solo lo storico chiuso: un'annullata di due anni fa resta, è traccia
    /// contabile. Cancellare per stato sbagliato sarebbe una perdita di dati silenziosa.
    /// </summary>
    [Theory]
    [InlineData(StatoPrenotazione.Attiva)]
    [InlineData(StatoPrenotazione.InCorso)]
    [InlineData(StatoPrenotazione.Annullata)]
    public async Task AutomaticDeletePrenotazioni_NonEliminaGliAltriStati(StatoPrenotazione stato)
    {
        var vecchia = PrenotazioneConFascia(1, stato, OggiFermo.AddYears(-2), new TimeOnly(23, 0));
        ArrangeQueryable(vecchia);

        await ServiceConOrologioFermo(IstanteFermo).AutomaticDeletePrenotazioniAsync();

        _prenotazioniRepoMock.Verify(r => r.DeleteAsync(It.IsAny<Prenotazione>()), Times.Never);
    }
}
