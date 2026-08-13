import { useForm } from 'react-hook-form'
  import { z } from 'zod'
  import { zodResolver } from '@hookform/resolvers/zod'
  import { useNavigate } from 'react-router-dom'
  import { useAuth } from '@/hooks/useAuth'
  import apiClient from '@/lib/axios'
  import { Button } from '@/components/ui/button'

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
        const response = await apiClient.post('/AuthenticationUser/login', data)
        const token = response.data.token
        login(token)
        const payload = JSON.parse(atob(token.split('.')[1]))
        const roles: string | string[] = payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']
        const soloCliente = Array.isArray(roles) ? roles.every((r) => r === 'Cliente') : roles === 'Cliente'
        navigate(soloCliente ? '/prenotazioni' : '/dashboard')
      } catch {
        setError('root', { message: 'Credenziali non valide' })
      }
    }

    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="w-full max-w-sm p-8 border rounded-lg shadow-sm">
          <h1 className="text-2xl font-bold mb-6">Gestora</h1>
          <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-4">
            <div>
              <input
                {...register('email')}
                type="email"
                placeholder="Email"
                className="w-full border rounded px-3 py-2"
              />
              {errors.email && <p className="text-red-500 text-sm mt-1">{errors.email.message}</p>}
            </div>
            <div>
              <input
                {...register('password')}
                type="password"
                placeholder="Password"
                className="w-full border rounded px-3 py-2"
              />
              {errors.password && <p className="text-red-500 text-sm mt-1">{errors.password.message}</p>}
            </div>
            {errors.root && <p className="text-red-500 text-sm">{errors.root.message}</p>}
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting ? 'Accesso...' : 'Accedi'}
            </Button>
          </form>
        </div>
      </div>
    )
  }