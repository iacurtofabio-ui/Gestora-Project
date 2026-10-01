using GestoraWebApi.Infrastructure.Auth;
using Microsoft.AspNetCore.Identity;

namespace GestoraWebApi.Tests.Infrastructure;

/// <summary>
/// Registrazione con email o nome gia' usati: prima il frontend riceveva gli errori di Identity
/// in una forma che non sapeva leggere (e in inglese) e mostrava "Registrazione non riuscita".
/// </summary>
public class ErroriIdentityTests
{
    private readonly IdentityErrorDescriberItaliano _descrittore = new();

    [Fact]
    public void EmailENomeDoppi_FinisconoSottoIlCampoGiusto_InItaliano()
    {
        var result = IdentityResult.Failed(
            _descrittore.DuplicateEmail("fabio@example.com"),
            _descrittore.DuplicateUserName("Fabio Iacurto"));

        var ex = ErroriIdentity.ComeValidationException(result, "Registrazione non riuscita.");

        Assert.Contains("già registrata", ex.Errors["email"].Single());
        Assert.Contains("già in uso", ex.Errors["username"].Single());
        Assert.Equal("Registrazione non riuscita.", ex.Message);
    }

    [Fact]
    public void ErroriDellaPassword_FinisconoSottoPassword()
    {
        var result = IdentityResult.Failed(_descrittore.PasswordRequiresDigit());

        var ex = ErroriIdentity.ComeValidationException(result, "x");

        Assert.True(ex.Errors.ContainsKey("password"));
    }
}
