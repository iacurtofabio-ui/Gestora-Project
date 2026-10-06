using FluentValidation.TestHelper;
using GestoraWebApi.Common;
using GestoraWebApi.Services.PrenotazioniPostazioni;
using GestoraWebApi.Validators;
using Microsoft.Extensions.Options;

namespace GestoraWebApi.Tests.Validators;

/// <summary>
/// REV-027: check-disponibilita e' l'unico endpoint pubblico dell'API ed era l'unico DTO in
/// ingresso senza validator. Questi test fissano il contratto del nuovo validator.
/// </summary>
public class CheckDisponibilitaDTOValidatorTests
{
    private static readonly DateTime IstanteFisso = new(2026, 9, 4, 10, 0, 0, DateTimeKind.Utc);

    private readonly TestClock _clock = new(IstanteFisso);
    private readonly CheckDisponibilitaDTOValidator _validator;

    public CheckDisponibilitaDTOValidatorTests()
    {
        _validator = new CheckDisponibilitaDTOValidator(_clock,
                      Options.Create(new PrenotazioniSettings { MaxCopertiPerPrenotazione = 50 }));
    }

    private CheckDisponibilitaDTO Dto(DateOnly? data = null, int coperti = 2) => new()
    {
        DataPrenotazione = data ?? _clock.TodayInRome,
        NumeroCoperti = coperti
    };

    [Fact]
    public void RichiestaValida_Passa()
    {
        _validator.TestValidate(Dto(_clock.TodayInRome.AddDays(3))).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Oggi_EAccettato()
    {
        _validator.TestValidate(Dto(_clock.TodayInRome)).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void DataPassata_ERifiutata()
    {
        _validator.TestValidate(Dto(_clock.TodayInRome.AddDays(-1)))
                  .ShouldHaveValidationErrorFor(x => x.DataPrenotazione);
    }

    // Una data non valorizzata arriva come DateOnly.MinValue (anno 1): senza validator finiva
    // in query come qualunque altra data.
    [Fact]
    public void DataNonValorizzata_ERifiutata()
    {
        _validator.TestValidate(Dto(DateOnly.MinValue))
                  .ShouldHaveValidationErrorFor(x => x.DataPrenotazione);
    }

    [Fact]
    public void OltreOrizzonteMassimo_ERifiutata()
    {
        _validator.TestValidate(Dto(_clock.TodayInRome.AddDays(366)))
                  .ShouldHaveValidationErrorFor(x => x.DataPrenotazione);
    }

    [Fact]
    public void UltimoGiornoDellOrizzonte_EAccettato()
    {
        _validator.TestValidate(Dto(_clock.TodayInRome.AddDays(365))).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    [InlineData(51)]
    public void NumeroCopertiFuoriRange_ERifiutato(int coperti)
    {
        _validator.TestValidate(Dto(coperti: coperti))
                  .ShouldHaveValidationErrorFor(x => x.NumeroCoperti);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    public void NumeroCopertiAiLimiti_EAccettato(int coperti)
    {
        _validator.TestValidate(Dto(coperti: coperti)).ShouldNotHaveAnyValidationErrors();
    }

    // Dimostra che il validator usa il limite massimo di coperti dalla configurazione.
    [Fact]
    public void UsaIlTettoDeiCopertiDellaConfigurazione()
    {
        // Arrange
        var validator = new CheckDisponibilitaDTOValidator(_clock,
                      Options.Create(new PrenotazioniSettings { MaxCopertiPerPrenotazione = 10 }));

        // Act & Assert
        validator.TestValidate(Dto(coperti: 10)).ShouldNotHaveAnyValidationErrors();

        validator.TestValidate(Dto(coperti: 11))
                 .ShouldHaveValidationErrorFor(x => x.NumeroCoperti)
                 .WithErrorMessage("Il numero di coperti non può superare 10.");
    }
}
