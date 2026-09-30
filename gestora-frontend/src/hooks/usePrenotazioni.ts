import {
  useQuery,
  useMutation,
  useQueryClient,
  keepPreviousData,
  type QueryClient,
} from '@tanstack/react-query'
import apiClient from '@/lib/axios'
import { toast } from 'sonner'
import type { PrenotazioneDTO, PrenotazioneCreateDTO } from '@/types/prenotazione'
import { segnalaErrore } from '@/lib/apiError'
import { useAuth } from '@/hooks/useAuth'
import { Endpoints } from '@/lib/endpoints'

type PrenotazioniParams = {
  data?: string
  stato?: string
  /** FASE 7: filtro per fascia, usato dal clic su una riga della dashboard. */
  fasciaOrariaId?: number
  page?: number
  pageSize?: number
}

/** Forma della risposta paginata del backend (PagedResult<T>). */
type PrenotazioniPaginatedResponse = {
  items: PrenotazioneDTO[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}

/**
 * REV-043 — pagina di prenotazioni, con i suoi metadati.
 *
 * Prima l'hook chiedeva `pageSize: 100` fisso e teneva solo `.items`, buttando via totalCount e
 * totalPages che il backend restituisce gia'. Il risultato: superate le 100 prenotazioni le righe
 * successive semplicemente non esistevano per l'interfaccia, senza alcun avviso - il caso peggiore,
 * perche' i dati mancanti non si vedono. In un locale reale si arriva a 100 prenotazioni in
 * qualche settimana.
 *
 * Il Cliente usa un endpoint diverso, non paginato (get-mie-prenotazioni, che restituisce solo le
 * proprie e sono poche): per non costringere la pagina a due rami diversi, la sua risposta viene
 * riportata alla stessa forma, come un'unica pagina che contiene tutto.
 */
export function usePrenotazioni(params: PrenotazioniParams = {}) {
  const { user } = useAuth()
  const isStaff = user?.roles.includes('Admin') || user?.roles.includes('Staff')

  return useQuery<PrenotazioniPaginatedResponse>({
    queryKey: ['prenotazioni', isStaff, params],
    queryFn: () =>
      isStaff
        ? apiClient
            .get<PrenotazioniPaginatedResponse>(Endpoints.prenotazione.getAll, { params })
            .then((r) => r.data)
        : apiClient.get<PrenotazioneDTO[]>(Endpoints.prenotazione.mie).then((r) => ({
            items: r.data,
            totalCount: r.data.length,
            page: 1,
            pageSize: r.data.length,
            totalPages: 1,
          })),
    // Cambiando pagina si tengono a video i dati precedenti finche' arrivano i nuovi: senza,
    // la tabella si svuota e l'intestazione salta a ogni clic su Successiva.
    placeholderData: keepPreviousData,
  })
}

/**
 * FASE 7: ogni scrittura su una prenotazione cambia anche i numeri della dashboard (coperti,
 * conteggi, no-show). Prima solo 'prenotazioni' veniva invalidata: la dashboard restava con i
 * dati vecchi finche' non scattava da sola il refetchInterval, fino a un minuto di ritardo.
 */
function invalidaPrenotazioniEDashboard(queryClient: QueryClient) {
  queryClient.invalidateQueries({ queryKey: ['prenotazioni'] })
  queryClient.invalidateQueries({ queryKey: ['dashboard-giornaliera'] })
  queryClient.invalidateQueries({ queryKey: ['dashboard-settimanale'] })
}

export function useCreaPrenotazione() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (data: PrenotazioneCreateDTO) => apiClient.post(Endpoints.prenotazione.crea, data),
    onSuccess: () => {
      invalidaPrenotazioniEDashboard(queryClient)
      toast.success('Prenotazione creata con successo')
    },
    onError: segnalaErrore('Errore durante la creazione'),
  })
}

/**
 * NEW-001 — la modifica esisteva solo lato backend: nessun hook, nessun pulsante, quindi il fix
 * REV-002 (Admin e Staff modificano la prenotazione di un cliente) non era raggiungibile
 * dall'applicazione. Il corpo e' lo stesso PrenotazioneCreateDTO della creazione, l'id viaggia in
 * query string come negli altri endpoint di questo controller.
 */
export function useModificaPrenotazione() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, data }: { id: number; data: PrenotazioneCreateDTO }) =>
      apiClient.put(Endpoints.prenotazione.update(id), data),
    onSuccess: () => {
      invalidaPrenotazioniEDashboard(queryClient)
      toast.success('Prenotazione modificata con successo')
    },
    onError: segnalaErrore('Errore durante la modifica'),
  })
}

export function useConfermaPrenotazione() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: number) => apiClient.patch(Endpoints.prenotazione.conferma(id)),
    onSuccess: () => {
      invalidaPrenotazioniEDashboard(queryClient)
      toast.success('Prenotazione confermata')
    },
    onError: segnalaErrore('Errore durante la conferma'),
  })
}

export function useCompletaPrenotazione() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: number) => apiClient.patch(Endpoints.prenotazione.completa(id)),
    onSuccess: () => {
      invalidaPrenotazioniEDashboard(queryClient)
      toast.success('Prenotazione completata')
    },
    onError: segnalaErrore('Errore durante il completamento'),
  })
}

export function useAnnullaPrenotazione() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: number) => apiClient.patch(Endpoints.prenotazione.annulla(id)),
    onSuccess: () => {
      invalidaPrenotazioniEDashboard(queryClient)
      toast.success('Prenotazione annullata')
    },
    onError: segnalaErrore("Errore durante l'annullamento"),
  })
}

export function useDeletePrenotazione() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: number) => apiClient.delete(Endpoints.prenotazione.delete(id)),
    onSuccess: () => {
      invalidaPrenotazioniEDashboard(queryClient)
      toast.success('Prenotazione eliminata')
    },
    onError: segnalaErrore("Errore durante l'eliminazione"),
  })
}
