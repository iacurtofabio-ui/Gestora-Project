using GestoraWebApi.Auth;
using GestoraWebApi.Enums;

namespace GestoraWebApi.Models
{
    public class Prenotazione : Entita
    {
        public DateOnly DataPrenotazione { get; set; }
        public required int NumeroCoperti { get; set; }
        public StatoPrenotazione Stato { get; set; } = StatoPrenotazione.Attiva;
        public string? Note { get; set; }

        
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }

        public long FasciaOrariaId { get; set; }
        public virtual FasciaOraria FasciaOraria { get; set; }


        public ICollection<PrenotazionePostazione> PrenotazioniPostazioni { get; set; }
    }
}
