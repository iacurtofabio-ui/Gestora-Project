import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'
import { emailSchema, passwordSchema, usernameSchema } from '@/lib/validazioni'
import { Navigate, useNavigate } from 'react-router-dom'
import type { AxiosError } from 'axios'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { ErroreForm } from '@/components/PageState'
import { useSetupStato, useCreaPrimoAdmin } from '@/hooks/useSetup'
import type { ApiErrorResponse } from '@/types/apiError'

// Le regole ricalcano RegisterDTOValidator lato backend: qui servono a dare l'errore subito,
// la validazione che conta resta quella del server.
const schema = z.object({
  username: usernameSchema,
  email: emailSchema,
  password: passwordSchema,
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
    <div className="flex min-h-screen items-center justify-center bg-background p-4">
      <div className="w-full max-w-md rounded-2xl border bg-card p-8">
        <h1 className="text-titolo mb-2">Benvenuto in Gestora</h1>
        <p className="text-corpo mb-6 text-muted-foreground text-pretty">
          Questa installazione non è ancora configurata. Crea l’utenza dell’amministratore: sarà
          l’unico account con cui gestire zone, tavoli, fasce orarie e gli altri utenti.
        </p>
        <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-4">
          <div>
            <label htmlFor="setup-username" className="text-corpo mb-1 block font-medium">
              Nome utente
            </label>
            <Input
              id="setup-username"
              {...register('username')}
              type="text"
              autoComplete="username"
            />
            {errors.username && (
              <p className="text-nota text-destructive mt-1">{errors.username.message}</p>
            )}
          </div>
          <div>
            <label htmlFor="setup-email" className="text-corpo mb-1 block font-medium">
              Email
            </label>
            <Input id="setup-email" {...register('email')} type="email" />
            {errors.email && (
              <p className="text-nota text-destructive mt-1">{errors.email.message}</p>
            )}
          </div>
          <div>
            <label htmlFor="setup-password" className="text-corpo mb-1 block font-medium">
              Password
            </label>
            <Input
              id="setup-password"
              {...register('password')}
              type="password"
              autoComplete="new-password"
            />
            {errors.password ? (
              <p className="text-nota text-destructive mt-1">{errors.password.message}</p>
            ) : (
              <p className="text-nota text-muted-foreground mt-1">
                Almeno 8 caratteri, con una maiuscola, un numero e un carattere speciale.
              </p>
            )}
          </div>
          <ErroreForm messaggio={errors.root?.message} />
          <Button type="submit" disabled={isSubmitting}>
            {isSubmitting ? 'Creazione…' : 'Crea amministratore'}
          </Button>
        </form>
      </div>
    </div>
  )
}
