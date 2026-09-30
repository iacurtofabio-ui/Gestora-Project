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

        /// <summary>
        /// FASE 4: posizione (1-based) della fascia fra le fasce attive dello stesso giorno della
        /// settimana, ordinate per orario di inizio. Calcolato nel service (una query sola sulle
        /// fasce, gia in cache), non in AutoMapper: serve un elenco esterno per il confronto.
        /// </summary>
        public int NumeroTurno { get; set; }
    }

    public class PostazioneAssegnataDTO
    {
        public int Numero { get; set; }
        public string? NomeZona { get; set; }
        public long ZonaId { get; set; }

        /// <summary>
        /// FASE 4 (era gia REV-001/NEW-001): il dato esiste dal checkpoint 2b
        /// (PrenotazionePostazione.NumeroPosti), non era mai uscito nell API.
        /// </summary>
        public int NumeroPosti { get; set; }
    }
}
