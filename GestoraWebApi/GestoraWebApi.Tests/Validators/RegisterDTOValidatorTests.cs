using FluentValidation.TestHelper;
using GestoraWebApi.Services.Auth.DTOs;
using GestoraWebApi.Validators;

namespace GestoraWebApi.Tests.Validators;

/// <summary>
/// Il nome utente ammette gli spazi ("Fabio Iacurto"), ma non in testa, in coda o doppi:
/// sembrerebbero lo stesso nome di un altro utente pur essendo diversi.
/// </summary>
public class RegisterDTOValidatorTests
{
    private readonly RegisterDTOValidator _validator = new();

    private static RegisterDTO Dto(string username) => new()
    {
        Username = username,
        Email = "fabio@example.com",
        Password = "Password1!"
    };

    [Theory]
    [InlineData("Fabio Iacurto")]
    [InlineData("D'Angelo")]
    [InlineData("fabio.iacurto")]
    public void NomeValido_Passa(string username)
    {
        _validator.TestValidate(Dto(username)).ShouldNotHaveValidationErrorFor(x => x.Username);
    }

    [Theory]
    [InlineData(" Fabio Iacurto")]
    [InlineData("Fabio Iacurto ")]
    [InlineData("Fabio  Iacurto")]
    public void SpaziInTestaInCodaODoppi_Rifiutati(string username)
    {
        _validator.TestValidate(Dto(username)).ShouldHaveValidationErrorFor(x => x.Username);
    }

    [Fact]
    public void ModificaUtente_StesseRegoleSugliSpazi()
    {
        var validator = new UpdateUserDTOValidator();

        validator.TestValidate(new UpdateUserDTO { UserName = "Fabio  Iacurto" })
                 .ShouldHaveValidationErrorFor(x => x.UserName);
        validator.TestValidate(new UpdateUserDTO { UserName = "Fabio Iacurto" })
                 .ShouldNotHaveAnyValidationErrors();
        // Campi facoltativi: una modifica della sola email non deve validare il nome.
        validator.TestValidate(new UpdateUserDTO { Email = "nuova@example.com" })
                 .ShouldNotHaveAnyValidationErrors();
    }
}
