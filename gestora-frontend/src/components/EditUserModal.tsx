import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { useUpdateUser } from '@/hooks/useAdminUtenti'
import { emailSchema, usernameSchema } from '@/lib/validazioni'
import type { UserDTO, UpdateUserFormDTO } from '@/types/utente'

type Props = {
  utente: UserDTO | undefined
  open: boolean
  onClose: () => void
}

// REV-048: era l'unico form di modifica senza validazione. Si poteva salvare uno username vuoto o
// un'email malformata e scoprirlo solo dalla risposta del server.
const schema = z.object({
  userName: usernameSchema,
  email: emailSchema,
})

export default function EditUserModal({ utente, open, onClose }: Props) {
  const updateUser = useUpdateUser()
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<UpdateUserFormDTO>({
    resolver: zodResolver(schema),
  })

  useEffect(() => {
    if (utente) reset({ userName: utente.userName, email: utente.email })
  }, [utente, reset])

  const onSubmit = (data: UpdateUserFormDTO) => {
    if (!utente) return
    updateUser.mutate({ id: utente.id, data }, { onSuccess: onClose })
  }

  return (
    <Dialog open={open} onOpenChange={onClose}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Modifica utente</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4 mt-2">
          <div className="space-y-1">
            <Label>Username</Label>
            <Input {...register('userName')} />
            {errors.userName && <p className="text-red-500 text-sm">{errors.userName.message}</p>}
          </div>
          <div className="space-y-1">
            <Label>Email</Label>
            <Input type="email" {...register('email')} />
            {errors.email && <p className="text-red-500 text-sm">{errors.email.message}</p>}
          </div>
          <div className="flex justify-end gap-2">
            <Button type="button" variant="outline" onClick={onClose}>
              Annulla
            </Button>
            <Button type="submit" disabled={updateUser.isPending}>
              {updateUser.isPending ? 'Salvataggio...' : 'Salva'}
            </Button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  )
}
