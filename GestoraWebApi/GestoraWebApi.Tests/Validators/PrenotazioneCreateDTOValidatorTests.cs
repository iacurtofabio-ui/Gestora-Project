using GestoraWebApi.Services.Prenotazioni.DTOs;
using GestoraWebApi.Validators;

namespace GestoraWebApi.Tests.Validators;

/// <summary>
/// REV-016 / REV-092: la soglia "data passata" segue l'ora italiana, non quella del server.
/// </summary>
public class PrenotazioneCreateDTOValidatorTests
{
    // 14/06/2026 23:30 UTC == 15/06/2026 a Roma (CEST).
    private static readonly DateTime UtcSera = new(2026, 6, 14, 23, 30, 0, DateTimeKind.Utc);

    [Fact]
    public void RifiutaLaDataDiIeriInOraItaliana()
    {
        var validator = new PrenotazioneCreateDTOValidator(new TestClock(UtcSera));

        var result = validator.Validate(new PrenotazioneCreateDTO
        {
            NumeroCoperti = 2,
            FasciaOrariaId = 1,
            DataPrenotazione = new DateOnly(2026, 6, 14)
        });

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(PrenotazioneCreateDTO.DataPrenotazione));
    }

    [Fact]
    public void AccettaLaDataDiOggiInOraItaliana_AncheSeIlServerUtcEAncoraIeri()
    {
        var validator = new PrenotazioneCreateDTOValidator(new TestClock(UtcSera));

        var result = validator.Validate(new PrenotazioneCreateDTO
        {
            NumeroCoperti = 2,
            FasciaOrariaId = 1,
            DataPrenotazione = new DateOnly(2026, 6, 15)
        });

        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(PrenotazioneCreateDTO.DataPrenotazione));
    }

    // ── REV-051 — i limiti sui dati in ingresso ──────────────────────────────

    private static PrenotazioneCreateDTO Valido() => new()
    {
        NumeroCoperti = 2,
        FasciaOrariaId = 1,
        DataPrenotazione = new DateOnly(2026, 6, 20)
    };

    private static FluentValidation.Results.ValidationResult Valida(PrenotazioneCreateDTO dto) =>
        new PrenotazioneCreateDTOValidator(new TestClock(UtcSera)).Validate(dto);

    [Fact]
    public void AccettaUnaRichiestaValida()
    {
        Assert.True(Valida(Valido()).IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(51)]
    public void RifiutaUnNumeroDiCopertiFuoriDaiLimiti(int coperti)
    {
        var dto = Valido();
        dto.NumeroCoperti = coperti;

        Assert.Contains(Valida(dto).Errors, e => e.PropertyName == nameof(PrenotazioneCreateDTO.NumeroCoperti));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    public void AccettaGliEstremiAmmessiDeiCoperti(int coperti)
    {
        var dto = Valido();
        dto.NumeroCoperti = coperti;

        Assert.DoesNotContain(Valida(dto).Errors, e => e.PropertyName == nameof(PrenotazioneCreateDTO.NumeroCoperti));
    }

    [Fact]
    public void RifiutaUnaFasciaOrariaNonSpecificata()
    {
        var dto = Valido();
        dto.FasciaOrariaId = 0;

        Assert.Contains(Valida(dto).Errors, e => e.PropertyName == nameof(PrenotazioneCreateDTO.FasciaOrariaId));
    }

    [Fact]
    public void RifiutaNoteOltreI500Caratteri()
    {
        var dto = Valido();
        dto.Note = new string('x', 501);

        Assert.Contains(Valida(dto).Errors, e => e.PropertyName == nameof(PrenotazioneCreateDTO.Note));
    }

    /// <summary>Le note sono facoltative: la regola sulla lunghezza non deve scattare su null.</summary>
    [Fact]
    public void AccettaNoteAssenti()
    {
        var dto = Valido();
        dto.Note = null;

        Assert.DoesNotContain(Valida(dto).Errors, e => e.PropertyName == nameof(PrenotazioneCreateDTO.Note));
    }
}
