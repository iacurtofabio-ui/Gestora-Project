namespace GestoraWebApi.Services.PrenotazioniPostazioni
{
    public class PostazioneDisponibilitaDTO
    {
        public long PostazioneId { get; set; }
        public int Numero { get; set; }
        public int Capienza { get; set; }
        public int PostiOccupati { get; set; }
        public int PostiDisponibili { get; set; } // capienza - posti occupati

        // se true => questa singola postazione soddisfa la richiesta (numeroCoperti)
        public bool DisponibilePerRichiesta { get; set; }
    }
}
