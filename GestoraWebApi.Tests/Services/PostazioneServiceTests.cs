using AutoMapper;
using GestoraWebApi.Common;
using GestoraWebApi.Enums;
using GestoraWebApi.Infrastructure.Exceptions;
using GestoraWebApi.Models;
using GestoraWebApi.Repositories.FasciaOrarie;
using GestoraWebApi.Repositories.Postazioni;
using GestoraWebApi.Repositories.Zone;
using GestoraWebApi.Services.LogActivity;
using GestoraWebApi.Services.Postazioni;
using GestoraWebApi.Services.Postazioni.DTOs;
using GestoraWebApi.Services.Sala;
using MockQueryable.Moq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using System.Security.Claims;

namespace GestoraWebApi.Tests.Services;

public class PostazioneServiceTests
{
    private readonly Mock<IPostazioneRepository> _postazioneRepoMock;
    private readonly Mock<IZonaRepository> _zonaRepoMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly IMemoryCache _cache;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<ILogActivityService> _logActivityMock;
    private readonly Mock<IFasciaOrariaRepository> _fasciaRepoMock;
    private readonly TestClock _clock;
    private readonly EsecutoreTransazioneFinto _transazione;
    private readonly Mock<ICoerenzaSalaService> _coerenzaSala = new();
    private readonly PostazioneService _service;

