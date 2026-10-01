using GestoraWebApi.Infrastructure.Exceptions;
using Microsoft.AspNetCore.Identity;

namespace GestoraWebApi.Infrastructure.Auth
{
    /// <summary>
    /// Traduce gli errori di Identity nel formato che il resto dell'API usa per gli errori di
    /// validazione (400 con l'elenco per campo). Prima era solo in SetupController: register e
    /// update-user restituivano result.Errors cosi' com'era, una forma che il frontend non sa
    /// leggere, e l'utente vedeva un generico "riprova" al posto di "questa email e' gia' in uso".
    /// </summary>
    public static class ErroriIdentity
    {
        public static ValidationException ComeValidationException(IdentityResult result, string messaggio)
        {
            var errori = result.Errors
                .GroupBy(CampoDi)
                .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());

            return new ValidationException(messaggio, errori);
        }

        private static string CampoDi(IdentityError errore) => errore.Code switch
        {
            var c when c.StartsWith("Password") => "password",
            "DuplicateUserName" or "InvalidUserName" => "username",
            "DuplicateEmail" or "InvalidEmail" => "email",
            _ => string.Empty
        };
    }

    /// <summary>
    /// Messaggi di Identity in italiano: quelli predefiniti sono in inglese e arrivano tali e
    /// quali all'utente (es. "Email 'x' is already taken."). Tradotti solo quelli che un utente
    /// puo' davvero incontrare; gli altri restano quelli di Identity.
    /// </summary>
    public class IdentityErrorDescriberItaliano : IdentityErrorDescriber
    {
        public override IdentityError DuplicateEmail(string email) => new()
        {
            Code = nameof(DuplicateEmail),
            Description = $"L'email '{email}' è già registrata. Accedi oppure usa un'altra email."
        };

        public override IdentityError DuplicateUserName(string userName) => new()
        {
            Code = nameof(DuplicateUserName),
            Description = $"Il nome utente '{userName}' è già in uso. Scegline un altro."
        };

        public override IdentityError InvalidUserName(string? userName) => new()
        {
            Code = nameof(InvalidUserName),
            Description = "Il nome utente può contenere lettere, numeri, spazi e i simboli . - _ ' @ +"
        };

        public override IdentityError InvalidEmail(string? email) => new()
        {
            Code = nameof(InvalidEmail),
            Description = "L'indirizzo email non è valido."
        };

        public override IdentityError PasswordTooShort(int length) => new()
        {
            Code = nameof(PasswordTooShort),
            Description = $"La password deve avere almeno {length} caratteri."
        };

        public override IdentityError PasswordRequiresDigit() => new()
        {
            Code = nameof(PasswordRequiresDigit),
            Description = "La password deve contenere almeno un numero."
        };

        public override IdentityError PasswordRequiresUpper() => new()
        {
            Code = nameof(PasswordRequiresUpper),
            Description = "La password deve contenere almeno una lettera maiuscola."
        };

        public override IdentityError PasswordRequiresLower() => new()
        {
            Code = nameof(PasswordRequiresLower),
            Description = "La password deve contenere almeno una lettera minuscola."
        };
    }
}
