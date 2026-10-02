using GestoraWebApi.Enums;
using GestoraWebApi.Infrastructure.Exceptions;
using GestoraWebApi.Models;
using GestoraWebApi.Repositories.Postazioni;
using GestoraWebApi.Repositories.Prenotazioni;
using GestoraWebApi.Repositories.Zone;
using GestoraWebApi.Services.PostazioneAssignment;
using GestoraWebApi.Services.Prenotazioni.DTOs;
using MockQueryable;
using Moq;

namespace GestoraWebApi.Tests.Services;

/// <summary>
/// REV-052: test del metodo che decide le assegnazioni reali,
/// <see cref="PostazioneAssignmentService.AssegnaPostazioneDisponibileAsync"/>. Il motore puro
/// che sceglie la combinazione è coperto a parte in <see cref="AssegnazioneTavoliTests"/>:
/// qui si verifica ciò che il motore non vede, cioè quali tavoli gli arrivano davvero in mano
/// (zone attive, filtro sulla zona preferita, slot già occupati) e come vengono tradotti gli
/// esiti a vuoto.
/// </summary>
public class PostazioneAssignmentServiceTests
{
    private const long ZonaSala = 1;
    private const long ZonaDehors = 2;
    private const long FasciaCena = 10;
    private const long FasciaPranzo = 11;

    private static readonly DateOnly Data = new(2026, 9, 7);

    private readonly Mock<IPostazioneRepository> _postazioneRepoMock = new();
    private readonly Mock<IPrenotazioniRepository> _prenotazioniRepoMock = new();
    private readonly Mock<IZonaRepository> _zonaRepoMock = new();
    private readonly PostazioneAssignmentService _service;

    public PostazioneAssignmentServiceTests()
    {
        _service = new PostazioneAssignmentService(
            _postazioneRepoMock.Object,
            _prenotazioniRepoMock.Object,
            _zonaRepoMock.Object);

        // Default: nessuna prenotazione esistente, tutte le zone attive.
        ArrangePrenotazioni();
        ArrangeZoneAttive(ZonaSala, ZonaDehors);
    }

    private static Postazione Tavolo(long id, int capienza, long zonaId = ZonaSala) =>
        new() { Id = id, Numero = (int)id, CapienzaMassima = capienza, Attiva = true, ZonaId = zonaId };

    private void ArrangeTavoliAttivi(params Postazione[] tavoli) =>
        _postazioneRepoMock.Setup(r => r.GetPostazioniAttiveAsync()).ReturnsAsync(tavoli.ToList());

    private void ArrangeZoneAttive(params long[] zoneIds) =>
        _zonaRepoMock.Setup(r => r.GetAllZoneAttiveAsync())
                     .ReturnsAsync(zoneIds.Select(id => new Zona { Id = id, Nome = $"Zona {id}", Attiva = true }).ToList());

    private void ArrangePrenotazioni(params Prenotazione[] prenotazioni) =>
        _prenotazioniRepoMock.Setup(r => r.GetAllQueryableAsync())
                             .Returns(prenotazioni.ToList().AsQueryable().BuildMock());

    /// <summary>Prenotazione che occupa i tavoli indicati nello slot data+fascia.</summary>
    private static Prenotazione PrenotazioneCheOccupa(long id, StatoPrenotazione stato, DateOnly data, long fasciaId, params long[] postazioniIds) =>
        new()
        {
            Id = id,
            NumeroCoperti = 2,
            UserId = "u",
            Stato = stato,
            DataPrenotazione = data,
            FasciaOrariaId = fasciaId,
            PrenotazioniPostazioni = postazioniIds
                .Select(pid => new PrenotazionePostazione
                {
                    PostazioneId = pid,
                    PrenotazioneId = id,
                    DataPrenotazione = data,
                    FasciaOrariaId = fasciaId
                })
                .ToList()
        };

    private static PrenotazioneCreateDTO Richiesta(int coperti, long? zonaId = null, long fasciaId = FasciaCena) =>
        new() { DataPrenotazione = Data, NumeroCoperti = coperti, FasciaOrariaId = fasciaId, ZonaId = zonaId };

    // ─── Quali tavoli entrano nella scelta ────────────────────────────────────

