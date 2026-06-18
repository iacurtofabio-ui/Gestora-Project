namespace GestoraWebApi.Models
{
    /// <summary>
    /// Struttura di risposta errore unificata per tutti gli endpoint.
    /// </summary>
    public class ErrorDetails
    {
        public int StatusCode { get; set; }
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Lista di errori di validazione per campo.
        /// Null per errori non di validazione (404, 500, ecc.).
        /// </summary>
        public List<ErrorItem>? Errors { get; set; }

        /// <summary>
        /// Stack trace — presente solo in ambiente Development.
        /// </summary>
        public string? Details { get; set; }
    }

    public class ErrorItem
    {
        public string Field { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
    }
}