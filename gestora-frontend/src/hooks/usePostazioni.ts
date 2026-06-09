import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import apiClient from '@/lib/axios'
import type { PostazioneDTO, PostazioneFormDTO } from '@/types/postazione'
import { toast } from 'sonner'

export function usePostazioni(zonaId: number, options?: { enabled?: boolean }) {
  return useQuery<PostazioneDTO[]>({
    queryKey: ['postazioni', zonaId],
    queryFn: () => apiClient.get(`/Postazione/get-postazioni-per-zona?zonaId=${zonaId}`).then(r => r.data),
    enabled: options?.enabled ?? true,
  })
}

export function useCreaPostazione() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (data: PostazioneFormDTO) => apiClient.post('/Postazione/crea-postazione', data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['postazioni'] })
      toast.success('Postazione creata con successo')
    },
    onError: (error: any) => {
      const data = error?.response?.data
      const errors: { field: string; error: string }[] = data?.errors ?? []
      const msg = errors.length > 0
        ? errors.map((e) => e.error).join(', ')
        : (data?.message ?? 'Errore durante la creazione')
      toast.error(msg)
    },
  })
}

export function useUpdatePostazione() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (data: PostazioneDTO) => apiClient.put('/Postazione/update-postazione', data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['postazioni'] })
      toast.success('Postazione aggiornata con successo')
    },
    onError: (error: any) => {
      const data = error?.response?.data
      const errors: { field: string; error: string }[] = data?.errors ?? []
      const msg = errors.length > 0
        ? errors.map((e) => e.error).join(', ')
        : (data?.message ?? 'Errore durante l\'aggiornamento')
      toast.error(msg)
    },
  })
}

export function useDeletePostazione() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: number) => apiClient.delete(`/Postazione/delete-postazione?id=${id}`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['postazioni'] })
      toast.success('Postazione eliminata con successo')
    },
    onError: (error: any) => {
      const data = error?.response?.data
      const errors: { field: string; error: string }[] = data?.errors ?? []
      const msg = errors.length > 0
        ? errors.map((e) => e.error).join(', ')
        : (data?.message ?? 'Errore durante la cancellazione')
      toast.error(msg)
    }
  })
}