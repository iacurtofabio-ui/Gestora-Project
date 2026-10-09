namespace GestoraWebApi.Infrastructure.Auth
{
    public class JwtSettings
    {
        public string Secret { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int ExpiryMinutes { get; set; }

        /// <summary>
        /// V2-010: durata massima di una sessione dal login. Il token vale ExpiryMinutes e il
        /// frontend lo rinnova finché la pagina è aperta, ma mai oltre questo limite: superato,
        /// si rifà il login. 12 ore coprono un turno lungo, pranzo più cena.
        /// </summary>
        public int MaxSessionHours { get; set; }
    }
}
