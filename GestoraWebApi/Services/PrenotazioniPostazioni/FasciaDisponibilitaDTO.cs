namespace GestoraWebApi.Services.PrenotazioniPostazioni
{
    public class FasciaDisponibilitaDTO
    {
        public long FasciaOrariaId { get; set; }
        public TimeOnly OrarioInizio { get; set; }
        public TimeOnly OrarioFine { get; set; }

        // Tetto dichiarato della fascia: è questo a decidere quando la fascia è esaurita
        // (decisione 8 della roadmap), non la somma delle capienze dei tavoli.
        public int MaxCoperti { get; set; }

        // Coperti ancora prenotabili nella fascia in base al tetto: MaxCoperti meno i coperti
        // già prenotati (prenotazioni non annullate).
        public int PostiResiduiFascia { get; set; }

        // Alias storico di PostiResiduiFascia, mantenuto per compatibilità con eventuali client.
        public int TotalePostiDisponibili { get; set; }
        public int TotaleCapienza { get; set; }

        public bool DisponibilePerRichiesta { get; set; }

        // Spiega perché la richiesta non è soddisfabile, distinguendo "tetto esaurito" da
        // "tetto libero ma nessuna combinazione di tavoli liberi sufficiente".
        public string? Messaggio { get; set; }

        public List<PostazioneDisponibilitaDTO> Postazioni { get; set; } = new();
    }
}
