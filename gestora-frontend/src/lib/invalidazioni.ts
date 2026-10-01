import type { QueryClient } from '@tanstack/react-query'

/**
 * Viste costruite sopra fasce, tavoli, zone e prenotazioni: Dashboard (tetto e coperti per
 * fascia), verifica di disponibilita' nel form di prenotazione, riepilogo della sala in
 * Postazioni. Prima le scritture su fasce, tavoli e zone invalidavano solo il proprio elenco:
 * cambiando il tetto di una fascia, la Dashboard mostrava quello vecchio fino al refresh
 * successivo. Si chiama dall'onSuccess di ogni mutation che tocca la sala.
 */
export function invalidaVisteDellaSala(queryClient: QueryClient) {
  queryClient.invalidateQueries({ queryKey: ['dashboard-giornaliera'] })
  queryClient.invalidateQueries({ queryKey: ['dashboard-settimanale'] })
  queryClient.invalidateQueries({ queryKey: ['disponibilita'] })
  queryClient.invalidateQueries({ queryKey: ['postazioni', 'riepilogo-sala'] })
}
