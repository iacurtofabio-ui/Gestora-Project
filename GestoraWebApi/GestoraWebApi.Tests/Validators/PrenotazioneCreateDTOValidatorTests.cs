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
}
