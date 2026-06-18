using System.Security.Claims;

namespace GestoraWebApi.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        /// <summary>
        /// Restituisce l'ID dell'utente autenticato dal JWT.
        /// Controlla prima il claim "sub" (standard JWT),
        /// poi il claim NameIdentifier (ASP.NET Identity).
        /// Lancia UnauthorizedAccessException se l'utente non è autenticato.
        /// </summary>
        public static string GetAuthenticatedUserId(this ClaimsPrincipal? user)
        {
            var userId = user?.FindFirst("sub")?.Value
                      ?? user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(userId))
                throw new UnauthorizedAccessException("Utente non autenticato.");

            return userId;
        }
    }
}