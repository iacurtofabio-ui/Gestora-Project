import { useEffect, useState } from 'react'
import { cn } from '@/lib/utils'
import { coloreBanda, quotaCoperti } from '@/lib/coperti'

/**
 * La banda dei coperti — l'elemento portante della direzione «Turno».
 *
 * L'idea: il dato che conta durante il servizio non e' "quanti coperti", e' "quanto manca al
 * tetto". Finche' resta un numero in una cella bisogna leggerlo e fare il conto; come banda che
 * si riempie si vede da lontano, senza leggere.
 *
 * Non e' un grafico e non introduce nessuna libreria: sono due div e una transizione di
 * larghezza. I dati arrivano da `copertiPerFascia` della Dashboard, che gia' contiene
 * `maxCoperti` e `copertiPrenotati` — nessuna chiamata nuova al backend.
 */

type Props = {
  prenotati: number
  capienza: number
  /**
   * Ritardo di partenza dell'animazione, in millisecondi. E' quello che produce la cascata
   * all'ingresso della Dashboard: le bande partono sfalsate di 40ms l'una dall'altra.
   */
  ritardoMs?: number
  /** Mentre React Query rinfresca i dati la banda respira, invece di smontare la pagina. */
  inAggiornamento?: boolean
  /** `totale` e' la banda grande in cima; `riga` quella di una singola fascia. */
  dimensione?: 'totale' | 'riga'
  className?: string
}

export function BandaCoperti({
  prenotati,
  capienza,
  ritardoMs = 0,
  inAggiornamento = false,
  dimensione = 'riga',
  className,
}: Props) {
  // La banda parte da zero e viene portata alla sua quota subito dopo il primo disegno: e'
  // quel secondo render a innescare la transizione CSS. Senza, il browser disegnerebbe
  // direttamente la larghezza finale e non ci sarebbe nessuna animazione.
  const [partita, setPartita] = useState(false)
  useEffect(() => {
    const id = requestAnimationFrame(() => setPartita(true))
    return () => cancelAnimationFrame(id)
  }, [])

  const quota = Math.min(100, quotaCoperti(prenotati, capienza))
  const barra = coloreBanda(quota)

  return (
    <div
      role="progressbar"
      aria-valuenow={prenotati}
      aria-valuemin={0}
      aria-valuemax={capienza}
      aria-label={`${prenotati} coperti prenotati su ${capienza}`}
      className={cn(
        'w-full overflow-hidden rounded-xs bg-traccia',
        dimensione === 'totale' ? 'h-3' : 'h-2',
        inAggiornamento && 'banda-in-aggiornamento',
        className
      )}
    >
      <div
        className={cn('banda-riempimento h-full rounded-xs', barra)}
        style={
          {
            '--quota': partita ? `${quota}%` : '0%',
            '--ritardo': `${ritardoMs}ms`,
          } as React.CSSProperties
        }
      />
    </div>
  )
}

