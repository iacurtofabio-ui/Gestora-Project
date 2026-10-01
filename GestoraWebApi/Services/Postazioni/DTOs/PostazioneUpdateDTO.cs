namespace GestoraWebApi.Services.Postazioni.DTOs
{
    public class PostazioneUpdateDTO
    {
        public long Id { get; set; }
        public int Numero { get; set; }
        public int CapienzaMassima { get; set; }
        public long ZonaId { get; set; }

        // Mancava: la casella "Attiva" del modale di modifica arrivava al backend e veniva
        // scartata, quindi un tavolo non si poteva disattivare. Default true: un client che
        // non invia il campo non disattiva il tavolo per sbaglio.
        public bool Attiva { get; set; } = true;
    }
}
