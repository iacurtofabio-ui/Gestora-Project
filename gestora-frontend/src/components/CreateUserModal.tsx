import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { useCreateUser } from '@/hooks/useAdminUtenti'
import { RUOLI_DISPONIBILI } from '@/types/utente'
import type { CreateUserFormDTO } from '@/types/utente'

type Props = {
  open: boolean
  onClose: () => void
}

const schema = z.object({
  username: z.string().min(1, 'Username obbligatorio'),
  email: z.string().email('Email non valida'),
  password: z.string().min(6, 'Almeno 6 caratteri'),
  role: z.enum(RUOLI_DISPONIBILI),
})

export default function CreateUserModal({ open, onClose }: Props) {
  const createUser = useCreateUser()
  const { register, handleSubmit, reset, formState: { errors } } = useForm<CreateUserFormDTO>({
    resolver: zodResolver(schema),
    defaultValues: { username: '', email: '', password: '', role: 'Cliente' },
  })

  const onSubmit = (data: CreateUserFormDTO) => {
    createUser.mutate(data, {
      onSuccess: () => { reset(); onClose() },
    })
  }

  return (
    <Dialog open={open} onOpenChange={onClose}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Crea utente</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4 mt-2">
          <div className="space-y-1">
            <Label>Username</Label>
            <Input {...register('username')} />
            {errors.username && <p className="text-red-500 text-xs">{errors.username.message}</p>}
          </div>
          <div className="space-y-1">
            <Label>Email</Label>
            <Input type="email" {...register('email')} />
            {errors.email && <p className="text-red-500 text-xs">{errors.email.message}</p>}
          </div>
          <div className="space-y-1">
            <Label>Password</Label>
            <Input type="password" {...register('password')} />
            {errors.password && <p className="text-red-500 text-xs">{errors.password.message}</p>}
          </div>
          <div className="space-y-1">
            <Label>Ruolo</Label>
            <select {...register('role')} className="border rounded px-3 py-2 w-full text-sm">
              {RUOLI_DISPONIBILI.map((r) => (
                <option key={r} value={r}>{r}</option>
              ))}
            </select>
          </div>
          <div className="flex justify-end gap-2">
            <Button type="button" variant="outline" onClick={onClose}>Annulla</Button>
            <Button type="submit" disabled={createUser.isPending}>Crea</Button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  )
}
