/**
 * REV-073 — una lista vuota mostrava solo l'intestazione della tabella, senza spiegazione:
 * su Postazioni, prima di scegliere una zona, sembrava un errore invece di uno stato normale.
 *
 * Va dentro un `<tbody>`, con `colSpan` pari al numero di colonne della tabella.
 */
export function EmptyState({ messaggio, colSpan }: { messaggio: string; colSpan: number }) {
  return (
    <tr>
      <td colSpan={colSpan} className="p-6 text-center text-sm text-gray-400">
        {messaggio}
      </td>
    </tr>
  )
}
