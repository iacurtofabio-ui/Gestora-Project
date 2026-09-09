import { cn } from '@/lib/utils'

/**
 * Attiva / non attiva, per zone, tavoli e fasce orarie.
 *
 * Erano tre copie della stessa pillola colorata scritta a mano in tre pagine
 * (`bg-success/15 text-success border-success/30`), con il rischio che prima o poi dicessero la
 * stessa cosa in tre modi.
 *
 * Il trattamento e' lo stesso di [StatoBadge] per le prenotazioni: un punto piu' la parola, non
 * una pillola. In una tabella dieci pillole affiancate pesano quanto dieci pulsanti e rubano
 * l'attenzione alle azioni. Il colore non e' mai l'unica informazione: la parola resta, perche'
 * un colore da solo non funziona per chi non distingue le tinte e non si legge in stampa.
 */
export function StatoAttivo({ attiva }: { attiva: boolean }) {
  return (
    <span
      className={cn(
        'text-corpo inline-flex items-center gap-2',
        attiva ? 'text-foreground' : 'text-muted-foreground'
      )}
    >
      <span
        aria-hidden="true"
        className={cn(
          'size-1.5 shrink-0 rounded-full',
          attiva ? 'bg-success' : 'bg-muted-foreground/50'
        )}
      />
      {attiva ? 'Attiva' : 'Non attiva'}
    </span>
  )
}
