namespace GestoraWebApi.Services.Postazioni.DTOs
{
    public class PostazioneUpdateDTO
    {
        public long Id { get; set; }
        public int Numero { get; set; }
        public int CapienzaMassima { get; set; }
        public long ZonaId { get; set; }
    }
}
