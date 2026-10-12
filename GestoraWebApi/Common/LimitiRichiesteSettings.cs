namespace GestoraWebApi.Common
{
    /// <summary>
    /// V2-011: quante richieste accettano gli endpoint pubblici prima di rispondere 429.
    /// In configurazione e non nel codice: sono numeri da tarare sull'uso reale, e su Azure si
    /// cambiano senza ripubblicare (<c>LimitiRichieste__...</c>). Controllati all'avvio.
    /// </summary>
    public class LimitiRichiesteSettings
    {
        public const string Sezione = "LimitiRichieste";

        /// <summary>
        /// Registrazioni ammesse per indirizzo nella finestra. Un utente vero si registra una
        /// volta, con qualche errore di compilazione: il margine copre anche più persone sullo
        /// stesso wifi o sulla stessa rete mobile.
        /// </summary>
        public int RegistrazioniPerFinestra { get; set; }

        /// <summary>Durata della finestra della registrazione, in minuti.</summary>
        public int RegistrazioneFinestraMinuti { get; set; }

        /// <summary>
        /// Verifiche di disponibilità di fila, senza attesa. Il form di prenotazione la chiama a
        /// ogni cambio di data o coperti, quindi chi compila ne fa parecchie in pochi secondi.
        /// </summary>
        public int DisponibilitaRaffica { get; set; }

        /// <summary>Verifiche al minuto che si recuperano dopo la raffica (ritmo sostenuto).</summary>
        public int DisponibilitaAlMinuto { get; set; }
    }
}
