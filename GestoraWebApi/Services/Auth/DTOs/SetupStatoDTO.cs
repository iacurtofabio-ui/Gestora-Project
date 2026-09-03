namespace GestoraWebApi.Services.Auth.DTOs
{
    /// <summary>
    /// Risposta di GET /api/Setup/stato. Contiene solo il minimo indispensabile al frontend per
    /// decidere se mostrare la schermata di primo avvio: nessun dettaglio sugli utenti esistenti,
    /// perche' l'endpoint e' pubblico.
    /// </summary>
    public class SetupStatoDTO
    {
        public bool SetupCompletato { get; set; }
    }
}
