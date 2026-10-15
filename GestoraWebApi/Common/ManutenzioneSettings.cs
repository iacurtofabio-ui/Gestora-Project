namespace GestoraWebApi.Common
{
    public class ManutenzioneSettings
    {
        public const string Sezione = "Manutenzione";

        /// <summary>
        /// V2-016: con true ogni chiamata a /api risponde 503 «portale in manutenzione»; /health
        /// resta libero. Si accende e si spegne da configurazione (su Azure
        /// Manutenzione__Attiva), senza toccare il codice. Spenta di default.
        /// </summary>
        public bool Attiva { get; set; }
    }
}
