namespace GestoraWebApi.Enums
{
    /// <summary>
    /// V2-007: perché una fascia è (o non è) prenotabile per i coperti richiesti.
    /// Viaggia come testo in <c>FasciaDisponibilitaDTO.Motivo</c>: la pagina pubblica sceglie da
    /// qui la frase per il cliente, invece di interpretare <c>Messaggio</c> (testo per lo Staff).
    /// </summary>
    public enum MotivoDisponibilita
    {
        /// <summary>Tetto e tavoli bastano: si può prenotare.</summary>
        Libera = 0,

        /// <summary>La fascia di oggi è già finita.</summary>
        Terminata = 1,

        /// <summary>Il tetto della fascia è raggiunto: nessun coperto disponibile.</summary>
        TettoEsaurito = 2,

        /// <summary>Restano coperti sotto il tetto, ma meno di quelli richiesti.</summary>
        PostiInsufficienti = 3,

        /// <summary>Il tetto lascerebbe spazio, ma nessuna combinazione di tavoli liberi basta.</summary>
        TavoliInsufficienti = 4
    }
}
