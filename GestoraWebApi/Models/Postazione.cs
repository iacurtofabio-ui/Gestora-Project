namespace GestoraWebApi.Models
{
    public class Postazione : Entita
    {
        public int Numero { get; set; }
        public int CapienzaMassima { get; set; }
        public bool Attiva { get; set; } = true;

        public long ZonaId { get; set; }
        public Zona Zona { get; set; }


        public ICollection<PrenotazionePostazione> PrenotazioniPostazioni { get; set; }
    }
}
