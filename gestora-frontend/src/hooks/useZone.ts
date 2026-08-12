import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import type { AxiosError } from 'axios'
import apiClient from '@/lib/axios'
import type { ZonaDTO, ZonaFormDTO } from '@/types/zona'
import type { ApiErrorResponse } from '@/types/apiError'
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
    onError: (error: AxiosError<ApiErrorResponse>) => {
      const data = error.response?.data
      const errors = data?.errors ?? []
      const msg = errors.length > 0
        ? errors.map((e) => e.error).join(', ')
        : (data?.message ?? 'Errore durante la creazione')
      toast.error(msg)
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
    onError: (error: AxiosError<ApiErrorResponse>) => {
      const data = error.response?.data
      const errors = data?.errors ?? []
      const msg = errors.length > 0
        ? errors.map((e) => e.error).join(', ')
        : (data?.message ?? 'Errore durante l\'aggiornamento')
      toast.error(msg)
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
    onError: (error: AxiosError<ApiErrorResponse>) => {
      const data = error.response?.data
      const errors = data?.errors ?? []
      const msg = errors.length > 0
        ? errors.map((e) => e.error).join(', ')
        : (data?.message ?? 'Errore durante la cancellazione')
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
    onError: (error: AxiosError<ApiErrorResponse>) => {
      const data = error.response?.data
      const errors = data?.errors ?? []
      const msg = errors.length > 0
        ? errors.map((e) => e.error).join(', ')
        : (data?.message ?? 'Errore durante l\'aggiornamento stato')
      toast.error(msg)
    },
  })
}
