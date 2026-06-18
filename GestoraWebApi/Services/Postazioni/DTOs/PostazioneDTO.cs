namespace GestoraWebApi.Services.Postazioni.DTOs
{
    public class PostazioneDTO
    {
        public long Id { get; set; }
        public int Numero { get; set; }
        public int CapienzaMassima { get; set; }
        public long ZonaId { get; set; }
        public bool Attiva { get; set; }
        public List<long> PrenotazioneId { get; set; } = new();

    }
}
