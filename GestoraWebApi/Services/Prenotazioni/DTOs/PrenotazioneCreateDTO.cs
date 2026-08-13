namespace GestoraWebApi.Services.Prenotazioni.DTOs
{
    public class PrenotazioneCreateDTO
    {
        public DateOnly DataPrenotazione { get; set; }
        public int NumeroCoperti { get; set; }
        public string? Note { get; set; }
        public long FasciaOrariaId { get; set; }
        public long? ZonaId { get; set; } = null;
    }
}
