import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import type { AxiosError } from 'axios'
import apiClient from '@/lib/axios'
import { toast } from 'sonner'
import type { FasciaOrariaDTO, FasciaOrariaFormDTO } from '@/types/fasciaOraria'
import type { ApiErrorResponse } from '@/types/apiError'



export function useFasceOrarie() {
    return useQuery<FasciaOrariaDTO[]>({
        queryKey: ['fasce-orarie'],
        queryFn: () => apiClient.get('/FasceOrarie/fasce-attive').then(r => r.data),
    })
}

export function useCreaFasciaOraria() {
    const queryClient = useQueryClient()
    return useMutation({
        mutationFn: (data: FasciaOrariaFormDTO) => apiClient.post('/FasceOrarie/crea-fascia', data),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['fasce-orarie'] })
            toast.success('Fascia oraria creata con successo')
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

export function useUpdateFasciaOraria() {
    const queryClient = useQueryClient()
    return useMutation({
        mutationFn: (data: FasciaOrariaDTO) => apiClient.put('/FasceOrarie/update-fascia', data),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['fasce-orarie'] })
            toast.success('Fascia oraria aggiornata con successo')
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

export function useDeleteFasciaOraria() {
    const queryClient = useQueryClient()
    return useMutation({
        mutationFn: (id: number) => apiClient.delete(`/FasceOrarie/delete-fascia?id=${id}`),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['fasce-orarie'] })
            toast.success('Fascia oraria eliminata con successo')
        },
        onError: (error: AxiosError<ApiErrorResponse>) => {
            const data = error.response?.data
            const errors = data?.errors ?? []
            const msg = errors.length > 0
                ? errors.map((e) => e.error).join(', ')
                : (data?.message ?? 'Errore durante l\'eliminazione')
            toast.error(msg)
        },
    })
}