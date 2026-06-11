import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { useAssignRole, useRemoveRole } from '@/hooks/useAdminUtenti'
import { RUOLI_DISPONIBILI } from '@/types/utente'
import type { UserDTO } from '@/types/utente'

type Props = {
  utente: UserDTO | undefined
  open: boolean
  onClose: () => void
}

export default function GestisciRuoliModal({ utente, open, onClose }: Props) {
  const assignRole = useAssignRole()
  const removeRole = useRemoveRole()

  if (!utente) return null

  return (
    <Dialog open={open} onOpenChange={onClose}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Gestisci ruoli — {utente.userName}</DialogTitle>
        </DialogHeader>
        <div className="space-y-3 mt-2">
          {RUOLI_DISPONIBILI.map((ruolo) => {
            const haRuolo = utente.roles.includes(ruolo)
            return (
              <div key={ruolo} className="flex items-center justify-between">
                <span className="text-sm font-medium">{ruolo}</span>
                {haRuolo ? (
                  <Button size="sm" variant="destructive"
                    disabled={removeRole.isPending}
                    onClick={() => removeRole.mutate({ userId: utente.id, role: ruolo })}>
                    Rimuovi
                  </Button>
                ) : (
                  <Button size="sm" variant="outline"
                    disabled={assignRole.isPending}
                    onClick={() => assignRole.mutate({ userId: utente.id, role: ruolo })}>
                    Assegna
                  </Button>
                )}
              </div>
            )
          })}
        </div>
        <div className="flex justify-end mt-4">
          <Button variant="outline" onClick={onClose}>Chiudi</Button>
        </div>
      </DialogContent>
    </Dialog>
  )
}