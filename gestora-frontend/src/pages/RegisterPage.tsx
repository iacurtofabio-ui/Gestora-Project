import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'
import { useNavigate, Link } from 'react-router-dom'
import apiClient from '@/lib/axios'
import { Endpoints } from '@/lib/endpoints'
import { Button } from '@/components/ui/button'
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
    <div className="min-h-screen flex items-center justify-center">
      <div className="w-full max-w-sm p-8 border rounded-lg shadow-sm">
        <h1 className="text-2xl font-bold mb-6">Registrati</h1>
        <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-4">
          <div>
            <Label htmlFor="register-username" className="sr-only">
              Username
            </Label>
            <input
              id="register-username"
              {...register('username')}
              type="text"
              placeholder="Username"
              className="w-full border rounded px-3 py-2"
            />
            {errors.username && (
              <p className="text-red-500 text-sm mt-1">{errors.username.message}</p>
            )}
          </div>
          <div>
            <Label htmlFor="register-email" className="sr-only">
              Email
            </Label>
            <input
              id="register-email"
              {...register('email')}
              type="email"
              placeholder="Email"
              className="w-full border rounded px-3 py-2"
            />
            {errors.email && <p className="text-red-500 text-sm mt-1">{errors.email.message}</p>}
          </div>
          <div>
            <Label htmlFor="register-password" className="sr-only">
              Password
            </Label>
            <input
              id="register-password"
              {...register('password')}
              type="password"
              placeholder="Password"
              className="w-full border rounded px-3 py-2"
            />
            {errors.password && (
              <p className="text-red-500 text-sm mt-1">{errors.password.message}</p>
            )}
          </div>
          {errors.root && <p className="text-red-500 text-sm">{errors.root.message}</p>}
          <Button type="submit" disabled={isSubmitting}>
            {isSubmitting ? 'Registrazione...' : 'Registrati'}
          </Button>
        </form>
        <p className="text-sm text-gray-500 mt-4 text-center">
          Hai già un account?{' '}
          <Link to="/login" className="underline">
            Accedi
          </Link>
        </p>
      </div>
    </div>
  )
}
