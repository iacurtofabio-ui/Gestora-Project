namespace GestoraWebApi.Infrastructure.Exceptions
{
    /// <summary>
    /// L'utente e' autenticato ma non ha il diritto di compiere l'operazione su quella risorsa
    /// (tipicamente un Cliente su una prenotazione che non e' sua). Mappata a 403 dal middleware.
    ///
    /// REV-025: prima questi casi sollevavano UnauthorizedAccessException e finivano in un 401,
    /// che per il frontend significa "sessione scaduta" e fa scattare il logout automatico.
    /// Un permesso negato non e' una sessione scaduta: il token e' valido, va mostrato l'errore
    /// senza buttare fuori l'utente. UnauthorizedAccessException resta riservata al caso vero
    /// di richiesta non autenticata.
    /// </summary>
    public class ForbiddenException : Exception
    {
        public ForbiddenException(string message) : base(message)
        {
        }

        public ForbiddenException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
