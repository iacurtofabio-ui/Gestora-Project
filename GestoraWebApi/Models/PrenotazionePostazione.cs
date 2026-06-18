namespace GestoraWebApi.Models
{
    public class PrenotazionePostazione
    {
        public long PostazioneId { get; set; }
        public virtual Postazione Postazione { get; set; }

        public long PrenotazioneId { get; set; }
        public virtual Prenotazione Prenotazione { get; set; }

        public int NumeroPosti { get; set; } = 0;

    }
}
