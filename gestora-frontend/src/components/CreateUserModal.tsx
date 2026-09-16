import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { NativeSelect } from '@/components/ui/native-select'
import { useCreateUser } from '@/hooks/useAdminUtenti'
import { RUOLI_DISPONIBILI } from '@/types/utente'
import { emailSchema, passwordSchema, usernameSchema } from '@/lib/validazioni'
import type { CreateUserFormDTO } from '@/types/utente'

type Props = {
  open: boolean
  onClose: () => void
}

const schema = z.object({
  username: usernameSchema,
  email: emailSchema,
  password: passwordSchema,
  role: z.enum(RUOLI_DISPONIBILI),
})

export default function CreateUserModal({ open, onClose }: Props) {
  const createUser = useCreateUser()
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<CreateUserFormDTO>({
    resolver: zodResolver(schema),
    defaultValues: { username: '', email: '', password: '', role: 'Cliente' },
  })

  const onSubmit = (data: CreateUserFormDTO) => {
    createUser.mutate(data, {
      onSuccess: () => {
        reset()
        onClose()
      },
    })
  }

  return (
    <Dialog open={open} onOpenChange={onClose}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle className="text-titolo">Crea un utente</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4 mt-2">
          <div className="space-y-1">
            <Label htmlFor="create-user-username">Nome</Label>
            <Input id="create-user-username" {...register('username')} />
            {errors.username && (
              <p className="text-nota text-destructive">{errors.username.message}</p>
            )}
          </div>
          <div className="space-y-1">
            <Label htmlFor="create-user-email">Email</Label>
            <Input id="create-user-email" type="email" {...register('email')} />
            {errors.email && <p className="text-nota text-destructive">{errors.email.message}</p>}
          </div>
          <div className="space-y-1">
            <Label htmlFor="create-user-password">Password</Label>
            <Input id="create-user-password" type="password" {...register('password')} />
            {errors.password && (
              <p className="text-nota text-destructive">{errors.password.message}</p>
            )}
          </div>
          <div className="space-y-1">
            <Label htmlFor="create-user-role">Ruolo</Label>
            <NativeSelect
              id="create-user-role"
              {...register('role')}
            >
              {RUOLI_DISPONIBILI.map((r) => (
                <option key={r} value={r}>
                  {r}
                </option>
              ))}
            </NativeSelect>
          </div>
          <div className="flex justify-end gap-2 border-t pt-4">
            <Button type="button" variant="ghost" onClick={onClose}>
              Annulla
            </Button>
            <Button type="submit" disabled={createUser.isPending}>
              {createUser.isPending ? 'Creazione…' : 'Crea utente'}
            </Button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  )
}