    public PostazioneServiceTests()
    {
        _postazioneRepoMock = new Mock<IPostazioneRepository>();
        _zonaRepoMock = new Mock<IZonaRepository>();
        _mapperMock = new Mock<IMapper>();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "test-user-id") }))
        });
        _logActivityMock = new Mock<ILogActivityService>();
        _fasciaRepoMock = new Mock<IFasciaOrariaRepository>();
        _clock = new TestClock(new DateTime(2026, 9, 4, 10, 0, 0, DateTimeKind.Utc));
        _transazione = new EsecutoreTransazioneFinto();
        _service = new PostazioneService(_postazioneRepoMock.Object, _mapperMock.Object, _zonaRepoMock.Object, _cache,
                                          _httpContextAccessorMock.Object, _logActivityMock.Object, _fasciaRepoMock.Object,
                                          _clock, _transazione, _coerenzaSala.Object);
    }

    // CACHE-001: AssociaPostazioneAZonaAsync cambia ZonaId ma non invalidava la cache
    // PostazioniAttive, lasciando la vecchia ZonaId servita dalla cache per 30 minuti.
    [Fact]
    public async Task AssociaPostazioneAZonaAsync_InvalidatesPostazioniAttiveCache()
    {
        // Arrange
        var postazione = new Postazione { Id = 1, Attiva = true, ZonaId = 10 };
        var nuovaZona = new Zona { Id = 20, Nome = "Terrazza", Attiva = true };

        _zonaRepoMock.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(nuovaZona);
        _postazioneRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(postazione);
        _postazioneRepoMock.Setup(r => r.GetImpegniFuturiAsync(1, _clock.TodayInRome)).ReturnsAsync(new List<Prenotazione>());

        _cache.Set(CacheKeys.PostazioniAttive, new List<PostazioneDTO> { new PostazioneDTO() });

        // Act
        await _service.AssociaPostazioneAZonaAsync(1, 20);

        // Assert
        Assert.False(_cache.TryGetValue(CacheKeys.PostazioniAttive, out _));
    }

    // FIX-001: UpdateAsync(PostazioneUpdateDTO) non validava l'esistenza della zona,
    // lasciando che una ZonaId inesistente esplodesse in un DbUpdateException tecnico.
    [Fact]
    public async Task UpdateAsync_ThrowsArgumentException_WhenZonaNonEsiste()
    {
        // Arrange
        var postazione = new Postazione { Id = 1, Numero = 5, Attiva = true, ZonaId = 10 };
        var dto = new PostazioneUpdateDTO { Id = 1, Numero = 5, CapienzaMassima = 4, ZonaId = 999 };

        _postazioneRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(postazione);
        _postazioneRepoMock.Setup(r => r.GetImpegniFuturiAsync(1, _clock.TodayInRome)).ReturnsAsync(new List<Prenotazione>());
        _postazioneRepoMock.Setup(r => r.GetAllQueryable())
                            .Returns(new List<Postazione>().AsQueryable().BuildMockDbSet().Object);
        _zonaRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Zona?)null);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateAsync(dto));
        _postazioneRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Postazione>()), Times.Never);
    }

    // ─── DeleteAsync: solo le prenotazioni vive bloccano l'eliminazione ───────

    [Fact]
    public async Task DeleteAsync_Rifiuta_ENominaIlNumeroDelTavolo_QuandoHaPrenotazioniVive()
    {
        var postazione = new Postazione { Id = 491, Numero = 7, Attiva = true };
        _postazioneRepoMock.Setup(r => r.GetByIdAsync(491)).ReturnsAsync(postazione);
        _postazioneRepoMock.Setup(r => r.HasPrenotazioniViveAsync(491)).ReturnsAsync(true);

        var ex = await Assert.ThrowsAsync<ConflictException>(() => _service.DeleteAsync(491));

        Assert.Contains("tavolo 7", ex.Message);
        Assert.DoesNotContain("491", ex.Message);
        _postazioneRepoMock.Verify(r => r.DeleteAsync(It.IsAny<Postazione>()), Times.Never);
    }

    // Bug 30/09/2026: una riga di legame con una prenotazione Completata/NonPresentata
    // (le Annullate non ne hanno) bloccava per sempre l'eliminazione del tavolo.
    [Fact]
    public async Task DeleteAsync_Elimina_QuandoRestanoSoloPrenotazioniChiuse()
    {
        var postazione = new Postazione
        {
            Id = 491, Numero = 7, Attiva = true,
            PrenotazioniPostazioni = new List<PrenotazionePostazione> { new() { PostazioneId = 491, PrenotazioneId = 1 } }
        };
        _postazioneRepoMock.Setup(r => r.GetByIdAsync(491)).ReturnsAsync(postazione);
        _postazioneRepoMock.Setup(r => r.HasPrenotazioniViveAsync(491)).ReturnsAsync(false);

        await _service.DeleteAsync(491);

        _postazioneRepoMock.Verify(r => r.DeleteAsync(postazione), Times.Once);
    }

    // Bug 30/09/2026: un id inesistente finiva in NullReferenceException (500) invece che 404.
    [Fact]
    public async Task GetPostazioneDTOByIdAsync_RestituisceNull_QuandoLIdNonEsiste()
    {
        _postazioneRepoMock.Setup(r => r.GetAllQueryable())
                           .Returns(new List<Postazione>().AsQueryable().BuildMockDbSet().Object);

        var risultato = await _service.GetPostazioneDTOByIdAsync(999);

        Assert.Null(risultato);
    }

    // ─── GetRiepilogoSalaAsync — decisione 9 (riepilogo sala) ─────────────────

    [Fact]
    public async Task GetRiepilogoSalaAsync_SommaTavoliInZoneAttive_ESegnalaCoperturaTettoPerFascia()
    {
        _zonaRepoMock.Setup(r => r.GetAllZoneAttiveAsync())
                     .ReturnsAsync(new List<Zona> { new() { Id = 1, Nome = "Sala", Attiva = true } });
        _postazioneRepoMock.Setup(r => r.GetPostazioniAttiveAsync())
                           .ReturnsAsync(new List<Postazione>
                           {
                               new() { Id = 1, Numero = 1, CapienzaMassima = 4, Attiva = true, ZonaId = 1 },
                               new() { Id = 2, Numero = 2, CapienzaMassima = 6, Attiva = true, ZonaId = 1 },
                               new() { Id = 3, Numero = 3, CapienzaMassima = 2, Attiva = true, ZonaId = 2 } // zona non attiva
                           });
        _fasciaRepoMock.Setup(r => r.GetFasceAttiveAsync())
                       .ReturnsAsync(new List<FasciaOraria>
                       {
                           new() { Id = 10, GiornoSettimana = DayOfWeek.Monday, OrarioInizio = new TimeOnly(12, 0), OrarioFine = new TimeOnly(15, 0), MaxCoperti = 8, Attiva = true },
                           new() { Id = 11, GiornoSettimana = DayOfWeek.Monday, OrarioInizio = new TimeOnly(19, 0), OrarioFine = new TimeOnly(23, 0), MaxCoperti = 20, Attiva = true }
                       });

        var riepilogo = await _service.GetRiepilogoSalaAsync();

        Assert.Equal(2, riepilogo.TavoliAttivi);   // il tavolo in zona non attiva è escluso
        Assert.Equal(10, riepilogo.PostiTotali);   // 4 + 6
        Assert.True(riepilogo.Fasce.Single(f => f.FasciaOrariaId == 10).TettoCoperto);   // 10 >= 8
        Assert.False(riepilogo.Fasce.Single(f => f.FasciaOrariaId == 11).TettoCoperto);  // 10 < 20
    }
    // ─── V2-009 — un tavolo con prenotazioni future si modifica, se non le danneggia ─────
    //
    // Prima (REV-099) bastava una prenotazione da oggi in poi, anche gia' conclusa, per bloccare
    // qualsiasi modifica: in un locale aperto quasi ogni tavolo ne ha sempre una. Ora contano
    // solo le prenotazioni ancora da servire, e solo se la modifica le danneggia.

    /// <summary>Prenotazione di sabato 05/09/2026, 20:00-22:00 (il clock dei test e' venerdi' 04/09).</summary>
    private static Prenotazione Impegno(int coperti, params (long Id, int Capienza, int Posti)[] tavoli) => new()
    {
        Id = 100,
        NumeroCoperti = coperti,
        DataPrenotazione = new DateOnly(2026, 9, 5),
        Stato = StatoPrenotazione.Attiva,
        FasciaOraria = new FasciaOraria
        {
            GiornoSettimana = DayOfWeek.Saturday,
            OrarioInizio = new TimeOnly(20, 0),
            OrarioFine = new TimeOnly(22, 0)
        },
        PrenotazioniPostazioni = tavoli.Select(t => new PrenotazionePostazione
        {
            PostazioneId = t.Id,
            NumeroPosti = t.Posti,
            Postazione = new Postazione { Id = t.Id, CapienzaMassima = t.Capienza, ZonaId = 10 }
        }).ToList()
    };

    /// <summary>Tavolo 5 (Id 1) in zona 10, con gli impegni indicati. Il DTO di partenza non cambia niente.</summary>
    private PostazioneUpdateDTO PreparaTavoloImpegnato(int capienzaAttuale, params Prenotazione[] impegni)
    {
        _postazioneRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(
            new Postazione { Id = 1, Numero = 5, CapienzaMassima = capienzaAttuale, Attiva = true, ZonaId = 10 });
        _postazioneRepoMock.Setup(r => r.GetImpegniFuturiAsync(1, _clock.TodayInRome)).ReturnsAsync(impegni.ToList());
        _postazioneRepoMock.Setup(r => r.GetAllQueryable())
                           .Returns(new List<Postazione>().AsQueryable().BuildMockDbSet().Object);
        _zonaRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new Zona { Id = 10, Nome = "Sala", Attiva = true });
        _zonaRepoMock.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(new Zona { Id = 20, Nome = "Terrazza", Attiva = true });

        return new PostazioneUpdateDTO { Id = 1, Numero = 5, CapienzaMassima = capienzaAttuale, ZonaId = 10, Attiva = true };
    }

    private void VerificaSalvato() =>
        _postazioneRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Postazione>()), Times.Once);

    private async Task<ConflictException> VerificaRifiutato(Func<Task> azione)
    {
        var ex = await Assert.ThrowsAsync<ConflictException>(azione);
        _postazioneRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Postazione>()), Times.Never);
        return ex;
    }

    [Fact]
    public async Task UpdateAsync_CambiaIlNumero_ConPrenotazioniFuture_Salva()
    {
        var dto = PreparaTavoloImpegnato(4, Impegno(4, (1, 4, 4)));
        dto.Numero = 7;

        await _service.UpdateAsync(dto);

        VerificaSalvato();
        // Una modifica innocua non deve nemmeno andare a leggere le prenotazioni.
        _postazioneRepoMock.Verify(r => r.GetImpegniFuturiAsync(It.IsAny<long>(), It.IsAny<DateOnly>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_AumentaIPosti_ConPrenotazioniFuture_Salva()
    {
        var dto = PreparaTavoloImpegnato(4, Impegno(4, (1, 4, 4)));
        dto.CapienzaMassima = 6;

        await _service.UpdateAsync(dto);

        VerificaSalvato();
    }

    [Fact]
    public async Task UpdateAsync_RiduceIPosti_LaPrenotazioneCiStaAncora_Salva()
    {
        var dto = PreparaTavoloImpegnato(6, Impegno(4, (1, 6, 4)));
        dto.CapienzaMassima = 4;

        await _service.UpdateAsync(dto);

        VerificaSalvato();
    }

    [Fact]
    public async Task UpdateAsync_RiduceIPosti_LaPrenotazioneNonCiStaPiu_RifiutaENominaLaPrenotazione()
    {
        var dto = PreparaTavoloImpegnato(6, Impegno(4, (1, 6, 4)));
        dto.CapienzaMassima = 3;

        var ex = await VerificaRifiutato(() => _service.UpdateAsync(dto));

        Assert.Contains("tavolo 5 a 3", ex.Message);
        Assert.Contains("sabato 05/09/2026, 20:00-22:00 (4 persone)", ex.Message);
    }

    // Due tavoli da 2 uniti fanno 6 posti grazie alle testate: un tavolo che scende a 1 rompe
    // l'unione, anche se a guardarlo da solo sembra una riduzione da poco.
    [Fact]
    public async Task UpdateAsync_RiduceIPosti_UnioneDiTavoliDa2CheNonTienePiu_Rifiuta()
    {
        var dto = PreparaTavoloImpegnato(2, Impegno(6, (1, 2, 3), (2, 2, 3)));
        dto.CapienzaMassima = 1;

        await VerificaRifiutato(() => _service.UpdateAsync(dto));
    }

    [Fact]
    public async Task UpdateAsync_RiduceIPosti_ConPiuPrenotazioni_ContaLeAltreNelMessaggio()
    {
        var dto = PreparaTavoloImpegnato(6, Impegno(5, (1, 6, 5)), Impegno(6, (1, 6, 6)), Impegno(2, (1, 6, 2)));
        dto.CapienzaMassima = 4;

        var ex = await VerificaRifiutato(() => _service.UpdateAsync(dto));

        // Bloccano solo le due da 5 e 6 persone: quella da 2 ci sta.
        Assert.Contains("(5 persone) e ad altre 1", ex.Message);
    }

    [Fact]
    public async Task UpdateAsync_Disattiva_ConPrenotazioniFuture_Rifiuta()
    {
        var dto = PreparaTavoloImpegnato(4, Impegno(4, (1, 4, 4)));
        dto.Attiva = false;

        var ex = await VerificaRifiutato(() => _service.UpdateAsync(dto));

        Assert.Contains("disattivare il tavolo 5", ex.Message);
    }

    [Fact]
    public async Task UpdateAsync_CambiaZona_ConPrenotazioniFuture_Rifiuta()
    {
        var dto = PreparaTavoloImpegnato(4, Impegno(4, (1, 4, 4)));
        dto.ZonaId = 20;

        var ex = await VerificaRifiutato(() => _service.UpdateAsync(dto));

        Assert.Contains("spostare di zona il tavolo 5", ex.Message);
    }

    [Fact]
    public async Task AssociaPostazioneAZonaAsync_ConPrenotazioniFuture_Rifiuta()
    {
        PreparaTavoloImpegnato(4, Impegno(4, (1, 4, 4)));

        await VerificaRifiutato(() => _service.AssociaPostazioneAZonaAsync(1, 20));
    }

    // Senza prenotazioni ancora da servire (solo storico, o nessuna) si puo' fare di tutto.
    [Fact]
    public async Task UpdateAsync_SenzaPrenotazioniDaServire_DisattivaRiduceESposta()
    {
        var dto = PreparaTavoloImpegnato(6);
        dto.Attiva = false;
        dto.CapienzaMassima = 2;
        dto.ZonaId = 20;

        await _service.UpdateAsync(dto);

        VerificaSalvato();
    }

    // La data passata al repository deve essere "oggi" secondo l'orologio del locale, non quello
    // della macchina: e' lo stesso motivo per cui esiste IClock (REV-016).
    [Fact]
    public async Task UpdateAsync_ChiedeLePrenotazioniFuture_APartireDaOggiInItalia()
    {
        var dto = PreparaTavoloImpegnato(6);
        dto.CapienzaMassima = 4;

        await _service.UpdateAsync(dto);

        _postazioneRepoMock.Verify(r => r.GetImpegniFuturiAsync(1, _clock.TodayInRome), Times.Once);
    }
    // ─── REV-023 — lo storico non si carica nel percorso caldo ───────────────

    // AUD-M8: l'elenco esponeva gli Id delle prenotazioni di ogni tavolo (anche al Cliente),
    // tenuti in cache senza invalidazione. Ora usa la query leggera e non li espone piu'.
    [Fact]
    public async Task GetPostazioniAttiveAsync_NonEsponeLePrenotazioni()
    {
        _postazioneRepoMock.Setup(r => r.GetPostazioniAttiveAsync())
                            .ReturnsAsync(new List<Postazione>
                            {
                                new()
                                {
                                    Id = 1, Numero = 5, CapienzaMassima = 4, Attiva = true, ZonaId = 10,
                                    PrenotazioniPostazioni = new List<PrenotazionePostazione>
                                    {
                                        new() { PostazioneId = 1, PrenotazioneId = 77 }
                                    }
                                }
                            });

        var result = await _service.GetPostazioniAttiveAsync();

        Assert.Single(result);
        Assert.Empty(result[0].PrenotazioneId);
    }

    [Fact]
    public async Task GetRiepilogoSalaAsync_UsaLaQueryLeggera_SenzaCaricareLoStorico()
    {
        // Il riepilogo somma tavoli e posti: dello storico non sa che farsene, e caricarlo
        // significherebbe tirare su tutte le righe join di tutti i tavoli a ogni apertura.
        _zonaRepoMock.Setup(r => r.GetAllZoneAttiveAsync())
                     .ReturnsAsync(new List<Zona> { new() { Id = 1, Nome = "Sala", Attiva = true } });
        _postazioneRepoMock.Setup(r => r.GetPostazioniAttiveAsync())
                            .ReturnsAsync(new List<Postazione>
                            {
                                new() { Id = 1, Numero = 1, CapienzaMassima = 4, Attiva = true, ZonaId = 1 }
                            });
        _fasciaRepoMock.Setup(r => r.GetFasceAttiveAsync()).ReturnsAsync(new List<FasciaOraria>());

        var riepilogo = await _service.GetRiepilogoSalaAsync();

        Assert.Equal(1, riepilogo.TavoliAttivi);
        Assert.Equal(4, riepilogo.PostiTotali);
    }

    // ─── V2-007 — elenco della pagina Tavoli ──────────────────────────────────

    [Fact]
    public async Task GetTavoliZonaPerGestione_MostraAncheIDisattivati_AncheSuZonaSpenta()
    {
        // Un tavolo disattivato deve restare in elenco, altrimenti non si può più riattivare.
        _zonaRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new Zona { Id = 10, Nome = "Dehors", Attiva = false });
        _postazioneRepoMock.Setup(r => r.GetTuttePostazioniPerZonaAsync(10))
                            .ReturnsAsync(new List<Postazione>
                            {
                                new() { Id = 1, Numero = 1, CapienzaMassima = 4, Attiva = true, ZonaId = 10 },
                                new() { Id = 2, Numero = 2, CapienzaMassima = 2, Attiva = false, ZonaId = 10 }
                            });

        var tavoli = await _service.GetTavoliZonaPerGestioneAsync(10);

        Assert.Equal(new[] { true, false }, tavoli.Select(t => t.Attiva));
        _postazioneRepoMock.Verify(r => r.GetPostazioniPerZonaAsync(It.IsAny<long>()), Times.Never);
    }

    [Fact]
    public async Task GetTavoliZonaPerGestione_ZonaInesistente_NotFound()
    {
        _zonaRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Zona?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetTavoliZonaPerGestioneAsync(999));
    }

    // ─── V2-007 — modifiche ai tavoli e tetto delle fasce ─────────────────────

    private PostazioneUpdateDTO PreparaUpdate(int nuovaCapienza, bool attiva)
    {
        _postazioneRepoMock.Setup(r => r.GetByIdAsync(1))
                           .ReturnsAsync(new Postazione { Id = 1, Numero = 5, CapienzaMassima = 6, Attiva = true, ZonaId = 10 });
        _postazioneRepoMock.Setup(r => r.GetImpegniFuturiAsync(1, _clock.TodayInRome)).ReturnsAsync(new List<Prenotazione>());
        _postazioneRepoMock.Setup(r => r.GetAllQueryable())
                           .Returns(new List<Postazione>().AsQueryable().BuildMockDbSet().Object);
        _zonaRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new Zona { Id = 10, Nome = "Sala", Attiva = true });
        return new PostazioneUpdateDTO { Id = 1, Numero = 5, CapienzaMassima = nuovaCapienza, ZonaId = 10, Attiva = attiva };
    }

    private void SalaSottoIlTetto() =>
        _coerenzaSala.Setup(c => c.VerificaModificaSalaAsync(It.IsAny<Func<IReadOnlyList<TavoloSala>, IEnumerable<TavoloSala>>>(),
                                                              It.IsAny<Func<IReadOnlySet<long>, IEnumerable<long>>?>()))
                     .ThrowsAsync(new ConflictException("sotto il tetto"));

    /// <summary>Esegue la modifica e restituisce la sala "dopo" che il service ha passato al controllo.</summary>
    private async Task<List<TavoloSala>> SalaDopo(Func<Task> azione)
    {
        Func<IReadOnlyList<TavoloSala>, IEnumerable<TavoloSala>>? tavoliDopo = null;
        _coerenzaSala.Setup(c => c.VerificaModificaSalaAsync(It.IsAny<Func<IReadOnlyList<TavoloSala>, IEnumerable<TavoloSala>>>(),
                                                              It.IsAny<Func<IReadOnlySet<long>, IEnumerable<long>>?>()))
                     .Callback<Func<IReadOnlyList<TavoloSala>, IEnumerable<TavoloSala>>, Func<IReadOnlySet<long>, IEnumerable<long>>?>((t, _) => tavoliDopo = t)
                     .Returns(Task.CompletedTask);

        await azione();

        Assert.NotNull(tavoliDopo);
        var salaOggi = new List<TavoloSala> { new(1, 10, 6), new(2, 10, 4) };
        return tavoliDopo!(salaOggi).OrderBy(t => t.Id).ToList();
    }

    [Fact]
    public async Task UpdateAsync_CapienzaRidotta_ControllaLaSalaConLaNuovaCapienza()
    {
        var dto = PreparaUpdate(nuovaCapienza: 2, attiva: true);

        var dopo = await SalaDopo(() => _service.UpdateAsync(dto));

        Assert.Equal(new[] { new TavoloSala(1, 10, 2), new TavoloSala(2, 10, 4) }, dopo);
    }

    [Fact]
    public async Task UpdateAsync_TavoloDisattivato_EsceDallaSalaDopo()
    {
        var dto = PreparaUpdate(nuovaCapienza: 6, attiva: false);

        var dopo = await SalaDopo(() => _service.UpdateAsync(dto));

        Assert.Equal(new[] { new TavoloSala(2, 10, 4) }, dopo);
    }

    [Fact]
    public async Task UpdateAsync_SalaSottoIlTettoDiUnaFascia_NonSalva()
    {
        var dto = PreparaUpdate(nuovaCapienza: 2, attiva: true);
        SalaSottoIlTetto();

        await Assert.ThrowsAsync<ConflictException>(() => _service.UpdateAsync(dto));
        _postazioneRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Postazione>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_TogliIlTavoloDallaSalaDopo()
    {
        _postazioneRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Postazione { Id = 1, Numero = 5, Attiva = true });
        _postazioneRepoMock.Setup(r => r.HasPrenotazioniViveAsync(1)).ReturnsAsync(false);

        var dopo = await SalaDopo(() => _service.DeleteAsync(1));

        Assert.Equal(new[] { new TavoloSala(2, 10, 4) }, dopo);
    }

    [Fact]
    public async Task DeleteAsync_SalaSottoIlTettoDiUnaFascia_NonElimina()
    {
        _postazioneRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Postazione { Id = 1, Numero = 5, Attiva = true });
        _postazioneRepoMock.Setup(r => r.HasPrenotazioniViveAsync(1)).ReturnsAsync(false);
        SalaSottoIlTetto();

        await Assert.ThrowsAsync<ConflictException>(() => _service.DeleteAsync(1));
        _postazioneRepoMock.Verify(r => r.DeleteAsync(It.IsAny<Postazione>()), Times.Never);
    }
}
