using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GestoraWebApi.Infrastructure.Auth;
using Microsoft.Extensions.Options;

namespace GestoraWebApi.Tests.Infrastructure;

/// <summary>
/// V2-010 — il token vale ExpiryMinutes e si rinnova, ma mai oltre MaxSessionHours dal login.
/// Si legge il token emesso (senza verificarne la firma, qui non serve) e si guardano la scadenza
/// e il claim con l'inizio della sessione.
/// </summary>
public class JwtTokenGeneratorTests
{
    private static readonly DateTime Adesso = new(2026, 10, 9, 18, 0, 0, DateTimeKind.Utc);

    private static JwtTokenGenerator Generatore(TestClock clock) => new(
        Options.Create(new JwtSettings
        {
            Secret = "segreto-di-prova-lungo-almeno-32-caratteri",
            Issuer = "GestoraWebApi",
            Audience = "GestoraClient",
            ExpiryMinutes = 60,
            MaxSessionHours = 12
        }),
        clock);

    private static JwtSecurityToken Leggi(string token) => new JwtSecurityTokenHandler().ReadJwtToken(token);

    private static string NuovoToken(JwtTokenGenerator generatore)
        => generatore.GenerateToken("utente-1", "staff@gestora.it", ["Staff"], "stamp-1");

    private static string TokenRinnovato(JwtTokenGenerator generatore, DateTime inizioSessione)
        => generatore.GenerateToken("utente-1", "staff@gestora.it", ["Staff"], "stamp-1", inizioSessione);

    private static DateTime? InizioSessioneDi(string token)
        => JwtTokenGenerator.LeggiInizioSessione(new ClaimsPrincipal(new ClaimsIdentity(Leggi(token).Claims)));

    [Fact]
    public void Login_TokenDiSessantaMinuti_ConLInizioSessioneAdesso()
    {
        var token = NuovoToken(Generatore(new TestClock(Adesso)));

        Assert.Equal(Adesso.AddMinutes(60), Leggi(token).ValidTo);
        Assert.Equal(Adesso, InizioSessioneDi(token));
    }

    [Fact]
    public void Rinnovo_ConservaLInizioDellaSessione_ESpostaLaScadenza()
    {
        var clock = new TestClock(Adesso.AddHours(3));
        var token = TokenRinnovato(Generatore(clock), Adesso);

        Assert.Equal(Adesso, InizioSessioneDi(token));
        Assert.Equal(Adesso.AddHours(3).AddMinutes(60), Leggi(token).ValidTo);
    }

    [Fact]
    public void Rinnovo_VicinoAlLimite_ScadeAlLimiteDelleDodiciOre()
    {
        // A 11 ore e mezza dal login un token da 60 minuti andrebbe oltre: si ferma a 12 ore.
        var clock = new TestClock(Adesso.AddHours(11).AddMinutes(30));
        var token = TokenRinnovato(Generatore(clock), Adesso);

        Assert.Equal(Adesso.AddHours(12), Leggi(token).ValidTo);
    }

    [Fact]
    public void SessioneAncoraValida_PrimaDelLimite_True()
    {
        var generatore = Generatore(new TestClock(Adesso.AddHours(11).AddMinutes(59)));

        Assert.True(generatore.SessioneAncoraValida(Adesso));
    }

    [Fact]
    public void SessioneAncoraValida_AlLimite_False()
    {
        var generatore = Generatore(new TestClock(Adesso.AddHours(12)));

        Assert.False(generatore.SessioneAncoraValida(Adesso));
    }

    [Fact]
    public void LeggiInizioSessione_TokenSenzaIlClaim_Null()
    {
        // Token emessi prima di V2-010: la loro sessione non si rinnova, si rifà il login.
        var utente = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "utente-1")]));

        Assert.Null(JwtTokenGenerator.LeggiInizioSessione(utente));
    }
}
