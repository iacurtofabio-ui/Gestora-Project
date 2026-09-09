import { useState } from 'react'
import { Loader2Icon } from 'lucide-react'
import { Link } from 'react-router-dom'
import { useCheckDisponibilita } from '@/hooks/useDisponibilita'
import { oggiInItalia } from '@/lib/date'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'

/**
 * Fase 13 — verifica della disponibilità aperta a chiunque, senza account.
 *
 * È l'unica parte viva della pagina pubblica, e funziona perché `check-disponibilita` è l'unico
 * endpoint del backend dichiarato pubblico (nessuna autenticazione). Risponde con **tutte** le
 * fasce del giorno scelto, ciascuna già valutata per il numero di coperti richiesto: una sola
 * chiamata basta per l'intero elenco.
 *
 * ⚠️ Attenzione se un domani si vuole arricchire questa pagina: `get-zone-attive`,
 * `get-all-fasce` e tutto il resto **richiedono l'accesso**. Chiamarli da qui produrrebbe un 401
 * su una pagina pubblica.
 */
export function VerificaDisponibilita() {
  const [data, setData] = useState('')
  const [coperti, setCoperti] = useState('2')
  // La ricerca non parte mentre si scrive: parte quando si preme il pulsante. Altrimenti a ogni
  // tasto sul numero di coperti partirebbe una chiamata.
  const [ricerca, setRicerca] = useState<{ data: string; coperti: number } | undefined>(undefined)

  const disponibilita = useCheckDisponibilita(ricerca?.data, ricerca?.coperti)

  function cerca(e: React.FormEvent) {
    e.preventDefault()
    const n = Number(coperti)
    if (!data || !Number.isFinite(n) || n < 1) return
    setRicerca({ data, coperti: n })
  }

  const fasce = disponibilita.data?.fasce ?? []
  const almenoUnaLibera = fasce.some((f) => f.disponibilePerRichiesta)

  return (
    <Card className="w-full max-w-2xl">
      <CardHeader>
        <CardTitle className="text-titolo">C'è posto?</CardTitle>
        <CardDescription>
          Scegli il giorno e quante persone siete. I turni liberi si vedono subito, senza registrarsi.
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        <form onSubmit={cerca} className="flex flex-col sm:flex-row gap-3 sm:items-end">
          <div className="flex-1 space-y-1">
            <Label htmlFor="disp-data">Giorno</Label>
            <Input
              id="disp-data"
              type="date"
              value={data}
              // Non ha senso proporre una data passata: il backend la rifiuterebbe comunque.
              min={oggiInItalia()}
              onChange={(e) => setData(e.target.value)}
              required
            />
          </div>
          <div className="w-full sm:w-32 space-y-1">
            <Label htmlFor="disp-coperti">Persone</Label>
            <Input
              id="disp-coperti"
              type="number"
              min={1}
              max={50}
              value={coperti}
              onChange={(e) => setCoperti(e.target.value)}
              required
            />
          </div>
          <Button type="submit" disabled={!data}>
            Vedi i turni liberi
          </Button>
        </form>

        {disponibilita.isFetching && (
          <p className="flex items-center gap-2 text-sm text-muted-foreground">
            <Loader2Icon className="size-4 animate-spin" />
            Sto controllando i turni…
          </p>
        )}

        {/* L'hook non ritenta di proposito (è un'anteprima): un errore qui va detto e basta,
            senza bloccare la pagina. */}
        {disponibilita.isError && (
          <p className="text-corpo text-destructive">
            Non riesco a controllare la disponibilità in questo momento. Riprova fra poco, oppure
            chiamaci: al telefono la prenotazione la prendiamo lo stesso.
          </p>
        )}

        {ricerca && !disponibilita.isFetching && !disponibilita.isError && (
          <div className="space-y-2">
            {fasce.length === 0 ? (
              <p className="text-sm text-muted-foreground">
                Per quel giorno non ci sono turni configurati. Prova con un'altra data.
              </p>
            ) : (
              <>
                <ul className="divide-y rounded-md border">
                  {fasce.map((f) => (
                    <li
                      key={f.fasciaOrariaId}
                      className="flex items-center justify-between gap-4 p-3"
                    >
                      <span className="text-orario tabular-nums">
                        {f.orarioInizio.slice(0, 5)}–{f.orarioFine.slice(0, 5)}
                      </span>
                      {f.disponibilePerRichiesta ? (
                        <span className="text-corpo inline-flex items-center gap-2">
                          <span aria-hidden="true" className="size-1.5 rounded-full bg-success" />
                          Libero
                        </span>
                      ) : (
                        <span className="flex items-baseline justify-end gap-2 text-right">
                          {/* Il motivo arriva già scritto dal backend e distingue "tetto coperti
                              esaurito" da "nessun tavolo abbastanza grande": è più utile di un
                              generico "non disponibile". */}
                          <span className="text-nota hidden text-muted-foreground sm:inline">
                            {f.messaggio}
                          </span>
                          <span className="text-corpo inline-flex shrink-0 items-center gap-2 text-muted-foreground">
                            <span
                              aria-hidden="true"
                              className="size-1.5 rounded-full bg-muted-foreground/50"
                            />
                            Pieno
                          </span>
                        </span>
                      )}
                    </li>
                  ))}
                </ul>

                {almenoUnaLibera ? (
                  <div className="flex flex-col sm:flex-row sm:items-center gap-3 pt-1">
                    <p className="text-sm text-muted-foreground flex-1">
                      Per prenotare serve un account: si crea in un minuto.
                    </p>
                    <Button asChild>
                      <Link to="/register">Prenota ora</Link>
                    </Button>
                  </div>
                ) : (
                  <p className="text-sm text-muted-foreground pt-1">
                    Quel giorno siamo al completo. Prova con un altro giorno o con un numero diverso
                    di persone.
                  </p>
                )}
              </>
            )}
          </div>
        )}
      </CardContent>
    </Card>
  )
}
