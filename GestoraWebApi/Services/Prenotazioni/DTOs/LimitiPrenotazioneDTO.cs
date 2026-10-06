namespace GestoraWebApi.Services.Prenotazioni.DTOs
{
    /// <summary>Limiti delle prenotazioni che il frontend deve conoscere per costruire i form.</summary>
    public class LimitiPrenotazioneDTO
    {
        public int MaxCopertiPerPrenotazione { get; set; }
    }
}
