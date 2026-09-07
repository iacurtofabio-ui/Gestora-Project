import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { useResetPassword } from '@/hooks/useAdminUtenti'
import { passwordSchema } from '@/lib/validazioni'
import type { UserDTO, ResetPasswordDTO } from '@/types/utente'

type Props = {
  utente: UserDTO | undefined
  open: boolean
  onClose: () => void
}

// REV-048: qui c'era solo `required: true`, quindi il form accettava qualsiasi password non vuota
// mentre il backend applica la policy di AdminResetPasswordDTOValidator (REV-013). L'Admin scopriva
// le regole solo dopo il rifiuto, senza sapere quale delle quattro non fosse rispettata.
const schema = z.object({
  newPassword: passwordSchema,
})

export default function ResetPasswordModal({ utente, open, onClose }: Props) {
  const resetPassword = useResetPassword()
  const { register, handleSubmit, reset, formState: { errors } } = useForm<ResetPasswordDTO>({
    resolver: zodResolver(schema),
  })

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
            <Input type="password" autoComplete="new-password" {...register('newPassword')} />
            {errors.newPassword ? (
              <p className="text-red-500 text-sm">{errors.newPassword.message}</p>
            ) : (
              <p className="text-xs text-gray-500">
                Almeno 8 caratteri, una maiuscola, un numero e un carattere speciale.
              </p>
            )}
          </div>
          <div className="flex justify-end gap-2">
            <Button type="button" variant="outline" onClick={onClose}>Annulla</Button>
            <Button type="submit" disabled={resetPassword.isPending}>
              {resetPassword.isPending ? 'Conferma...' : 'Conferma'}
            </Button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  )
}
