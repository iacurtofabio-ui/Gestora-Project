import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { ChevronLeftIcon, ChevronRightIcon } from 'lucide-react'
import { useAuth } from '@/hooks/useAuth'
import { useDashboardGiornaliera, useDashboardSettimanale } from '@/hooks/useDashboard'
import {
  usePrenotazioni,
  useConfermaPrenotazione,
  useCompletaPrenotazione,
  useAnnullaPrenotazione,
  useDeletePrenotazione,
} from '@/hooks/usePrenotazioni'
import {
  oggiInItalia,
  lunediSettimanaDi,
  dataEstesaInItalia,
  aggiungiGiorni,
} from '@/lib/date'
import { PageError, DashboardSkeleton } from '@/components/PageState'
import { BandaCoperti } from '@/components/BandaCoperti'
import { AzioniPrenotazione } from '@/components/AzioniPrenotazione'
import ConfirmDialog from '@/components/ConfirmDialog'
import { coloreQuota } from '@/lib/coperti'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'
import type { CopertiFasciaDTO } from '@/types/dashboard'
import type { PrenotazioneDTO } from '@/types/prenotazione'

/**
 * Direzione «Turno» — la sala vista nel tempo.
 *
 * FASE 7: la dashboard era statica (solo il giorno corrente, dati fermi finche' non si
 * ricaricava la pagina). Ora si naviga avanti e indietro nei giorni, i dati si aggiornano da
 * soli ogni minuto, e compare un blocco con le prossime prenotazioni in arrivo — la domanda a
 * cui la dashboard serve davvero durante il servizio: "chi arriva adesso?".
 *
 * La banda del giorno resta l'elemento dominante (decisione del redesign "Turno", non si
 * riapre): tutto il resto attorno resta disciplinato.
 */

/** Il passo della cascata all'ingresso: le bande partono sfalsate di 40ms l'una dall'altra. */
const PASSO_CASCATA_MS = 40

/** La banda grande parte per prima; le fasce la seguono. */
const RITARDO_FASCE_MS = 120

function RigaFascia({
  fascia,
  indice,
  inAggiornamento,
  data,
}: {
  fascia: CopertiFasciaDTO
  indice: number
  inAggiornamento: boolean
  data: string
}) {
  const oltreIlTetto = fascia.copertiOltreIlTetto > 0
  const esaurita = fascia.copertiDisponibili === 0 && !oltreIlTetto

  return (
    <li className="border-b last:border-b-0">
      {/* FASE 7: la riga porta all'elenco delle prenotazioni di quella fascia. `group/riga` fa
          reagire la banda al passaggio del mouse, come le azioni di riga altrove. */}
      <Link
        to={`/prenotazioni?data=${data}&fascia=${fascia.fasciaOrariaId}`}
        className={cn(
          'group/riga grid grid-cols-[auto_1fr] items-center gap-x-4 gap-y-2 rounded-md py-3 -mx-2 px-2 transition-colors hover:bg-muted/50',
          'sm:grid-cols-[7rem_1fr_auto]'
        )}
      >
        <span className="text-orario tabular-nums whitespace-nowrap">
          {fascia.oraInizio.slice(0, 5)}–{fascia.oraFine.slice(0, 5)}
        </span>

        <span
          className={cn(
            'text-orario justify-self-end tabular-nums whitespace-nowrap sm:order-last',
            coloreQuota(fascia.copertiPrenotati, fascia.maxCoperti)
          )}
        >
          {fascia.copertiPrenotati}
          <span className="text-muted-foreground"> / {fascia.maxCoperti}</span>
          {esaurita && <span className="text-destructive"> · pieno</span>}
          {oltreIlTetto && (
            <span className="text-destructive"> · {fascia.copertiOltreIlTetto} oltre il tetto</span>
          )}
        </span>

        <BandaCoperti
          prenotati={fascia.copertiPrenotati}
          capienza={fascia.maxCoperti}
          ritardoMs={RITARDO_FASCE_MS + indice * PASSO_CASCATA_MS}
          inAggiornamento={inAggiornamento}
          className="col-span-2 sm:col-span-1 sm:order-2"
        />
      </Link>
    </li>
  )
}

/** Un numero della striscia. Non e' una card: e' una cella di una griglia unica. */
function Numero({ etichetta, valore }: { etichetta: string; valore: number | undefined }) {
  return (
    <div className="bg-card p-4">
      <p className="text-nota text-muted-foreground">{etichetta}</p>
      <p className="text-readout-sm mt-1 tabular-nums">{valore ?? '—'}</p>
    </div>
  )
}

