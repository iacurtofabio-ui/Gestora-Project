import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import apiClient from '@/lib/axios'
import { toast } from 'sonner'
import type { FasciaOrariaDTO, FasciaOrariaFormDTO } from '@/types/fasciaOraria'
import { segnalaErrore } from '@/lib/apiError'



export function useFasceOrarie() {
    return useQuery<FasciaOrariaDTO[]>({
        queryKey: ['fasce-orarie'],
        queryFn: () => apiClient.get('/FasceOrarie/fasce-attive').then(r => r.data),
    })
}

export function useAllFasceOrarie() {
    return useQuery<FasciaOrariaDTO[]>({
        queryKey: ['fasce-orarie', 'all'],
        queryFn: () => apiClient.get('/FasceOrarie/get-all-fasce').then(r => r.data),
    })
}

export function useFascePerGiorno(giorno: number | undefined) {
    return useQuery<FasciaOrariaDTO[]>({
        queryKey: ['fasce-orarie', 'per-giorno', giorno],
        queryFn: () => apiClient.get(`/FasceOrarie/fasce-per-giorno?giorno=${giorno}`).then(r => r.data),
        enabled: giorno !== undefined,
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
        onError: segnalaErrore('Errore durante la creazione'),
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
        onError: segnalaErrore('Errore durante l\'aggiornamento'),
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
        onError: segnalaErrore('Errore durante l\'eliminazione'),
    })
}