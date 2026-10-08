namespace GestoraWebApi.Services.Prenotazioni.DTOs
{
    /// <summary>Limiti delle prenotazioni che il frontend deve conoscere per costruire i form.</summary>
    public class LimitiPrenotazioneDTO
    {
        /// <summary>Limite tecnico, per tutti i ruoli (form dello Staff).</summary>
        public int MaxCopertiPerPrenotazione { get; set; }

        /// <summary>V2-007: limite per il Cliente e la pagina pubblica; oltre, si contatta il ristorante.</summary>
        public int MaxCopertiPrenotazioneOnline { get; set; }
    }
}
