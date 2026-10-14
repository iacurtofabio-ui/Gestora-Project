namespace GestoraWebApi.Common
{
    /// <summary>
    /// Codici stabili che il frontend riconosce nelle risposte di errore, per scegliere cosa
    /// mostrare senza dipendere dal testo del messaggio.
    /// </summary>
    public static class CodiciErrore
    {
        /// <summary>V2-003: login con password giusta ma email non ancora confermata (403).</summary>
        public const string EmailNonConfermata = "EMAIL_NON_CONFERMATA";
    }
}
