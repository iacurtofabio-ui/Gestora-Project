using FluentValidation.TestHelper;
using GestoraWebApi.Services.FasciaOrarie.DTOs;
using GestoraWebApi.Validators;

namespace GestoraWebApi.Tests.Validators;

/// <summary>
/// Una fascia vive dentro un solo giorno: il dominio calcola la fine come data + OrarioFine.
/// Una fascia "20:00-00:00" risultava finita a mezzanotte del giorno stesso, cioe' prima di
/// cominciare: prenotazioni rifiutate come "gia' passate" e segnate "Non presentata" dal job
/// notturno la mattina stessa. Il validator ora la rifiuta all'ingresso.
/// </summary>
public class FasciaOrariaDTOValidatorTests
{
    private readonly FasciaOrariaDTOValidator _validator = new();

    private static FasciaOrariaDTO Dto(string inizio, string fine) => new()
    {
        GiornoSettimana = DayOfWeek.Saturday,
        MaxCoperti = 40,
        Attiva = true,
        OrarioInizio = inizio,
        OrarioFine = fine
    };

    [Fact]
    public void FasciaNormale_Passa()
    {
        _validator.TestValidate(Dto("19:00", "23:00")).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void FineAlle2359_Passa()
    {
        _validator.TestValidate(Dto("20:00", "23:59")).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void FineAMezzanotte_ERifiutata()
    {
        _validator.TestValidate(Dto("20:00", "00:00")).ShouldHaveValidationErrorFor(x => x.OrarioFine);
    }

    [Fact]
    public void FinePrimaDellInizio_ERifiutata()
    {
        _validator.TestValidate(Dto("12:00", "08:00")).ShouldHaveValidationErrorFor(x => x.OrarioFine);
    }

    [Fact]
    public void FineUgualeAllInizio_ERifiutata()
    {
        _validator.TestValidate(Dto("12:00", "12:00")).ShouldHaveValidationErrorFor(x => x.OrarioFine);
    }
}
