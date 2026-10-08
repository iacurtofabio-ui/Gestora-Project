namespace GestoraWebApi.Services.Sala
{
    /// <summary>Un tavolo ridotto a ciò che serve per contare i posti della sala.</summary>
    public record TavoloSala(long Id, long ZonaId, int Capienza);

    /// <summary>
    /// V2-007: unico punto in cui si contano i posti della sala. Logica pura, nessun database:
    /// la usano il riepilogo della pagina Postazioni e i controlli di coerenza su fasce, tavoli e
    /// zone (<see cref="CoerenzaSalaService"/>).
    ///
    /// Posti = somma semplice delle capienze dei tavoli attivi nelle zone attive, SENZA il bonus
    /// delle testate: due tavoli da 2 fanno 6 solo se un unico gruppo li occupa uniti, mentre due
    /// coppie ne usano 4. Il tetto di una fascia deve stare entro i posti che esistono sempre.
    /// </summary>
    public static class CapienzaSala
    {
        /// <param name="tavoliAttivi">Tavoli attivi (quelli disattivati non vanno passati).</param>
        /// <param name="zoneAttive">Id delle zone attive: i tavoli di altre zone non contano.</param>
        public static int Posti(IEnumerable<TavoloSala> tavoliAttivi, IReadOnlySet<long> zoneAttive) =>
            tavoliAttivi.Where(t => zoneAttive.Contains(t.ZonaId)).Sum(t => t.Capienza);
    }
}
