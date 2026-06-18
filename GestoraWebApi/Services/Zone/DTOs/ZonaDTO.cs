namespace GestoraWebApi.Services.Zone.DTOs
{
    public class ZonaDTO
    {
        public long Id { get; set; }
        public string Nome { get; set; }
        public bool Attiva { get; set; } = true;
    }
}
