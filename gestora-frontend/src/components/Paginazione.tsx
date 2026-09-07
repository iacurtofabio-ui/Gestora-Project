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
    <div className="flex items-center justify-between p-4 border-t text-sm text-gray-600">
      <span>
        {totaleElementi === 0
          ? 'Nessuna prenotazione'
          : `${primoDellaPagina}-${ultimoDellaPagina} di ${totaleElementi}`}
      </span>

      <div className="flex items-center gap-3">
        <Button
          type="button"
          size="sm"
          variant="outline"
          disabled={pagina <= 1 || inCaricamento}
          onClick={() => onCambioPagina(pagina - 1)}
        >
          Precedente
        </Button>

        <span className="tabular-nums">
          Pagina {pagina} di {Math.max(paginePresenti, 1)}
        </span>

        <Button
          type="button"
          size="sm"
          variant="outline"
          disabled={pagina >= paginePresenti || inCaricamento}
          onClick={() => onCambioPagina(pagina + 1)}
        >
          Successiva
        </Button>
      </div>
    </div>
  )
}
