namespace GestoraWebApi.Common
{
    public class PrenotazioniSettings
    {
        public const string Sezione = "Prenotazioni";

        /// <summary>
        /// Limite tecnico dei coperti per una prenotazione, per tutti i ruoli (Staff e Admin
        /// compresi). Lo applicano i validatori.
        /// </summary>
        public int MaxCopertiPerPrenotazione { get; set; }

        /// <summary>
        /// V2-007: limite per chi prenota da solo (Cliente e pagina pubblica). Oltre, si contatta
        /// il ristorante; lo Staff al telefono può arrivare fino al limite tecnico.
        /// Mai più alto di <see cref="MaxCopertiPerPrenotazione"/> (controllato all'avvio).
        /// </summary>
        public int MaxCopertiPrenotazioneOnline { get; set; }
    }
}
