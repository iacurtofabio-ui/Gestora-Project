namespace GestoraWebApi.Services.Postazioni.DTOs
{
    public class PostazioniDisponibiliDTO
    {
        public long Id { get; set; }
        public int Numero { get; set; }
        public int CapienzaMassima { get; set; }
        public long ZonaId { get; set; }
        public bool Attiva { get; set; }
        public bool Unita { get; set; }
    }
}
