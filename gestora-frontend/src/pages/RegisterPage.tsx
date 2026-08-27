import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'
import { useNavigate, Link } from 'react-router-dom'
import apiClient from '@/lib/axios'
import { Button } from '@/components/ui/button'

// GAP-001: registrazione pubblica per i clienti — assegna sempre il ruolo Cliente
// (POST /register lato backend non accetta un ruolo diverso).
const schema = z.object({
  username: z.string().min(1, 'Username obbligatorio'),
  email: z.string().email('Email non valida'),
  password: z.string().min(6, 'Almeno 6 caratteri'),
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
      await apiClient.post('/AuthenticationUser/register', data)
      navigate('/login')
    } catch (err) {
      const message = err && typeof err === 'object' && 'response' in err
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
            <input
              {...register('username')}
              type="text"
              placeholder="Username"
              className="w-full border rounded px-3 py-2"
            />
            {errors.username && <p className="text-red-500 text-sm mt-1">{errors.username.message}</p>}
          </div>
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
            {isSubmitting ? 'Registrazione...' : 'Registrati'}
          </Button>
        </form>
        <p className="text-sm text-gray-500 mt-4 text-center">
          Hai già un account? <Link to="/login" className="underline">Accedi</Link>
        </p>
      </div>
    </div>
  )
}
