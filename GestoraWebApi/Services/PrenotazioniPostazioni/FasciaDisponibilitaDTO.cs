namespace GestoraWebApi.Services.PrenotazioniPostazioni
{
    public class FasciaDisponibilitaDTO
    {
        public long FasciaOrariaId { get; set; }
        public TimeOnly OrarioInizio { get; set; }
        public TimeOnly OrarioFine { get; set; }

        // posti disponibili totali nella fascia (tiene conto anche di prenotazioni non assegnate)
        public int TotalePostiDisponibili { get; set; }
        public int TotaleCapienza { get; set; }
        public bool DisponibilePerRichiesta { get; set; }

        public List<PostazioneDisponibilitaDTO> Postazioni { get; set; } = new();
    }
}
