import { useQuery } from '@tanstack/react-query'
import apiClient from '@/lib/axios'
import { Endpoints } from '@/lib/endpoints'
import type { DisponibilitaResponseDTO, LimitiPrenotazioneDTO } from '@/types/disponibilita'

/**
 * NEW-002 — semaforo di disponibilità nel form di prenotazione, invece di scoprire il rifiuto
 * solo all'invio. `check-disponibilita` è pubblico e risponde con **tutte** le fasce del giorno
 * della settimana scelto, ciascuna già con `disponibilePerRichiesta` e un `messaggio` che
 * distingue tetto esaurito da tavoli insufficienti (vedi DisponibilitaService, checkpoint 2c):
 * una sola chiamata per (data, coperti) basta per l'intera select.
 *
 * In un modulo separato da `usePrenotazioni.ts` di proposito: quel file è mockato per intero nei
 * test di PrenotazioneModal (REV-047), e un secondo hook nello stesso modulo sarebbe sparito con
 * lo stesso mock.
 */
export function useCheckDisponibilita(
  dataPrenotazione: string | undefined,
  numeroCoperti: number | undefined
) {
  return useQuery<DisponibilitaResponseDTO>({
    queryKey: ['disponibilita', dataPrenotazione, numeroCoperti],
    queryFn: () =>
      apiClient
        .post<DisponibilitaResponseDTO>(Endpoints.prenotazione.checkDisponibilita, {
          dataPrenotazione,
          numeroCoperti,
        })
        .then((r) => r.data),
    enabled: Boolean(dataPrenotazione) && Boolean(numeroCoperti) && numeroCoperti! > 0,
    // E' solo un'anteprima: un errore qui non deve disturbare la compilazione del form, e non
    // vale la pena ritentare un input che l'utente sta ancora scrivendo.
    retry: false,
  })
}

/**
   * i limiti delle prenotazioni (oggi il tetto dei coperti) vengono dalla configurazione
   * del backend: il frontend non li scrive più a mano. Endpoint pubblico, usabile anche dalla
   * pagina senza account.
   *
   * `staleTime: Infinity`: il valore cambia solo quando si riavvia il backend.
   */
export function useLimitiPrenotazione() {
  return useQuery<LimitiPrenotazioneDTO>({
    queryKey: ['limiti-prenotazione'],
    queryFn: () =>
      apiClient
        .get<LimitiPrenotazioneDTO>(Endpoints.prenotazione.limitiPrenotazione)
        .then((r) => r.data),
    staleTime: Infinity,
    refetchOnWindowFocus: false,
  })
}
