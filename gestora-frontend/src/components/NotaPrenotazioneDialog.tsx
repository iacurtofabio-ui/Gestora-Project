import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'

type Props = {
  /** Undefined = finestra chiusa. */
  nota: { cliente: string; testo: string } | undefined
  onClose: () => void
}

/** Mostra la nota di una prenotazione per intero: nella tabella se ne vede solo l'anteprima. */
export default function NotaPrenotazioneDialog({ nota, onClose }: Props) {
  return (
    <Dialog open={nota !== undefined} onOpenChange={(aperta) => !aperta && onClose()}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle className="text-titolo">Nota della prenotazione</DialogTitle>
          <DialogDescription className="text-nota">{nota?.cliente}</DialogDescription>
        </DialogHeader>
        <p className="text-corpo max-h-[60vh] overflow-y-auto whitespace-pre-wrap break-words">
          {nota?.testo}
        </p>
      </DialogContent>
    </Dialog>
  )
}
