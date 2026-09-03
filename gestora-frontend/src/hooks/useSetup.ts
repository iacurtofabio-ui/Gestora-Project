import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import type { AxiosError } from 'axios'
import apiClient from '@/lib/axios'
import type { ApiErrorResponse } from '@/types/apiError'

export type SetupStato = {
  setupCompletato: boolean
}

export type PrimoAdminForm = {
  username: string
  email: string
  password: string
}

/**
 * REV-007 — dice se l'installazione ha già un amministratore. Endpoint pubblico.
 *
 * `retry: false` e `staleTime: Infinity`: la risposta cambia una volta sola nella vita
 * dell'installazione (da false a true), non ha senso rinterrogarla a ogni focus della finestra.
 * Se la chiamata fallisce (backend giù) la guardia lascia passare al login: meglio un errore
 * di login che dirottare tutti su una schermata di primo avvio per un problema di rete.
 */
export function useSetupStato() {
  return useQuery<SetupStato>({
    queryKey: ['setup', 'stato'],
    queryFn: () => apiClient.get('/Setup/stato').then((r) => r.data),
    retry: false,
    staleTime: Infinity,
    refetchOnWindowFocus: false,
  })
}

export function useCreaPrimoAdmin() {
  const queryClient = useQueryClient()
  return useMutation<unknown, AxiosError<ApiErrorResponse>, PrimoAdminForm>({
    mutationFn: (data: PrimoAdminForm) => apiClient.post('/Setup/admin', data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['setup'] })
    },
  })
}
