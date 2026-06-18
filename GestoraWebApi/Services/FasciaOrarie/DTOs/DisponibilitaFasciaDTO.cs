namespace GestoraWebApi.Services.FasciaOrarie.DTOs
{
    public class DisponibilitaFasciaDTO
    {
        public long FasciaId { get; set; }
        public int PostiTotali { get; set; }
        public int PostiOccupati { get; set; }
        public int PostiDisponibili { get; set; }
        public bool Disponibile { get; set; }
    }
}
