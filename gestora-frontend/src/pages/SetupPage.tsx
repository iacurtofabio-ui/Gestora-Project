import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'
import { Navigate, useNavigate } from 'react-router-dom'
import type { AxiosError } from 'axios'
import { Button } from '@/components/ui/button'
import { useSetupStato, useCreaPrimoAdmin } from '@/hooks/useSetup'
import type { ApiErrorResponse } from '@/types/apiError'

// Le regole ricalcano RegisterDTOValidator lato backend: qui servono a dare l'errore subito,
// la validazione che conta resta quella del server.
const schema = z.object({
  username: z.string().min(3, 'Almeno 3 caratteri').max(50, 'Massimo 50 caratteri'),
  email: z.string().email('Email non valida'),
  password: z
    .string()
    .min(8, 'Almeno 8 caratteri')
    .regex(/[A-Z]/, 'Serve almeno una lettera maiuscola')
    .regex(/[0-9]/, 'Serve almeno un numero')
    .regex(/[^a-zA-Z0-9]/, 'Serve almeno un carattere speciale'),
})

type SetupForm = z.infer<typeof schema>

/**
 * REV-007 — schermata di primo avvio. Sostituisce l'endpoint pubblico seed-admin: è il primo
 * e unico passo di configurazione di una nuova installazione di Gestora in un locale.
 * Creato l'amministratore, la pagina si chiude da sola e si passa al login.
 */
export default function SetupPage() {
  const navigate = useNavigate()
  const { data, isPending: statoInCaricamento } = useSetupStato()
  const creaPrimoAdmin = useCreaPrimoAdmin()

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
    setError,
  } = useForm<SetupForm>({ resolver: zodResolver(schema) })

  async function onSubmit(form: SetupForm) {
    try {
      await creaPrimoAdmin.mutateAsync(form)
      // Nessun login automatico: si entra con le credenziali appena scelte, così si
      // verificano subito invece di scoprire un errore di digitazione il giorno dopo.
      navigate('/login', { replace: true })
    } catch (err) {
      const payload = (err as AxiosError<ApiErrorResponse>).response?.data
      const dettagli = payload?.errors ?? []
      setError('root', {
        message:
          dettagli.length > 0
            ? dettagli.map((e) => e.error).join(', ')
            : (payload?.message ?? 'Creazione dell’amministratore non riuscita. Riprova.'),
      })
    }
  }

  if (statoInCaricamento) return null

  // L'installazione è già configurata: la pagina non ha più ragione di esistere.
  if (data?.setupCompletato) return <Navigate to="/login" replace />

  return (
    <div className="min-h-screen flex items-center justify-center">
      <div className="w-full max-w-md p-8 border rounded-lg shadow-sm">
        <h1 className="text-2xl font-bold mb-2">Benvenuto in Gestora</h1>
        <p className="text-sm text-gray-500 mb-6">
          Questa installazione non è ancora configurata. Crea l’utenza dell’amministratore:
          sarà l’unico account con cui gestire zone, tavoli, fasce orarie e gli altri utenti.
        </p>
        <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-4">
          <div>
            <label className="block text-sm font-medium mb-1">Nome utente</label>
            <input
              {...register('username')}
              type="text"
              autoComplete="username"
              className="w-full border rounded px-3 py-2"
            />
            {errors.username && <p className="text-red-500 text-sm mt-1">{errors.username.message}</p>}
          </div>
          <div>
            <label className="block text-sm font-medium mb-1">Email</label>
            <input {...register('email')} type="email" className="w-full border rounded px-3 py-2" />
            {errors.email && <p className="text-red-500 text-sm mt-1">{errors.email.message}</p>}
          </div>
          <div>
            <label className="block text-sm font-medium mb-1">Password</label>
            <input
              {...register('password')}
              type="password"
              autoComplete="new-password"
              className="w-full border rounded px-3 py-2"
            />
            {errors.password ? (
              <p className="text-red-500 text-sm mt-1">{errors.password.message}</p>
            ) : (
              <p className="text-gray-500 text-xs mt-1">
                Almeno 8 caratteri, con una maiuscola, un numero e un carattere speciale.
              </p>
            )}
          </div>
          {errors.root && <p className="text-red-500 text-sm">{errors.root.message}</p>}
          <Button type="submit" disabled={isSubmitting}>
            {isSubmitting ? 'Creazione in corso...' : 'Crea amministratore'}
          </Button>
        </form>
      </div>
    </div>
  )
}