    /// <summary>
    /// REV-024: un tavolo attivo dentro una zona disattivata non esiste, per l'assegnazione.
    /// Il tavolo grande è l'unico che coprirebbe la richiesta, ma sta in una zona spenta.
    /// </summary>
    [Fact]
    public async Task NonAssegnaTavoliDiZoneDisattivate()
    {
        ArrangeTavoliAttivi(Tavolo(1, 8, ZonaDehors), Tavolo(2, 2, ZonaSala));
        ArrangeZoneAttive(ZonaSala);

        var ex = await Assert.ThrowsAsync<ConflictException>(() => _service.AssegnaPostazioneDisponibileAsync(Richiesta(8)));

        Assert.Contains("coperti richiesti", ex.Message);
    }

    [Fact]
    public async Task ConZonaPreferita_AssegnaSoloTavoliDiQuellaZona()
    {
        // Il tavolo che spreca meno per 4 coperti sarebbe il 2 (sala), ma la zona chiesta è il dehors.
        ArrangeTavoliAttivi(Tavolo(1, 8, ZonaDehors), Tavolo(2, 4, ZonaSala));

        var assegnati = await _service.AssegnaPostazioneDisponibileAsync(Richiesta(4, zonaId: ZonaDehors));

        var assegnato = Assert.Single(assegnati);
        Assert.Equal(1, assegnato.Postazione.Id);
    }

    /// <summary>
    /// Il messaggio deve dire che il problema è la zona scelta, non che il locale è pieno:
    /// è l'unico modo per far capire al cliente che basta togliere la preferenza.
    /// </summary>
    [Fact]
    public async Task ConZonaPreferitaSenzaTavoli_IlMessaggioNominaLaZona()
    {
        ArrangeTavoliAttivi(Tavolo(1, 4, ZonaSala));

        var ex = await Assert.ThrowsAsync<ConflictException>(
            () => _service.AssegnaPostazioneDisponibileAsync(Richiesta(2, zonaId: ZonaDehors)));

        Assert.Contains("zona preferita", ex.Message);
    }

    [Fact]
    public async Task SenzaNessunTavoloAttivo_ThrowsConflictException()
    {
        ArrangeTavoliAttivi();

        var ex = await Assert.ThrowsAsync<ConflictException>(() => _service.AssegnaPostazioneDisponibileAsync(Richiesta(2)));

        Assert.DoesNotContain("zona preferita", ex.Message);
    }

    // ─── Slot già occupati ────────────────────────────────────────────────────

    [Fact]
    public async Task NonAssegnaUnTavoloGiaOccupatoNelloStessoSlot()
    {
        ArrangeTavoliAttivi(Tavolo(1, 4), Tavolo(2, 4));
        ArrangePrenotazioni(PrenotazioneCheOccupa(100, StatoPrenotazione.Attiva, Data, FasciaCena, 1));

        var assegnati = await _service.AssegnaPostazioneDisponibileAsync(Richiesta(4));

        var assegnato = Assert.Single(assegnati);
        Assert.Equal(2, assegnato.Postazione.Id);
    }

    [Fact]
    public async Task TuttiITavoliOccupatiNellaFascia_ThrowsConflictException()
    {
        ArrangeTavoliAttivi(Tavolo(1, 4));
        ArrangePrenotazioni(PrenotazioneCheOccupa(100, StatoPrenotazione.Attiva, Data, FasciaCena, 1));

        var ex = await Assert.ThrowsAsync<ConflictException>(() => _service.AssegnaPostazioneDisponibileAsync(Richiesta(4)));

        Assert.Contains("fascia oraria selezionata", ex.Message);
    }

    /// <summary>
    /// REV-003: una prenotazione annullata libera il tavolo. Se restasse conteggiata fra le
    /// occupate, un annullo renderebbe il tavolo inutilizzabile per sempre in quello slot.
    /// </summary>
    [Fact]
    public async Task UnaPrenotazioneAnnullataNonOccupaIlTavolo()
    {
        ArrangeTavoliAttivi(Tavolo(1, 4));
        ArrangePrenotazioni(PrenotazioneCheOccupa(100, StatoPrenotazione.Annullata, Data, FasciaCena, 1));

        var assegnati = await _service.AssegnaPostazioneDisponibileAsync(Richiesta(4));

        Assert.Equal(1, Assert.Single(assegnati).Postazione.Id);
    }

