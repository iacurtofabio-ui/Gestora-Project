namespace GestoraWebApi.Enums
{
    public enum StatoPrenotazione
    {
        Attiva = 0,
        InCorso = 1,
        Annullata = 2,
        Completata = 3,

        /// <summary>
        /// FASE 3: prenotazione creata ma mai confermata, con data/fascia ormai passate. Prima
        /// restava "Attiva" per sempre (la dashboard settimanale la contava già come no-show, ma
        /// solo per calcolo: in tabella continuava a proporre "Conferma"/"Annulla" su qualcosa
        /// che non aveva più senso toccare). <c>HasConversion&lt;string&gt;</c> in GestoraContext:
        /// aggiungere un valore qui non tocca lo schema, nessuna migration.
        /// </summary>
        NonPresentata = 4
    }
}
