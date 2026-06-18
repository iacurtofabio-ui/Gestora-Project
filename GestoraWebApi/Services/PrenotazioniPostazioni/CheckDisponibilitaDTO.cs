namespace GestoraWebApi.Services.PrenotazioniPostazioni
{
    public class CheckDisponibilitaDTO
    {
        public DateOnly DataPrenotazione { get; set; }
        public int NumeroCoperti { get; set; } = 1; // numero richiesto (default 1)
    }
}
