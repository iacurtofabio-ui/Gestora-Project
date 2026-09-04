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
                    <AlertDialogTitle>{titolo}</AlertDialogTitle>
                    <AlertDialogDescription>{descrizione}</AlertDialogDescription>
                </AlertDialogHeader>
                <AlertDialogFooter>
                    <AlertDialogCancel onClick={onCancel}>Annulla</AlertDialogCancel>
                    <AlertDialogAction
                        onClick={onConfirm}
                        className="bg-red-500 hover:bg-red-600"
                    >
                        {testoConferma}
                    </AlertDialogAction>
                </AlertDialogFooter>
            </AlertDialogContent>
        </AlertDialog>
    )
}