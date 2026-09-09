import { cn } from '@/lib/utils'
import { STATI_PRENOTAZIONE, STATO_LABELS } from '@/types/prenotazione'

/**
 * Lo stato di una prenotazione.
 *
 * Su una tabella piena e' la differenza fra leggere riga per riga e vedere a colpo d'occhio dove
 * sono le annullate. Il colore non e' pero' l'unica informazione: resta la parola, perche' un
 * colore da solo non funziona per chi non distingue le tinte e non si legge in stampa.
 *
 * Direzione «Turno» — non e' piu' una pillola colorata. In una tabella densa dieci pillole
 * affiancate pesano quanto dieci pulsanti e rubano l'attenzione alle azioni vere. Qui lo stato e'
 * un punto piu' la parola: si legge alla stessa velocita' e pesa un decimo.
 */
const PUNTO: Record<string, string> = {
  // In attesa di conferma: e' la riga su cui c'e' ancora qualcosa da fare.
  [STATI_PRENOTAZIONE.ATTIVA]: 'bg-warning',
  // Confermata, cliente atteso.
  [STATI_PRENOTAZIONE.IN_CORSO]: 'bg-success',
  // Servizio concluso: informazione di archivio, non deve attirare l'occhio.
  [STATI_PRENOTAZIONE.COMPLETATA]: 'bg-muted-foreground/50',
  [STATI_PRENOTAZIONE.ANNULLATA]: 'bg-destructive',
}

const TESTO: Record<string, string> = {
  [STATI_PRENOTAZIONE.ATTIVA]: 'text-foreground',
  [STATI_PRENOTAZIONE.IN_CORSO]: 'text-foreground',
  [STATI_PRENOTAZIONE.COMPLETATA]: 'text-muted-foreground',
  [STATI_PRENOTAZIONE.ANNULLATA]: 'text-muted-foreground line-through decoration-1',
}

export function StatoBadge({ stato }: { stato: string | null }) {
  if (!stato) return <span className="text-muted-foreground">—</span>

  return (
    <span className={cn('text-corpo inline-flex items-center gap-2', TESTO[stato])}>
      <span
        aria-hidden="true"
        className={cn('size-1.5 shrink-0 rounded-full', PUNTO[stato] ?? 'bg-muted-foreground')}
      />
      {STATO_LABELS[stato] ?? stato}
    </span>
  )
}