    [Fact]
    public async Task IlTavoloOccupatoInUnAltraFascia_RestaLibero()
    {
        ArrangeTavoliAttivi(Tavolo(1, 4));
        ArrangePrenotazioni(PrenotazioneCheOccupa(100, StatoPrenotazione.Attiva, Data, FasciaPranzo, 1));

        var assegnati = await _service.AssegnaPostazioneDisponibileAsync(Richiesta(4));

        Assert.Equal(1, Assert.Single(assegnati).Postazione.Id);
    }

    [Fact]
    public async Task IlTavoloOccupatoInUnAltraData_RestaLibero()
    {
        ArrangeTavoliAttivi(Tavolo(1, 4));
        ArrangePrenotazioni(PrenotazioneCheOccupa(100, StatoPrenotazione.Attiva, Data.AddDays(1), FasciaCena, 1));

        var assegnati = await _service.AssegnaPostazioneDisponibileAsync(Richiesta(4));

        Assert.Equal(1, Assert.Single(assegnati).Postazione.Id);
    }

    /// <summary>
    /// In modifica il tavolo già assegnato alla prenotazione che si sta modificando non deve
    /// risultare occupato da sé stessa, altrimenti cambiare le sole note farebbe fallire il
    /// salvataggio quando quello è l'unico tavolo adatto.
    /// </summary>
    [Fact]
    public async Task InModifica_IlTavoloDellaPrenotazioneStessaNonRisultaOccupato()
    {
        ArrangeTavoliAttivi(Tavolo(1, 4));
        ArrangePrenotazioni(PrenotazioneCheOccupa(100, StatoPrenotazione.Attiva, Data, FasciaCena, 1));

        var assegnati = await _service.AssegnaPostazioneDisponibileAsync(Richiesta(4), excludePrenotazioneId: 100);

        Assert.Equal(1, Assert.Single(assegnati).Postazione.Id);
    }

    // ─── Esito dell'assegnazione ──────────────────────────────────────────────

    /// <summary>
    /// REV-001: i coperti richiesti vanno distribuiti sui tavoli dell'unione — è il dato su cui
    /// si appoggiano disponibilità e riepilogo sala. La somma deve tornare esatta.
    /// </summary>
    [Fact]
    public async Task DistribuisceICopertiSuiTavoliDellUnione()
    {
        // Due tavoli da 2 valgono 6 coperti (bonus testate): l'unione è l'unica che copre 6.
        ArrangeTavoliAttivi(Tavolo(1, 2), Tavolo(2, 2));

        var assegnati = await _service.AssegnaPostazioneDisponibileAsync(Richiesta(6));

        Assert.Equal(2, assegnati.Count);
        Assert.Equal(6, assegnati.Sum(a => a.PostiOccupati));
        Assert.All(assegnati, a => Assert.True(a.PostiOccupati > 0));
    }

    /// <summary>
    /// Il percorso reale deve usare lo stesso criterio del motore: fra due tavoli capienti
    /// vince quello che spreca meno posti, non il primo che passa.
    /// </summary>
    [Fact]
    public async Task AssegnaIlTavoloCheSprecaMenoPosti()
    {
        ArrangeTavoliAttivi(Tavolo(1, 8), Tavolo(2, 4));

        var assegnati = await _service.AssegnaPostazioneDisponibileAsync(Richiesta(3));

        var assegnato = Assert.Single(assegnati);
        Assert.Equal(2, assegnato.Postazione.Id);
        Assert.Equal(3, assegnato.PostiOccupati);
    }

    [Fact]
    public async Task NessunaCombinazioneCopreIRichiesti_ThrowsConflictException()
    {
        ArrangeTavoliAttivi(Tavolo(1, 2));

        var ex = await Assert.ThrowsAsync<ConflictException>(() => _service.AssegnaPostazioneDisponibileAsync(Richiesta(10)));

        Assert.Contains("coperti richiesti", ex.Message);
    }
}
