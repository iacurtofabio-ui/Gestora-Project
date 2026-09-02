namespace GestoraWebApi.Infrastructure.Exceptions
{
    /// <summary>
    /// Lo stato attuale della risorsa non permette l'operazione richiesta: risorsa gia' esistente,
    /// stato incompatibile, slot appena occupato da qualcun altro. Mappata a 409 dal middleware.
    /// E' l'unica eccezione che produce un 409 (REV-026): una InvalidOperationException che
    /// affiora fino al middleware e' un errore interno, non una regola di dominio.
    /// </summary>
    public class ConflictException : Exception
    {
        public ConflictException(string message) : base(message)
        {
        }

        public ConflictException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
