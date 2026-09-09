import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog'

type Props = {
  open: boolean
  descrizione: string
  onConfirm: () => void
  onCancel: () => void
  /**
   * Titolo ed etichetta del pulsante di conferma. Hanno un default perche' il dialogo nasce
   * per le eliminazioni, ma vanno passati quando l'azione e' un'altra: la pagina Prenotazioni
   * usa lo stesso dialogo per annullare e per eliminare, due cose diverse che non possono
   * presentarsi entrambe come "Elimina".
   */
  titolo?: string
  testoConferma?: string
}

export default function ConfirmDialog({
  open,
  descrizione,
  onConfirm,
  onCancel,
  titolo = 'Conferma eliminazione',
  testoConferma = 'Elimina',
}: Props) {
  return (
    <AlertDialog open={open}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle className="text-titolo">{titolo}</AlertDialogTitle>
          <AlertDialogDescription className="text-corpo text-muted-foreground text-pretty">
            {descrizione}
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          {/* "Torna indietro" e non "Annulla": su Prenotazioni l'azione da confermare si chiama
              gia' "Annulla prenotazione", e due pulsanti affiancati con lo stesso verbo che fanno
              cose opposte sono il modo piu' rapido per far premere quello sbagliato. */}
          <AlertDialogCancel onClick={onCancel}>Torna indietro</AlertDialogCancel>
          <AlertDialogAction
            onClick={onConfirm}
            className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
          >
            {testoConferma}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}
