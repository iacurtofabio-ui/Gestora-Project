namespace GestoraWebApi.Models
{
    public class FasciaOraria : Entita
    {
        public TimeOnly OrarioInizio { get; set; }
        public TimeOnly OrarioFine { get; set; }
        public DayOfWeek GiornoSettimana { get; set; }
        public int MaxPrenotazioni { get; set; }
        public bool Attiva { get; set; }



        // relazione con Prenotazione
        public virtual ICollection<Prenotazione> Prenotazioni { get; set; } = new List<Prenotazione>();
    }
}
