import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { useUpdateUser } from '@/hooks/useAdminUtenti'
import type { UserDTO, UpdateUserFormDTO } from '@/types/utente'

type Props = {
  utente: UserDTO | undefined
  open: boolean
  onClose: () => void
}

export default function EditUserModal({ utente, open, onClose }: Props) {
  const updateUser = useUpdateUser()
  const { register, handleSubmit, reset } = useForm<UpdateUserFormDTO>()

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
          </div>
          <div className="space-y-1">
            <Label>Email</Label>
            <Input type="email" {...register('email')} />
          </div>
          <div className="flex justify-end gap-2">
            <Button type="button" variant="outline" onClick={onClose}>Annulla</Button>
            <Button type="submit" disabled={updateUser.isPending}>Salva</Button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  )
}