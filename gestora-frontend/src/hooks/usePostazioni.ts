import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import apiClient from '@/lib/axios'
import type { PostazioneDTO, PostazioneFormDTO, RiepilogoSala } from '@/types/postazione'
import { segnalaErrore } from '@/lib/apiError'
import { toast } from 'sonner'

export function usePostazioni(zonaId: number, options?: { enabled?: boolean }) {
  return useQuery<PostazioneDTO[]>({
    queryKey: ['postazioni', zonaId],
    queryFn: () => apiClient.get(`/Postazione/get-postazioni-per-zona?zonaId=${zonaId}`).then(r => r.data),
    enabled: options?.enabled ?? true,
  })
}

export function useRiepilogoSala(options?: { enabled?: boolean }) {
  return useQuery<RiepilogoSala>({
    queryKey: ['postazioni', 'riepilogo-sala'],
    queryFn: () => apiClient.get('/Postazione/riepilogo-sala').then(r => r.data),
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
    onError: segnalaErrore('Errore durante la creazione'),
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
    onError: segnalaErrore('Errore durante l\'aggiornamento'),
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
    onError: segnalaErrore('Errore durante la cancellazione'),
  })
}