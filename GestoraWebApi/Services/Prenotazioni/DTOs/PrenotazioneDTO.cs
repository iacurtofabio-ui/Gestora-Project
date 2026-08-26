namespace GestoraWebApi.Services.Prenotazioni.DTOs
{
    public class PrenotazioneDTO
    {
        public long Id { get; set; }
        public DateOnly DataPrenotazione { get; set; }
        public int NumeroCoperti { get; set; }
        public string? Note { get; set; }
        public string? Stato { get; set; }

        // Utente
        public string? NomeUtente { get; set; }
        public string? NomeCliente { get; set; }

        // Fascia Oraria
        public string? OraInizio { get; set; }
        public string? OraFine { get; set; }
        public long FasciaOrariaId { get; set; }

        // Postazioni assegnate
        public List<PostazioneAssegnataDTO> Postazioni { get; set; } = [];
    }

    public class PostazioneAssegnataDTO
    {
        public int Numero { get; set; }
        public string? NomeZona { get; set; }
        public long ZonaId { get; set; }
    }
}
