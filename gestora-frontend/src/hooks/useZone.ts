import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import apiClient from '@/lib/axios'
import type { ZonaDTO, ZonaFormDTO } from '@/types/zona'
import { segnalaErrore } from '@/lib/apiError'
import { toast } from 'sonner'
import { Endpoints } from '@/lib/endpoints'

export function useZone() {
  return useQuery<ZonaDTO[]>({
    queryKey: ['zone'],
    queryFn: () => apiClient.get(Endpoints.zona.getAll).then((r) => r.data),
  })
}

/**
 * Zone selezionabili in una prenotazione.
 *
 * Non si puo' usare useZone: get-all-zone e' riservato ad Admin e Staff, quindi per il Cliente
 * rispondeva 403 e la select della zona restava vuota - il campo era inutilizzabile proprio per
 * chi prenota da solo. get-zone-attive e' aperto anche al Cliente e, restituendo le sole zone
 * attive, e' anche la lista giusta: una zona disattivata non e' prenotabile (REV-024 la esclude
 * dall'assegnazione), quindi non ha senso proporla nel form.
 */
export function useZoneAttive() {
  return useQuery<ZonaDTO[]>({
    queryKey: ['zone', 'attive'],
    queryFn: () => apiClient.get(Endpoints.zona.getAttive).then((r) => r.data),
  })
}

export function useCreaZona() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (data: ZonaFormDTO) => apiClient.post(Endpoints.zona.crea, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['zone'] })
      toast.success('Zona creata con successo')
    },
    onError: segnalaErrore('Errore durante la creazione'),
  })
}

export function useUpdateZona() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (data: ZonaDTO) => apiClient.put(Endpoints.zona.update, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['zone'] })
      toast.success('Zona aggiornata con successo')
    },
    onError: segnalaErrore("Errore durante l'aggiornamento"),
  })
}

export function useDeleteZona() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: number) => apiClient.delete(Endpoints.zona.delete(id)),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['zone'] })
      toast.success('Zona eliminata con successo')
    },
    onError: segnalaErrore('Errore durante la cancellazione'),
  })
}
