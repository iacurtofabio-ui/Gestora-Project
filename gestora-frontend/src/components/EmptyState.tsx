import type { ReactNode } from 'react'
import { TableCell, TableRow } from '@/components/ui/table'

/**
 * REV-073 — una lista vuota mostrava solo l'intestazione della tabella, senza spiegazione:
 * su Postazioni, prima di scegliere una zona, sembrava un errore invece di uno stato normale.
 *
 * Direzione «Turno» — una lista vuota non e' un'informazione, e' un momento in cui l'utente non
 * sa cosa fare. Oltre alla frase c'e' quindi posto per l'azione che risolve: `azione` prende un
 * pulsante ("Aggiungi prenotazione", "Azzera i filtri"). Senza azione il componente si comporta
 * come prima.
 *
 * Va dentro un `<TableBody>`, con `colSpan` pari al numero di colonne della tabella.
 */
export function EmptyState({
  messaggio,
  colSpan,
  azione,
}: {
  messaggio: string
  colSpan: number
  azione?: ReactNode
}) {
  return (
    <TableRow className="hover:bg-transparent">
      <TableCell colSpan={colSpan} className="h-40 text-center">
        <div className="flex flex-col items-center justify-center gap-3">
          <p className="text-corpo max-w-sm text-muted-foreground text-pretty">{messaggio}</p>
          {azione}
        </div>
      </TableCell>
    </TableRow>
  )
}
