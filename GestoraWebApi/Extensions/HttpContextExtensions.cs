using GestoraWebApi.Common;
using Microsoft.AspNetCore.Http;

namespace GestoraWebApi.Extensions
{
    /// <summary>
    /// REV-059: id utente autenticato e indirizzo IP servono a ogni scrittura che va
    /// nell'audit trail. Prima ogni service (FasciaOrariaService, PostazioneService,
    /// ZonaService, PrenotazioniService) e due controller (AuthenticationUserController,
    /// SetupController) avevano una coppia di metodi privati identici, copiata a mano.
    /// Centralizzati qui, sopra i due helper gia' esistenti (ClaimsPrincipalExtensions,
    /// Common/IndirizzoClient), che restano il punto vero di lettura.
    /// </summary>
    public static class HttpContextExtensions
    {
        public static string GetAuthenticatedUserId(this HttpContext? context)
            => context?.User.GetAuthenticatedUserId()
               ?? throw new UnauthorizedAccessException("Utente non autenticato.");

        public static string? GetIpAddress(this HttpContext? context)
            => IndirizzoClient.Ottieni(context);
    }
}
