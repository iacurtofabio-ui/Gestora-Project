import type { ReactNode } from 'react'
import { cn } from '@/lib/utils'

/**
 * La schermata che occupa tutta la pagina quando non c'e' niente altro da mostrare:
 * errore non gestito, accesso negato, configurazione mancante.
 *
 * Erano quattro copie della stessa struttura scritte a mano
 * (`min-h-screen flex items-center justify-center` + una card con `text-lg font-semibold`), con
 * quattro spaziature leggermente diverse. Il testo dice **che cosa e' successo** e **come
 * rimediare**: sono i due soli motivi per cui una schermata cosi' esiste.
 *
 * `tono` decide solo il colore del filo in cima: rosso quando qualcosa e' andato storto, neutro
 * quando invece e' una condizione normale (per esempio un permesso che non si ha).
 */
export function SchermataMessaggio({
  titolo,
  children,
  azioni,
  dettaglio,
  tono = 'neutro',
}: {
  titolo: string
  children: ReactNode
  azioni?: ReactNode
  /** Il messaggio tecnico originale. Va mostrato, ma piccolo: serve a chi segnala, non a chi legge. */
  dettaglio?: string
  tono?: 'neutro' | 'errore'
}) {
  return (
    <div className="flex min-h-screen items-center justify-center bg-background p-6">
      <div className="w-full max-w-lg overflow-hidden rounded-2xl border bg-card">
        <div className={cn('h-1', tono === 'errore' ? 'bg-destructive' : 'bg-primary')} />
        <div className="space-y-3 p-6">
          <h1 className="text-titolo">{titolo}</h1>
          <div className="text-corpo space-y-3 text-muted-foreground text-pretty">{children}</div>
          {dettaglio && (
            <p className="text-nota rounded-md bg-muted p-2 break-words text-muted-foreground">
              {dettaglio}
            </p>
          )}
          {azioni && <div className="flex flex-wrap gap-2 pt-1">{azioni}</div>}
        </div>
      </div>
    </div>
  )
}
