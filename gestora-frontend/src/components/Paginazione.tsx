import { ChevronLeftIcon, ChevronRightIcon, Loader2Icon } from 'lucide-react'
import { Button } from '@/components/ui/button'

type Props = {
  pagina: number
  paginePresenti: number
  totaleElementi: number
  /** Elementi effettivamente mostrati in questa pagina: serve a scrivere "1-20 di 137". */
  elementiInPagina: number
  pageSize: number
  inCaricamento?: boolean
  onCambioPagina: (pagina: number) => void
}

/**
 * REV-043 — barra di navigazione fra le pagine.
 *
 * Mostra sempre il totale, anche quando c'e' una sola pagina: e' il dato che prima mancava del
 * tutto, e sapere quante prenotazioni ci sono in totale e' utile di per se'. I pulsanti si
 * disabilitano agli estremi invece di sparire, cosi' la barra non cambia forma sotto il dito.
 *
 * Direzione «Turno» — la barra chiude il riquadro della tabella e non deve alzare la voce: e'
 * navigazione, non contenuto. Testo secondario, pulsanti in chiaro, e il fatto che stia
 * caricando si dice con una rotella accanto al conteggio invece che spegnendo tutto.
 */
export default function Paginazione({
  pagina,
  paginePresenti,
  totaleElementi,
  elementiInPagina,
  pageSize,
  inCaricamento = false,
  onCambioPagina,
}: Props) {
  const primoDellaPagina = totaleElementi === 0 ? 0 : (pagina - 1) * pageSize + 1
  const ultimoDellaPagina = primoDellaPagina === 0 ? 0 : primoDellaPagina + elementiInPagina - 1

  return (
    <div className="flex flex-wrap items-center justify-between gap-3 border-t p-3">
      <p className="text-nota flex items-center gap-2 text-muted-foreground tabular-nums">
        {totaleElementi === 0
          ? 'Nessuna prenotazione'
          : `${primoDellaPagina}-${ultimoDellaPagina} di ${totaleElementi}`}
        {inCaricamento && <Loader2Icon className="size-3 animate-spin" />}
      </p>

      <div className="flex items-center gap-2">
        <Button
          type="button"
          size="sm"
          variant="outline"
          disabled={pagina <= 1 || inCaricamento}
          onClick={() => onCambioPagina(pagina - 1)}
        >
          <ChevronLeftIcon />
          Precedente
        </Button>

        <span className="text-nota px-1 text-muted-foreground tabular-nums">
          {pagina} di {Math.max(paginePresenti, 1)}
        </span>

        <Button
          type="button"
          size="sm"
          variant="outline"
          disabled={pagina >= paginePresenti || inCaricamento}
          onClick={() => onCambioPagina(pagina + 1)}
        >
          Successiva
          <ChevronRightIcon />
        </Button>
      </div>
    </div>
  )
}
