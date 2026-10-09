using GestoraWebApi.Common;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace GestoraWebApi.Infrastructure.Auth
{
    public interface IJwtTokenGenerator
    {
        /// <summary>Token di una sessione nuova (login): la sessione inizia adesso.</summary>
        string GenerateToken(string userId, string email, IEnumerable<string> roles, string securityStamp);

        /// <summary>
        /// Token che prosegue una sessione già aperta (V2-010, rinnovo): conserva l'inizio della
        /// sessione, così il limite massimo dal login non si sposta mai in avanti.
        /// </summary>
        string GenerateToken(string userId, string email, IEnumerable<string> roles, string securityStamp,
                             DateTime inizioSessioneUtc);

        /// <summary>True se la sessione iniziata a quell'istante non ha ancora superato la durata massima.</summary>
        bool SessioneAncoraValida(DateTime inizioSessioneUtc);
    }

    public class JwtTokenGenerator : IJwtTokenGenerator
    {
        private readonly JwtSettings _settings;
        private readonly IClock _clock;

        public JwtTokenGenerator(IOptions<JwtSettings> settings, IClock clock)
        {
            _settings = settings.Value;
            _clock = clock;
        }

        /// <summary>
        /// Nome del claim con il security stamp dell'utente. AUD-M6: a ogni richiesta
        /// AuthenticationExtensions lo confronta con quello attuale; cambio di ruolo, eliminazione
        /// o reset password cambiano lo stamp e i token gia' emessi smettono di valere subito,
        /// invece di restare validi fino alla scadenza.
        /// </summary>
        public const string ClaimSecurityStamp = "stamp";

        /// <summary>
        /// V2-010: istante del login, in secondi dall'epoca UTC. Passa invariato da un rinnovo
        /// all'altro. Nome proprio e non lo standard <c>auth_time</c>, che il middleware JWT
        /// rinominerebbe in ingresso.
        /// </summary>
        public const string ClaimInizioSessione = "inizio_sessione";

        public string GenerateToken(string userId, string email, IEnumerable<string> roles, string securityStamp)
            => GenerateToken(userId, email, roles, securityStamp, _clock.UtcNow);

        public string GenerateToken(string userId, string email, IEnumerable<string> roles, string securityStamp,
                                    DateTime inizioSessioneUtc)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(ClaimSecurityStamp, securityStamp),
                new Claim(ClaimInizioSessione,
                    new DateTimeOffset(inizioSessioneUtc, TimeSpan.Zero).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture),
                    ClaimValueTypes.Integer64)
            };

            // Un claim per ogni ruolo — ASP.NET Core li legge tutti
            foreach (var role in roles)
                claims.Add(new Claim(ClaimTypes.Role, role));

            var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer:            _settings.Issuer,
                audience:          _settings.Audience,
                claims:            claims,
                expires:           Scadenza(inizioSessioneUtc),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public bool SessioneAncoraValida(DateTime inizioSessioneUtc)
            => _clock.UtcNow < FineSessione(inizioSessioneUtc);

        /// <summary>
        /// Legge l'inizio della sessione dal token di chi chiama. Null se manca o non è leggibile,
        /// per esempio in un token emesso prima di V2-010: quella sessione non si rinnova.
        /// </summary>
        public static DateTime? LeggiInizioSessione(ClaimsPrincipal utente)
        {
            var valore = utente.FindFirstValue(ClaimInizioSessione);
            return long.TryParse(valore, NumberStyles.Integer, CultureInfo.InvariantCulture, out var secondi)
                ? DateTimeOffset.FromUnixTimeSeconds(secondi).UtcDateTime
                : null;
        }

        // Il token vale ExpiryMinutes, ma non va mai oltre la fine della sessione.
        private DateTime Scadenza(DateTime inizioSessioneUtc)
        {
            var scadenzaNormale = _clock.UtcNow.AddMinutes(_settings.ExpiryMinutes);
            var fine = FineSessione(inizioSessioneUtc);
            return scadenzaNormale < fine ? scadenzaNormale : fine;
        }

        private DateTime FineSessione(DateTime inizioSessioneUtc)
            => inizioSessioneUtc.AddHours(_settings.MaxSessionHours);
    }
}
