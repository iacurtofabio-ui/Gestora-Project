import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'
import { useNavigate, Link } from 'react-router-dom'
import apiClient from '@/lib/axios'
import { Endpoints } from '@/lib/endpoints'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Logo } from '@/components/Logo'
import { Input } from '@/components/ui/input'
import { ErroreForm } from '@/components/PageState'
import { Label } from '@/components/ui/label'
import { emailSchema, passwordSchema, usernameSchema } from '@/lib/validazioni'

// GAP-001: registrazione pubblica per i clienti — assegna sempre il ruolo Cliente
// (POST /register lato backend non accetta un ruolo diverso).
const schema = z.object({
  username: usernameSchema,
  email: emailSchema,
  password: passwordSchema,
})

type RegisterForm = z.infer<typeof schema>

export default function RegisterPage() {
  const navigate = useNavigate()

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
    setError,
  } = useForm<RegisterForm>({ resolver: zodResolver(schema) })

  async function onSubmit(data: RegisterForm) {
    try {
      await apiClient.post(Endpoints.auth.register, data)
      navigate('/login')
    } catch (err) {
      const message =
        err && typeof err === 'object' && 'response' in err
          ? (err as { response?: { data?: { message?: string } } }).response?.data?.message
          : undefined
      setError('root', { message: message ?? 'Registrazione non riuscita. Riprova.' })
    }
  }

  return (
    <div className="min-h-screen flex flex-col items-center justify-center gap-6 p-4">
      <Link to="/" aria-label="Torna alla pagina iniziale">
        <Logo className="text-foreground" />
      </Link>
      <Card className="w-full max-w-sm">
        <CardHeader>
          <CardTitle className="text-titolo">Crea il tuo account</CardTitle>
          <CardDescription>SOTTOCrea il tuo account</CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-4">
            <div>
              <Label htmlFor="register-username" className="sr-only">
                Username
              </Label>
              <Input
                id="register-username"
                {...register('username')}
                type="text"
                placeholder="Username"
              />
              {errors.username && (
                <p className="text-nota text-destructive mt-1">{errors.username.message}</p>
              )}
            </div>
            <div>
              <Label htmlFor="register-email" className="sr-only">
                Email
              </Label>
              <Input id="register-email" {...register('email')} type="email" placeholder="Email" />
              {errors.email && (
                <p className="text-nota text-destructive mt-1">{errors.email.message}</p>
              )}
            </div>
            <div>
              <Label htmlFor="register-password" className="sr-only">
                Password
              </Label>
              <Input
                id="register-password"
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
              {isSubmitting ? 'Creazione…' : 'Crea account'}
            </Button>
          </form>
          <p className="text-corpo mt-4 text-center text-muted-foreground">
            Hai già un account?{' '}
            <Link to="/login" className="underline underline-offset-4 hover:text-foreground">
              Accedi
            </Link>
          </p>
        </CardContent>
      </Card>
    </div>
  )
}
