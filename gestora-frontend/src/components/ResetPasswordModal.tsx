import { useForm } from 'react-hook-form'
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { useResetPassword } from '@/hooks/useAdminUtenti'
import type { UserDTO, ResetPasswordDTO } from '@/types/utente'

type Props = {
  utente: UserDTO | undefined
  open: boolean
  onClose: () => void
}

export default function ResetPasswordModal({ utente, open, onClose }: Props) {
  const resetPassword = useResetPassword()
  const { register, handleSubmit, reset } = useForm<ResetPasswordDTO>()

  const onSubmit = (data: ResetPasswordDTO) => {
    if (!utente) return
    resetPassword.mutate({ id: utente.id, data }, {
      onSuccess: () => { reset(); onClose() }
    })
  }

  return (
    <Dialog open={open} onOpenChange={onClose}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Reset password — {utente?.userName}</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4 mt-2">
          <div className="space-y-1">
            <Label>Nuova password</Label>
            <Input type="password" {...register('newPassword', { required: true })} />
          </div>
          <div className="flex justify-end gap-2">
            <Button type="button" variant="outline" onClick={onClose}>Annulla</Button>
            <Button type="submit" disabled={resetPassword.isPending}>Conferma</Button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  )
}