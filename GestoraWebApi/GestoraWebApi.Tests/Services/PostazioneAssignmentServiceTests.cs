using GestoraWebApi.Models;
using GestoraWebApi.Repositories.Postazioni;
using GestoraWebApi.Repositories.Prenotazioni;
using GestoraWebApi.Services.PostazioneAssignment;
using Moq;

namespace GestoraWebApi.Tests.Services;

public class PostazioneAssignmentServiceTests
{
    private readonly PostazioneAssignmentService _service;

    public PostazioneAssignmentServiceTests()
    {
        // TrovaCombinazioniDisponibili è una funzione pura: non usa repository.
        // I mock servono solo per soddisfare il costruttore.
        _service = new PostazioneAssignmentService(
            new Mock<IPostazioneRepository>().Object,
            new Mock<IPrenotazioniRepository>().Object);
    }

    [Fact]
    public void TrovaCombinazioniDisponibili_ReturnsSingolaPostazione_WhenCapienzaSufficiente()
    {
        // Arrange: una postazione da 4 posti, richiesta per 3 coperti
        var postazioni = new List<Postazione>
        {
            new Postazione { Id = 1, CapienzaMassima = 4, ZonaId = 1 }
        };

        // Act
        var result = _service.TrovaCombinazioniDisponibili(postazioni, numeroCoperti: 3);

        // Assert: trovata una combinazione con una sola postazione
        Assert.Single(result);
        Assert.Single(result[0]);
        Assert.Equal(1, result[0][0].Id);
    }

    [Fact]
    public void TrovaCombinazioniDisponibili_ReturnsCombinazione_WhenServonoPiuPostazioni()
    {
        // Arrange: due postazioni da 2 posti nella stessa zona, richiesta per 4 coperti
        var postazioni = new List<Postazione>
        {
            new Postazione { Id = 1, CapienzaMassima = 2, ZonaId = 1 },
            new Postazione { Id = 2, CapienzaMassima = 2, ZonaId = 1 }
        };

        // Act
        var result = _service.TrovaCombinazioniDisponibili(postazioni, numeroCoperti: 4);

        // Assert: trovata una combinazione con due postazioni
        Assert.Single(result);
        Assert.Equal(2, result[0].Count);
    }

    [Fact]
    public void TrovaCombinazioniDisponibili_ReturnsEmpty_WhenCapienzaTotaleInsufficient()
    {
        // Arrange: una sola postazione da 2 posti, richiesta per 5 coperti
        var postazioni = new List<Postazione>
        {
            new Postazione { Id = 1, CapienzaMassima = 2, ZonaId = 1 }
        };

        // Act
        var result = _service.TrovaCombinazioniDisponibili(postazioni, numeroCoperti: 5);

        // Assert: nessuna combinazione possibile
        Assert.Empty(result);
    }

    [Fact]
    public void TrovaCombinazioniDisponibili_PreferisceSingolaPostazione_AncheSeEsisteCombinazione()
    {
        // Arrange: una postazione singola da 4 e due da 2 nella stessa zona
        var postazioni = new List<Postazione>
        {
            new Postazione { Id = 1, CapienzaMassima = 4, ZonaId = 1 },
            new Postazione { Id = 2, CapienzaMassima = 2, ZonaId = 1 },
            new Postazione { Id = 3, CapienzaMassima = 2, ZonaId = 1 }
        };

        // Act
        var result = _service.TrovaCombinazioniDisponibili(postazioni, numeroCoperti: 3);

        // Assert: viene preferita la singola postazione (Id=1), non la combinazione
        Assert.Single(result);
        Assert.Single(result[0]);
        Assert.Equal(1, result[0][0].Id);
    }
}
