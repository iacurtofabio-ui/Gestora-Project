namespace GestoraWebApi.Models
{
    public class Zona : Entita
    {
        public required string Nome { get; set; }

        public bool Attiva { get; set; } = true;

        public ICollection<Postazione> Postazioni { get; set; } = new List<Postazione>();
    }
}
