import { isAxiosError, type AxiosError } from 'axios'
import { toast } from 'sonner'
import type { ApiErrorResponse } from '@/types/apiError'

/**
 * REV-041 — un solo punto per tradurre l'errore dell'API in un messaggio da mostrare.
 *
 * Lo stesso blocco di sei righe era ripetuto identico in 26 punti fra hook e componenti: cambiava
 * soltanto la frase di ripiego. Oltre alla duplicazione il problema era pratico - il giorno in cui
 * il backend cambia la forma della risposta d'errore, o si vuole distinguere un caso in piu',
 * bisogna ricordarsi di 26 posti.
 *
 * L'ordine di lettura riflette quello che il backend produce davvero (vedi ExceptionMiddleware):
 * gli errori di validazione arrivano nell'array `errors` con un messaggio per campo, tutto il
 * resto in `message`.
 */
export function messaggioErrore(error: AxiosError<ApiErrorResponse>, fallback: string): string {
  // Nessuna risposta significa che la richiesta non e' mai arrivata: backend spento, rete assente
  // o CORS. Dire "errore durante la creazione" qui manderebbe fuori strada, perche' la creazione
  // non e' nemmeno stata tentata.
  if (!error.response) {
    return 'Server non raggiungibile. Controlla la connessione e riprova.'
  }

  const data = error.response.data
  const errors = data?.errors ?? []

  if (errors.length > 0) {
    return errors.map((e) => e.error).join(', ')
  }

  return data?.message ?? fallback
}

/**
 * Gestore di errore pronto da passare a `onError` di React Query.
 *
 * Uso: `onError: segnalaErrore('Errore durante la creazione')`.
 */
export function segnalaErrore(fallback: string) {
  return (error: AxiosError<ApiErrorResponse>) => {
    toast.error(messaggioErrore(error, fallback))
  }
}

/**
 * NEW-006 — stessa idea di `messaggioErrore`, ma per gli errori di *caricamento* (React Query
 * `error` di una query, non di una mutation). Prima ogni pagina mostrava lo stesso testo fisso
 * ("Errore nel caricamento") a prescindere dalla causa: backend spento, permessi mancanti o
 * database in errore erano indistinguibili. `error` di una query non è tipizzato come
 * `AxiosError`: va verificato con `isAxiosError` prima di riusare `messaggioErrore`.
 */
export function messaggioErroreCaricamento(error: unknown, fallback: string): string {
  if (isAxiosError<ApiErrorResponse>(error)) {
    return messaggioErrore(error, fallback)
  }
  return fallback
}
