namespace GestoraWebApi.Common
{
    public class UtenzeSettings
    {
        public const string Sezione = "Utenze";

        /// <summary>
        /// V2-015: dopo quanti giorni dalla registrazione un Cliente mai entrato e senza
        /// prenotazioni viene eliminato dalla pulizia notturna. Controllato all'avvio (&gt; 0).
        /// </summary>
        public int GiorniUtenzaMaiUsata { get; set; }
    }
}
