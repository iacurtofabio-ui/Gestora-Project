import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import apiClient from '@/lib/axios'
import { toast } from 'sonner'
import type {
  UserDTO,
  UpdateUserFormDTO,
  AssignRoleDTO,
  ResetPasswordDTO,
  CreateUserFormDTO,
} from '@/types/utente'
import type { AxiosError } from 'axios'
import { segnalaErrore } from '@/lib/apiError'
import type { ApiErrorResponse } from '@/types/apiError'
import { Endpoints } from '@/lib/endpoints'

export function useUtenti() {
  return useQuery<UserDTO[]>({
    queryKey: ['utenti'],
    queryFn: () => apiClient.get(Endpoints.auth.getUsers).then((r) => r.data),
  })
}

// GAP-001: il backend non ha un endpoint dedicato "crea utente con ruolo" — /register crea
// sempre un Cliente. Per l'Admin che crea un account Staff/Admin, si compone la stessa
// sequenza di chiamate già disponibili: registrazione + eventuale cambio ruolo.
/** Registrazione riuscita, cambio di ruolo no: l'utente esiste ma resta Cliente. */
class RuoloNonAssegnato extends Error {
  readonly ruolo: string
  constructor(ruolo: string) {
    super('Ruolo non assegnato')
    this.ruolo = ruolo
  }
}

export function useCreateUser() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (data: CreateUserFormDTO) => {
      const response = await apiClient.post(Endpoints.auth.register, {
        username: data.username,
        email: data.email,
        password: data.password,
      })

      if (data.role !== 'Cliente') {
        // Le chiamate non sono una sola operazione: se il ruolo fallisce l'utente esiste gia'.
        // Prima l'errore diceva "creazione non riuscita", e riprovando si otteneva "email gia' in
        // uso"; se l'utente non si trovava, il ruolo veniva saltato senza avvisare.
        try {
          const users = await apiClient.get<UserDTO[]>(Endpoints.auth.getUsers).then((r) => r.data)
          const nuovoUtente = users.find((u) => u.email.toLowerCase() === data.email.toLowerCase())
          if (!nuovoUtente) throw new Error('utente appena creato non trovato')
          await apiClient.post(Endpoints.auth.assignRole, {
            userId: nuovoUtente.id,
            role: data.role,
          })
          await apiClient.delete(Endpoints.auth.removeRole, {
            data: { userId: nuovoUtente.id, role: 'Cliente' },
          })
        } catch {
          throw new RuoloNonAssegnato(data.role)
        }
      }

      return response
    },
    onSuccess: () => {
      toast.success('Utente creato con successo')
    },
    onError: (error) => {
      if (error instanceof RuoloNonAssegnato) {
        toast.warning(
          `Utente creato, ma il ruolo ${error.ruolo} non è stato assegnato: assegnalo da «Gestisci ruoli».`
        )
        return
      }
      segnalaErrore("Errore durante la creazione dell'utente")(error as AxiosError<ApiErrorResponse>)
    },
    // Anche a meta' strada l'utente puo' essere stato creato: la lista va riletta comunque.
    onSettled: () => queryClient.invalidateQueries({ queryKey: ['utenti'] }),
  })
}

export function useUpdateUser() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateUserFormDTO }) =>
      apiClient.put(Endpoints.auth.updateUser(id), data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['utenti'] })
      toast.success('Utente aggiornato con successo')
    },
    onError: segnalaErrore("Errore durante l'aggiornamento"),
  })
}

export function useDeleteUser() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => apiClient.delete(Endpoints.auth.deleteUser(id)),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['utenti'] })
      toast.success('Utente eliminato con successo')
    },
    onError: segnalaErrore("Errore durante l'eliminazione"),
  })
}

export function useAssignRole() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (data: AssignRoleDTO) => apiClient.post(Endpoints.auth.assignRole, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['utenti'] })
      toast.success('Ruolo assegnato con successo')
    },
    onError: segnalaErrore("Errore durante l'assegnazione del ruolo"),
  })
}

export function useRemoveRole() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (data: AssignRoleDTO) => apiClient.delete(Endpoints.auth.removeRole, { data }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['utenti'] })
      toast.success('Ruolo rimosso con successo')
    },
    onError: segnalaErrore('Errore durante la rimozione del ruolo'),
  })
}

export function useResetPassword() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: ResetPasswordDTO }) =>
      apiClient.post(Endpoints.auth.resetPassword(id), data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['utenti'] })
      toast.success('Password resettata con successo')
    },
    onError: segnalaErrore('Errore durante il reset password'),
  })
}
