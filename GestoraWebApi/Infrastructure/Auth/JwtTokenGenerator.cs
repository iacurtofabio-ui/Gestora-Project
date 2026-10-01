using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace GestoraWebApi.Infrastructure.Auth
{
    public interface IJwtTokenGenerator
    {
        string GenerateToken(string userId, string email, IEnumerable<string> roles, string securityStamp);
    }

    public class JwtTokenGenerator : IJwtTokenGenerator
    {
        private readonly JwtSettings _settings;

        public JwtTokenGenerator(IOptions<JwtSettings> settings)
        {
            _settings = settings.Value;
        }

        /// <summary>
        /// Nome del claim con il security stamp dell'utente. AUD-M6: a ogni richiesta
        /// AuthenticationExtensions lo confronta con quello attuale; cambio di ruolo, eliminazione
        /// o reset password cambiano lo stamp e i token gia' emessi smettono di valere subito,
        /// invece di restare validi fino a 60 minuti.
        /// </summary>
        public const string ClaimSecurityStamp = "stamp";

        public string GenerateToken(string userId, string email, IEnumerable<string> roles, string securityStamp)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(ClaimSecurityStamp, securityStamp)
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
                expires:           DateTime.UtcNow.AddMinutes(_settings.ExpiryMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
