import { QueryClient } from '@tanstack/react-query'
import { AxiosError } from 'axios'

/**
 * REV-049 — impostazioni di base delle query, prima lasciate ai default di React Query.
 *
 * I due difetti che si volevano chiudere:
 *
 * 1. **Ritentare quello che non puo' riuscire.** Il default riprova tre volte ogni query fallita.
 *    Su un 4xx non ha senso: un 403 e' una decisione del server, non un disturbo di rete, e
 *    riprovarlo produce solo altre quattro chiamate rifiutate. E' successo davvero in Fase 6, dove
 *    la select delle zone per il Cliente generava quattro 403 di fila (difetto 1/4). Un 401 in piu'
 *    e' anche peggio, perche' ogni tentativo passa dall'interceptor che chiude la sessione.
 *    Sui 5xx e sugli errori di rete i tentativi restano, perche' li' hanno senso.
 *
 * 2. **Ricaricare tutto in continuazione.** Con staleTime a 0 ogni ritorno sulla finestra
 *    riscarica ogni lista visibile. Zone, fasce e postazioni cambiano qualche volta al giorno:
 *    30 secondi di dato considerato fresco tolgono la gran parte di quelle chiamate senza che
 *    l'utente veda mai un dato vecchio in modo percepibile. Le scritture invalidano comunque a
 *    mano le proprie chiavi, quindi dopo un salvataggio la lista si aggiorna subito.
 */
function daRiprovare(numeroTentativi: number, errore: unknown): boolean {
  const stato = errore instanceof AxiosError ? errore.response?.status : undefined

  // Nessuno stato: la richiesta non e' mai arrivata (rete, backend spento). Vale la pena riprovare.
  if (stato !== undefined && stato >= 400 && stato < 500) return false

  return numeroTentativi < 2
}

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: daRiprovare,
      staleTime: 30_000,
      refetchOnWindowFocus: false,
    },
    mutations: {
      // Una mutation ripetuta da sola puo' creare un doppione: qui non si ritenta mai.
      retry: false,
    },
  },
})
