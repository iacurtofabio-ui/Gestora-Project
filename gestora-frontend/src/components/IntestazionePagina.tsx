import type { ReactNode } from 'react'

/**
 * L'intestazione di una schermata a elenco.
 *
 * Prima ogni pagina la scriveva a modo suo: `text-2xl font-bold` su Utenti, `text-sm
 * font-semibold` dentro il bordo della card su Zone, Postazioni, Fasce e Prenotazioni. Stesso
 * ruolo, quattro trattamenti — e in tre casi su quattro il titolo della SCHERMATA era
 * l'intestazione di una card, cioe' pesava quanto il titolo di un blocco qualsiasi.
 *
 * Qui il titolo sta fuori dal contenitore, in `text-titolo`, uno solo per pagina. Accanto va il
 * conteggio come dato secondario, e a destra l'azione primaria della pagina — l'unico pulsante
 * pieno della schermata.
 */
export function IntestazionePagina({
  titolo,
  conteggio,
  azione,
}: {
  titolo: string
  /** Es. "12 zone". Si nasconde da solo durante il caricamento, passando `undefined`. */
  conteggio?: string
  azione?: ReactNode
}) {
  return (
    <div className="flex flex-wrap items-center justify-between gap-x-4 gap-y-2">
      <div className="flex flex-wrap items-baseline gap-x-3 gap-y-1">
        <h1 className="text-titolo">{titolo}</h1>
        {conteggio && (
          <p className="text-nota text-muted-foreground tabular-nums">{conteggio}</p>
        )}
      </div>
      {azione}
    </div>
  )
}
