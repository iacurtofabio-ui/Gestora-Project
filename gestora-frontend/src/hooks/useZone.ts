import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import apiClient from '@/lib/axios'
import type { ZonaDTO, ZonaFormDTO } from '@/types/zona'
import { toast } from 'sonner'

export function useZone() {
  return useQuery<ZonaDTO[]>({
    queryKey: ['zone'],
    queryFn: () => apiClient.get('/Zona/get-all-zone').then(r => r.data),
  })
}

export function useCreaZona() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (data: ZonaFormDTO) => apiClient.post('/Zona/crea-zona', data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['zone'] })
      toast.success('Zona creata con successo')
    },
  })
}

export function useUpdateZona() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (data: ZonaDTO) => apiClient.put('/Zona/update-zona', data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['zone'] })
      toast.success('Zona aggiornata con successo')
    },
  })
}

export function useDeleteZona() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: number) => apiClient.delete(`/Zona/delete-zona/${id}`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['zone'] })
      toast.success('Zona eliminata con successo')
    },
    onError: (error: any) => {
      const msg = error?.response?.data?.message ?? 'Errore durante l\'eliminazione'
      toast.error(msg)
    }
  })
}

export function useUpdateStatoZona() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, attiva }: { id: number; attiva: boolean }) =>
      apiClient.patch(`/Zona/update-stato/${id}?attiva=${attiva}`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['zone'] })
      toast.success('Stato della zona aggiornato con successo')
    },
  })
}