/**
 * FASE 7 — le prossime 5 prenotazioni non ancora concluse del giorno scelto, a partire dall'ora
 * corrente (solo se il giorno scelto e' oggi: su un altro giorno l'ordine parte dall'inizio).
 * Riusa AzioniPrenotazione per coerenza con la tabella Prenotazioni: le stesse regole valgono
 * ovunque, non solo qui.
 */
function ProssimeInArrivo({ data }: { data: string }) {
  const { user } = useAuth()
  const isStaff = Boolean(user?.roles.includes('Admin') || user?.roles.includes('Staff'))
  const isAdmin = Boolean(user?.roles.includes('Admin'))
  const navigate = useNavigate()

  const prenotazioni = usePrenotazioni({ data, pageSize: 100 })
  const conferma = useConfermaPrenotazione()
  const completa = useCompletaPrenotazione()
  const annulla = useAnnullaPrenotazione()
  const elimina = useDeletePrenotazione()
  const [idDaAnnullare, setIdDaAnnullare] = useState<number | undefined>(undefined)
  const [idDaEliminare, setIdDaEliminare] = useState<number | undefined>(undefined)

  if (!prenotazioni.data) return null

  const oraAttuale = data === oggiInItalia()
    ? new Intl.DateTimeFormat('en-GB', {
        hour: '2-digit',
        minute: '2-digit',
        hour12: false,
        timeZone: 'Europe/Rome',
      }).format(new Date())
    : '00:00'

  const prossime = prenotazioni.data.items
    .filter(
      (p) =>
        (p.stato === 'Attiva' || p.stato === 'InCorso') &&
        (p.oraInizio ?? '00:00') >= oraAttuale
    )
    .sort((a, b) => (a.oraInizio ?? '').localeCompare(b.oraInizio ?? ''))
    .slice(0, 5)

  if (prossime.length === 0) return null

  return (
    <section className="space-y-1">
      <h2 className="text-sezione text-muted-foreground">In arrivo</h2>
      <ul>
        {prossime.map((p: PrenotazioneDTO) => (
          <li
            key={p.id}
            className="group/riga flex items-center justify-between gap-3 border-b py-2.5 last:border-b-0"
          >
            <div className="min-w-0">
              <p className="text-corpo truncate font-medium">{p.nomeCliente ?? p.nomeUtente}</p>
              <p className="text-nota text-muted-foreground tabular-nums">
                {p.oraInizio?.slice(0, 5)} · {p.numeroCoperti} coperti
              </p>
            </div>
            <AzioniPrenotazione
              prenotazione={p}
              isStaff={isStaff}
              isAdmin={isAdmin}
              inCorso={
                (conferma.isPending && conferma.variables === p.id) ||
                (completa.isPending && completa.variables === p.id)
              }
              onConferma={() => conferma.mutate(p.id)}
              onCompleta={() => completa.mutate(p.id)}
              onModifica={() => navigate(`/prenotazioni?data=${p.dataPrenotazione}`)}
              onAnnulla={() => setIdDaAnnullare(p.id)}
              onElimina={() => setIdDaEliminare(p.id)}
            />
          </li>
        ))}
      </ul>

      <ConfirmDialog
        open={idDaAnnullare !== undefined}
        titolo="Annullare la prenotazione?"
        testoConferma="Annulla prenotazione"
        descrizione="Il tavolo torna disponibile per quella fascia. L'operazione non si puo' annullare."
        onConfirm={() => {
          annulla.mutate(idDaAnnullare!)
          setIdDaAnnullare(undefined)
        }}
        onCancel={() => setIdDaAnnullare(undefined)}
      />
      <ConfirmDialog
        open={idDaEliminare !== undefined}
        titolo="Eliminare definitivamente?"
        testoConferma="Elimina"
        descrizione="La prenotazione sparisce dallo storico. L'operazione non si puo' annullare."
        onConfirm={() => {
          elimina.mutate(idDaEliminare!)
          setIdDaEliminare(undefined)
        }}
        onCancel={() => setIdDaEliminare(undefined)}
      />
    </section>
  )
}

