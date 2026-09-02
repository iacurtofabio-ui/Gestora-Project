namespace GestoraWebApi.Models
{
    public class PrenotazionePostazione
    {
        public long PostazioneId { get; set; }
        public virtual Postazione Postazione { get; set; }

        public long PrenotazioneId { get; set; }
        public virtual Prenotazione Prenotazione { get; set; }

        public int NumeroPosti { get; set; } = 0;

        // Copia denormalizzata dello slot della prenotazione (REV-003). Serve solo a rendere
        // possibile l'unique index UX_PrenotazionePostazione_Slot: la coppia data/fascia vive
        // su Prenotazione, ma un vincolo di unicita' puo' insistere solo su colonne della
        // stessa tabella. Non e' una FK verso FasceOrarie: l'integrita' e' gia' garantita
        // dalla FK verso Prenotazione, da cui questi due valori sono sempre copiati.
        public DateOnly DataPrenotazione { get; set; }

        public long FasciaOrariaId { get; set; }

    }
}
