import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'
import { useNavigate, Link } from 'react-router-dom'
import { messaggioErroreLogin } from '@/lib/apiError'
import { useAuth } from '@/hooks/useAuth'
import { paginaDiCasa } from '@/lib/navigazione'
import apiClient from '@/lib/axios'
import { Endpoints } from '@/lib/endpoints'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Logo } from '@/components/Logo'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { ErroreForm } from '@/components/PageState'

const schema = z.object({
  email: z.string().email('Email non valida'),
  password: z.string().min(1, 'Password obbligatoria'),
})

type LoginForm = z.infer<typeof schema>

export default function LoginPage() {
  const { login } = useAuth()
  const navigate = useNavigate()

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
    setError,
  } = useForm<LoginForm>({ resolver: zodResolver(schema) })

  async function onSubmit(data: LoginForm) {
    try {
      const response = await apiClient.post(Endpoints.auth.login, data)
      // REV-014: il token non si ridecodifica qui. Ci pensa il context, che e' anche l'unico
      // punto in cui la decodifica e' protetta; qui si usa il risultato gia' pronto.
      const utente = login(response.data.token)
      navigate(paginaDiCasa(utente.roles))
    } catch (errore) {
      // Un token illeggibile non e' un problema di credenziali: dirlo com'e', altrimenti si
      // manda l'utente a riprovare all'infinito una password che era giusta.
      setError('root', { message: messaggioErroreLogin(errore) })
    }
  }

  return (
    <div className="min-h-screen flex flex-col items-center justify-center gap-6 p-4">
      <Link to="/" aria-label="Torna alla pagina iniziale">
        <Logo className="text-foreground" />
      </Link>
      <Card className="w-full max-w-sm">
        <CardHeader>
          <CardTitle className="text-titolo">Accedi</CardTitle>
          {/* Era rimasto un segnaposto, "SOTTOAccedi", pubblicato cosi' com'era. */}
          <CardDescription className="text-corpo text-muted-foreground">
            Entra per vedere e gestire le prenotazioni.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-4">
            <div>
              <Label htmlFor="login-email" className="sr-only">
                Email
              </Label>
              <Input id="login-email" {...register('email')} type="email" placeholder="Email" />
              {errors.email && (
                <p className="text-nota text-destructive mt-1">{errors.email.message}</p>
              )}
            </div>
            <div>
              <Label htmlFor="login-password" className="sr-only">
                Password
              </Label>
              <Input
                id="login-password"
                {...register('password')}
                type="password"
                placeholder="Password"
              />
              {errors.password && (
                <p className="text-nota text-destructive mt-1">{errors.password.message}</p>
              )}
            </div>
            <ErroreForm messaggio={errors.root?.message} />
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting ? 'Accesso…' : 'Accedi'}
            </Button>
          </form>
          <p className="text-corpo mt-4 text-center text-muted-foreground">
            Non hai un account?{' '}
            <Link to="/register" className="underline underline-offset-4 hover:text-foreground">
              Registrati
            </Link>
          </p>
        </CardContent>
      </Card>
      <Link to="/" className="text-nota text-muted-foreground hover:text-foreground">
        ← Torna alla pagina del locale
      </Link>
    </div>
  )
}
