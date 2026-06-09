import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import apiClient from '@/lib/axios'
import { toast } from 'sonner'
import type { PrenotazioneDTO, PrenotazioneCreateDTO } from '@/types/prenotazione'

type PrenotazioniParams = {
    data?: string
    stato?: string
    page?: number
    pageSize?: number
}

type PrenotazioniPaginatedResponse = {
    items: PrenotazioneDTO[]
    totalCount: number
    page: number
    pageSize: number
    totalPages: number
}

export function usePrenotazioni(params: PrenotazioniParams = {}) {
    return useQuery<PrenotazioneDTO[]>({
        queryKey: ['prenotazioni', params],
        queryFn: () =>
            apiClient
                .get<PrenotazioniPaginatedResponse>('/Prenotazione/get-all-prenotazioni', { params })
                .then(r => r.data.items),
    })
}

export function useCreaPrenotazione() {
    const queryClient = useQueryClient()
    return useMutation({
        mutationFn: (data: PrenotazioneCreateDTO) => apiClient.post('/Prenotazione/crea-prenotazione', data),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['prenotazioni'] })
            toast.success('Prenotazione creata con successo')
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

export function useConfermaPrenotazione() {
    const queryClient = useQueryClient()
    return useMutation({
        mutationFn: (id: number) => apiClient.patch(`/Prenotazione/conferma-prenotazione?id=${id}`),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['prenotazioni'] })
            toast.success('Prenotazione confermata')
        },
        onError: (error: any) => {
            const data = error?.response?.data
            const errors: { field: string; error: string }[] = data?.errors ?? []
            const msg = errors.length > 0
                ? errors.map((e) => e.error).join(', ')
                : (data?.message ?? 'Errore durante la conferma')
            toast.error(msg)
        },
    })
}

export function useCompletaPrenotazione() {
    const queryClient = useQueryClient()
    return useMutation({
        mutationFn: (id: number) => apiClient.patch(`/Prenotazione/completa-prenotazione?id=${id}`),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['prenotazioni'] })
            toast.success('Prenotazione completata')
        },
        onError: (error: any) => {
            const data = error?.response?.data
            const errors: { field: string; error: string }[] = data?.errors ?? []
            const msg = errors.length > 0
                ? errors.map((e) => e.error).join(', ')
                : (data?.message ?? 'Errore durante il completamento')
            toast.error(msg)
        },
    })
}

export function useAnnullaPrenotazione() {
    const queryClient = useQueryClient()
    return useMutation({
        mutationFn: (id: number) => apiClient.patch(`/Prenotazione/annulla-prenotazione?id=${id}`),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['prenotazioni'] })
            toast.success('Prenotazione annullata')
        },
        onError: (error: any) => {
            const data = error?.response?.data
            const errors: { field: string; error: string }[] = data?.errors ?? []
            const msg = errors.length > 0
                ? errors.map((e) => e.error).join(', ')
                : (data?.message ?? 'Errore durante l\'annullamento')
            toast.error(msg)
        },
    })
}

export function useDeletePrenotazione() {
    const queryClient = useQueryClient()
    return useMutation({
        mutationFn: (id: number) => apiClient.delete(`/Prenotazione/delete-prenotazione?id=${id}`),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['prenotazioni'] })
            toast.success('Prenotazione eliminata')
        },
        onError: (error: any) => {
            const data = error?.response?.data
            const errors: { field: string; error: string }[] = data?.errors ?? []
            const msg = errors.length > 0
                ? errors.map((e) => e.error).join(', ')
                : (data?.message ?? 'Errore durante l\'eliminazione')
            toast.error(msg)
        },
    })
}