namespace GestoraWebApi.Services.Sala
{
    /// <summary>
    /// V2-007: il tetto di una fascia attiva non può superare i posti della sala. Si controlla
    /// quando si configura (fasce, tavoli, zone), non quando si prenota.
    /// </summary>
    public interface ICoerenzaSalaService
    {
        /// <summary>Posti della sala adesso: tavoli attivi nelle zone attive, somma semplice.</summary>
        Task<int> GetPostiSalaAsync();

        /// <summary>
        /// Per una fascia attiva che si sta salvando o attivando: 409 se il tetto supera i posti.
        /// </summary>
        Task VerificaTettoFasciaAsync(int maxCoperti);

        /// <summary>
        /// Per una modifica a tavoli o zone: calcola i posti come sarebbero DOPO la modifica e dà
        /// 409 se scendono sotto il tetto di una fascia attiva. Una modifica che non riduce i
        /// posti passa sempre, anche se la sala fosse già incoerente.
        /// </summary>
        /// <param name="tavoliDopo">Riceve i tavoli attivi di oggi, restituisce quelli dopo la modifica.</param>
        /// <param name="zoneAttiveDopo">Riceve le zone attive di oggi, restituisce quelle dopo (facoltativo).</param>
        Task VerificaModificaSalaAsync(
            Func<IReadOnlyList<TavoloSala>, IEnumerable<TavoloSala>> tavoliDopo,
            Func<IReadOnlySet<long>, IEnumerable<long>>? zoneAttiveDopo = null);
    }
}
