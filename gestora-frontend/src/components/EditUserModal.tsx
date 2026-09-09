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
          <DialogTitle className="text-titolo">Modifica utente</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4 mt-2">
          <div className="space-y-1">
            <Label htmlFor="edit-user-username">Nome</Label>
            <Input id="edit-user-username" {...register('userName')} />
            {errors.userName && (
              <p className="text-nota text-destructive">{errors.userName.message}</p>
            )}
          </div>
          <div className="space-y-1">
            <Label htmlFor="edit-user-email">Email</Label>
            <Input id="edit-user-email" type="email" {...register('email')} />
            {errors.email && <p className="text-nota text-destructive">{errors.email.message}</p>}
          </div>
          <div className="flex justify-end gap-2 border-t pt-4">
            <Button type="button" variant="ghost" onClick={onClose}>
              Annulla
            </Button>
            <Button type="submit" disabled={updateUser.isPending}>
              {updateUser.isPending ? 'Salvataggio…' : 'Salva modifiche'}
            </Button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  )
}
