import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import type { AxiosError } from 'axios'
import apiClient from '@/lib/axios'
import { toast } from 'sonner'
import type { UserDTO, UpdateUserFormDTO, AssignRoleDTO, ResetPasswordDTO, CreateUserFormDTO } from '@/types/utente'
import type { ApiErrorResponse } from '@/types/apiError'

export function useUtenti() {
  return useQuery<UserDTO[]>({
    queryKey: ['utenti'],
    queryFn: () => apiClient.get('/AuthenticationUser/get-users').then(r => r.data),
  })
}

// GAP-001: il backend non ha un endpoint dedicato "crea utente con ruolo" — /register crea
// sempre un Cliente. Per l'Admin che crea un account Staff/Admin, si compone la stessa
// sequenza di chiamate già disponibili: registrazione + eventuale cambio ruolo.
export function useCreateUser() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (data: CreateUserFormDTO) => {
      const response = await apiClient.post('/AuthenticationUser/register', {
        username: data.username,
        email: data.email,
        password: data.password,
      })

      if (data.role !== 'Cliente') {
        const users = await apiClient.get<UserDTO[]>('/AuthenticationUser/get-users').then(r => r.data)
        const nuovoUtente = users.find(u => u.email === data.email)
        if (nuovoUtente) {
          await apiClient.post('/AuthenticationUser/assign-role', { userId: nuovoUtente.id, role: data.role })
          await apiClient.delete('/AuthenticationUser/remove-role', { data: { userId: nuovoUtente.id, role: 'Cliente' } })
        }
      }

      return response
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['utenti'] })
      toast.success('Utente creato con successo')
    },
    onError: (error: AxiosError<ApiErrorResponse>) => {
      const data = error.response?.data
      const errors = data?.errors ?? []
      const msg = errors.length > 0
        ? errors.map((e) => e.error).join(', ')
        : (data?.message ?? 'Errore durante la creazione dell\'utente')
      toast.error(msg)
    },
  })
}

export function useUpdateUser() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateUserFormDTO }) =>
      apiClient.put(`/AuthenticationUser/update-user/${id}`, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['utenti'] })
      toast.success('Utente aggiornato con successo')
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

export function useDeleteUser() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => apiClient.delete(`/AuthenticationUser/delete-user/${id}`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['utenti'] })
      toast.success('Utente eliminato con successo')
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

export function useAssignRole() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (data: AssignRoleDTO) => apiClient.post('/AuthenticationUser/assign-role', data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['utenti'] })
      toast.success('Ruolo assegnato con successo')
    },
    onError: (error: AxiosError<ApiErrorResponse>) => {
      const data = error.response?.data
      const errors = data?.errors ?? []
      const msg = errors.length > 0
        ? errors.map((e) => e.error).join(', ')
        : (data?.message ?? 'Errore durante l\'assegnazione del ruolo')
      toast.error(msg)
    },
  })
}

export function useRemoveRole() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (data: AssignRoleDTO) => apiClient.delete('/AuthenticationUser/remove-role', { data }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['utenti'] })
      toast.success('Ruolo rimosso con successo')
    },
    onError: (error: AxiosError<ApiErrorResponse>) => {
      const data = error.response?.data
      const errors = data?.errors ?? []
      const msg = errors.length > 0
        ? errors.map((e) => e.error).join(', ')
        : (data?.message ?? 'Errore durante la rimozione del ruolo')
      toast.error(msg)
    },
  })
}

export function useResetPassword() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: ResetPasswordDTO }) =>
      apiClient.post(`/AuthenticationUser/reset-password/${id}`, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['utenti'] })
      toast.success('Password resettata con successo')
    },
    onError: (error: AxiosError<ApiErrorResponse>) => {
      const data = error.response?.data
      const errors = data?.errors ?? []
      const msg = errors.length > 0
        ? errors.map((e) => e.error).join(', ')
        : (data?.message ?? 'Errore durante il reset password')
      toast.error(msg)
    },
  })
}