import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import apiClient from '@/lib/axios'
import { toast } from 'sonner'
import type { FasciaOrariaDTO, FasciaOrariaFormDTO } from '@/types/fasciaOraria'
import { segnalaErrore } from '@/lib/apiError'
import { Endpoints } from '@/lib/endpoints'

export function useFasceOrarie() {
  return useQuery<FasciaOrariaDTO[]>({
    queryKey: ['fasce-orarie'],
    queryFn: () => apiClient.get(Endpoints.fasciaOraria.attive).then((r) => r.data),
  })
}

export function useAllFasceOrarie() {
  return useQuery<FasciaOrariaDTO[]>({
    queryKey: ['fasce-orarie', 'all'],
    queryFn: () => apiClient.get(Endpoints.fasciaOraria.getAll).then((r) => r.data),
  })
}

export function useFascePerGiorno(giorno: number | undefined) {
  return useQuery<FasciaOrariaDTO[]>({
    queryKey: ['fasce-orarie', 'per-giorno', giorno],
    queryFn: () => apiClient.get(Endpoints.fasciaOraria.perGiorno(giorno!)).then((r) => r.data),
    enabled: giorno !== undefined,
  })
}

export function useCreaFasciaOraria() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (data: FasciaOrariaFormDTO) => apiClient.post(Endpoints.fasciaOraria.crea, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['fasce-orarie'] })
      toast.success('Fascia oraria creata con successo')
    },
    onError: segnalaErrore('Errore durante la creazione'),
  })
}

export function useUpdateFasciaOraria() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (data: FasciaOrariaDTO) => apiClient.put(Endpoints.fasciaOraria.update, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['fasce-orarie'] })
      toast.success('Fascia oraria aggiornata con successo')
    },
    onError: segnalaErrore("Errore durante l'aggiornamento"),
  })
}

export function useDeleteFasciaOraria() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: number) => apiClient.delete(Endpoints.fasciaOraria.delete(id)),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['fasce-orarie'] })
      toast.success('Fascia oraria eliminata con successo')
    },
    onError: segnalaErrore("Errore durante l'eliminazione"),
  })
}