export default function DashboardPage() {
  // FASE 7: il giorno mostrato e' navigabile, non piu' fisso su oggi.
  const [data, setData] = useState(oggiInItalia())
  const { user } = useAuth()
  const isAdmin = user?.roles.includes('Admin') ?? false
  const giornaliera = useDashboardGiornaliera(data)
  const settimanale = useDashboardSettimanale(lunediSettimanaDi(data))

  if (giornaliera.isLoading || settimanale.isLoading) return <DashboardSkeleton />
  if (giornaliera.isError || settimanale.isError)
    return (
      <PageError
        error={giornaliera.error ?? settimanale.error}
        fallback="Errore nel caricamento della dashboard."
        onRiprova={() => {
          giornaliera.refetch()
          settimanale.refetch()
        }}
        className="m-0"
      />
    )

  const fasce = giornaliera.data?.copertiPerFascia ?? []
  const capienzaGiorno = fasce.reduce((somma, f) => somma + f.maxCoperti, 0)
  const copertiGiorno = giornaliera.data?.totaleCopertiPrenotati ?? 0
  const residuo = capienzaGiorno - copertiGiorno
  const inAggiornamento = giornaliera.isFetching && !giornaliera.isLoading

  const oraAggiornamento = giornaliera.dataUpdatedAt
    ? new Intl.DateTimeFormat('it-IT', {
        hour: '2-digit',
        minute: '2-digit',
        timeZone: 'Europe/Rome',
      }).format(giornaliera.dataUpdatedAt)
    : null

  return (
    <div className="mx-auto max-w-5xl space-y-10">
      {/* ------------------------------------------------------------------
          Il momento orchestrato: la banda del giorno.
          ------------------------------------------------------------------ */}
      <section className="space-y-4">
        <div className="flex flex-wrap items-center justify-between gap-x-4 gap-y-2">
          <h1 className="text-titolo">Oggi in sala</h1>

          {/* FASE 7: navigazione per giorno. ‹ ieri · Oggi · domani › piu' una data a scelta. */}
          <div className="flex items-center gap-1">
            <Button
              variant="ghost"
              size="icon-sm"
              onClick={() => setData((d) => aggiungiGiorni(d, -1))}
              aria-label="Giorno precedente"
            >
              <ChevronLeftIcon />
            </Button>
            <Button variant="ghost" size="sm" onClick={() => setData(oggiInItalia())}>
              Oggi
            </Button>
            <input
              type="date"
              value={data}
              onChange={(e) => e.target.value && setData(e.target.value)}
              className="border-input bg-background text-corpo h-8 rounded-md border px-2"
              aria-label="Scegli una data"
            />
            <Button
              variant="ghost"
              size="icon-sm"
              onClick={() => setData((d) => aggiungiGiorni(d, 1))}
              aria-label="Giorno successivo"
            >
              <ChevronRightIcon />
            </Button>
          </div>
        </div>

        <div className="flex flex-wrap items-baseline justify-between gap-x-4 gap-y-1">
          <p className="text-corpo text-muted-foreground first-letter:uppercase">
            {dataEstesaInItalia(data)}
          </p>
          {oraAggiornamento && (
            <p className="text-nota text-muted-foreground tabular-nums">
              aggiornato alle {oraAggiornamento}
            </p>
          )}
        </div>

        {capienzaGiorno > 0 ? (
          <div className="space-y-3">
            {/* Numeri e didascalia su due righe distinte: in linea, sotto i 400px la didascalia
                si infila fra "78 /" e "132" e spezza il dato in due. */}
            <div>
              <p className="flex items-baseline gap-2">
                <span
                  className={cn(
                    'text-readout tabular-nums',
                    coloreQuota(copertiGiorno, capienzaGiorno)
                  )}
                >
                  {copertiGiorno}
                </span>
                <span className="text-titolo text-muted-foreground tabular-nums">
                  / {capienzaGiorno}
                </span>
              </p>
              <p className="text-corpo text-muted-foreground">
                coperti prenotati sulla capienza del giorno
                {residuo > 0 && <span className="tabular-nums"> · ancora {residuo} liberi</span>}
                {residuo === 0 && <span className="text-warning"> · al completo</span>}
                {residuo < 0 && (
                  <span className="text-destructive tabular-nums">
                    {' '}
                    · {-residuo} oltre il tetto
                  </span>
                )}
              </p>
            </div>
            <BandaCoperti
              prenotati={copertiGiorno}
              capienza={capienzaGiorno}
              dimensione="totale"
              inAggiornamento={inAggiornamento}
            />
          </div>
        ) : (
          /* Stato vuoto: non e' un errore, e' una sala non ancora configurata. Dice cosa manca
             e porta dove si rimedia. */
          <div className="rounded-xl border border-dashed p-6">
            <p className="text-sezione">Per questo giorno non c'e' nessuna fascia oraria</p>
            <p className={cn('text-corpo mt-1 text-muted-foreground text-pretty', isAdmin && 'mb-4')}>
              Senza fasce non c'e' un tetto di coperti da riempire, e la sala non accetta
              prenotazioni.{' '}
              {isAdmin ? 'Puoi configurarle adesso.' : 'Chiedi a un amministratore di configurarle.'}
            </p>
            {isAdmin && (
              <Button asChild size="sm">
                <Link to="/fasce-orarie">Configura le fasce orarie</Link>
              </Button>
            )}
          </div>
        )}
      </section>

      {/* ------------------------------------------------------------------
          Le fasce, una per riga, cliccabili. Nessuna card: la riga E' la banda.
          ------------------------------------------------------------------ */}
      {fasce.length > 0 && (
        <section className="space-y-1">
          <h2 className="text-sezione text-muted-foreground">Le fasce del giorno</h2>
          <ul>
            {fasce.map((fascia, i) => (
              <RigaFascia
                key={fascia.fasciaOrariaId}
                fascia={fascia}
                indice={i}
                inAggiornamento={inAggiornamento}
                data={data}
              />
            ))}
          </ul>
        </section>
      )}

      {/* ------------------------------------------------------------------
          Le prossime prenotazioni in arrivo. Si nasconde da sola se non ce ne sono.
          ------------------------------------------------------------------ */}
      <ProssimeInArrivo data={data} />

      {/* ------------------------------------------------------------------
          I numeri di contorno. Una griglia sola divisa da filetti, non quattro
          card identiche: sono informazioni di servizio, non titoli.
          ------------------------------------------------------------------ */}
      <section className="space-y-3">
        <h2 className="text-sezione text-muted-foreground">Il resto della giornata</h2>
        <div className="grid grid-cols-2 gap-px overflow-hidden rounded-xl border bg-border sm:grid-cols-4">
          <Numero etichetta="Prenotazioni" valore={giornaliera.data?.totalePrenotazioni} />
          <Numero etichetta="Ancora da confermare" valore={giornaliera.data?.prenotazioniAttive} />
          <Numero etichetta="Tavoli liberi" valore={giornaliera.data?.postazioniLibere} />
          <Numero etichetta="Tavoli occupati" valore={giornaliera.data?.postazioniOccupate} />
        </div>
      </section>

      {/* ------------------------------------------------------------------
          La settimana. Disciplinata e silenziosa: qui non si compete con la banda.
          Ogni riga porta alle prenotazioni di quel giorno; la banda mostra la
          capienza cosi' come la vista giornaliera.
          ------------------------------------------------------------------ */}
      <section className="space-y-3">
        <div className="flex flex-wrap items-baseline justify-between gap-x-4 gap-y-1">
          <h2 className="text-sezione text-muted-foreground">Questa settimana</h2>
          <p className="text-nota text-muted-foreground tabular-nums">
            {settimanale.data?.totalePrenotazioni ?? 0} prenotazioni ·{' '}
            {settimanale.data?.totaleCoperti ?? 0} coperti
          </p>
        </div>

        <div className="overflow-x-auto">
          <table className="w-full">
            <thead>
              <tr className="border-b">
                <th className="text-nota py-2 pr-4 text-left font-medium text-muted-foreground">
                  Giorno
                </th>
                <th className="text-nota py-2 pr-4 text-right font-medium text-muted-foreground">
                  Prenotazioni
                </th>
                <th className="text-nota py-2 pr-4 text-left font-medium text-muted-foreground">
                  Coperti / capienza
                </th>
                <th className="text-nota py-2 text-right font-medium text-muted-foreground">
                  Annullate
                </th>
              </tr>
            </thead>
            <tbody>
              {settimanale.data?.giorni.map((giorno) => (
                <tr key={giorno.data} className="group/riga border-b last:border-b-0">
                  <td className="p-0">
                    <Link
                      to={`/prenotazioni?data=${giorno.data}`}
                      className="text-corpo flex h-full items-center py-2.5 pr-4 capitalize transition-colors group-hover/riga:text-primary"
                    >
                      {giorno.giornoNome}
                    </Link>
                  </td>
                  <td className="text-corpo py-2.5 pr-4 text-right tabular-nums">
                    {giorno.numeroPrenotazioni}
                  </td>
                  <td className="py-2.5 pr-4">
                    {giorno.capienzaGiorno > 0 ? (
                      <div className="flex items-center gap-2">
                        <span className="text-nota tabular-nums whitespace-nowrap text-muted-foreground">
                          {giorno.numeroCoperti}/{giorno.capienzaGiorno}
                        </span>
                        <BandaCoperti
                          prenotati={giorno.numeroCoperti}
                          capienza={giorno.capienzaGiorno}
                          className="w-20"
                        />
                        {giorno.nonPresentate > 0 && (
                          <span className="text-nota whitespace-nowrap text-muted-foreground">
                            · {giorno.nonPresentate} non presentate
                          </span>
                        )}
                      </div>
                    ) : (
                      <span className="text-nota text-muted-foreground">—</span>
                    )}
                  </td>
                  <td
                    className={cn(
                      'text-corpo py-2.5 text-right tabular-nums',
                      giorno.annullate > 0 ? 'text-muted-foreground' : 'text-muted-foreground/50'
                    )}
                  >
                    {giorno.annullate}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>
    </div>
  )
}
