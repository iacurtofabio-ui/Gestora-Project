import { Link } from 'react-router-dom'
import { useDashboardGiornaliera, useDashboardSettimanale } from '@/hooks/useDashboard'
import {
  oggiInItalia,
  lunediSettimanaCorrenteInItalia,
  dataEstesaInItalia,
} from '@/lib/date'
import { PageError, DashboardSkeleton } from '@/components/PageState'
import { BandaCoperti } from '@/components/BandaCoperti'
import { coloreQuota } from '@/lib/coperti'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'
import type { CopertiFasciaDTO } from '@/types/dashboard'

/**
 * Direzione «Turno» — la sala vista nel tempo.
 *
 * Prima questa pagina era quattro card identiche con dentro un numero, piu' due card identiche
 * con dentro due tabelle identiche: sei contenitori dello stesso peso, e il dato che conta
 * davvero durante il servizio — quanto manca al tetto di coperti — era un numero in una cella
 * come tutti gli altri.
 *
 * Ora l'audacia si spende in un punto solo: la banda dei coperti. Tutto il resto attorno e'
 * disciplinato e silenzioso — nessuna card, solo una griglia di numeri separati da filetti e una
 * tabella settimanale che non alza la voce.
 *
 * Nessuna chiamata nuova: `copertiPerFascia` conteneva gia' `maxCoperti` e `copertiPrenotati`.
 * La capienza del giorno e' la loro somma, calcolata qui perche' e' presentazione.
 */

/** Il passo della cascata all'ingresso: le bande partono sfalsate di 40ms l'una dall'altra. */
const PASSO_CASCATA_MS = 40

/** La banda grande parte per prima; le fasce la seguono. */
const RITARDO_FASCE_MS = 120

function RigaFascia({
  fascia,
  indice,
  inAggiornamento,
}: {
  fascia: CopertiFasciaDTO
  indice: number
  inAggiornamento: boolean
}) {
  const esaurita = fascia.copertiDisponibili === 0

  return (
    <li
      className={cn(
        'grid grid-cols-[auto_1fr] items-center gap-x-4 gap-y-2 border-b py-3 last:border-b-0',
        // Sotto i 640px la banda va a capo e prende tutta la larghezza: su 375px, in linea,
        // le resterebbero ~230px fra orario e numeri, e una banda piu' corta del testo che
        // accompagna smette di essere l'elemento portante della schermata.
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
      </span>

      <BandaCoperti
        prenotati={fascia.copertiPrenotati}
        capienza={fascia.maxCoperti}
        ritardoMs={RITARDO_FASCE_MS + indice * PASSO_CASCATA_MS}
        inAggiornamento={inAggiornamento}
        className="col-span-2 sm:col-span-1 sm:order-2"
      />
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

export default function DashboardPage() {
  // REV-016: entrambe le date sono calcolate in ora italiana, non in UTC.
  const oggi = oggiInItalia()
  const giornaliera = useDashboardGiornaliera(oggi)
  const settimanale = useDashboardSettimanale(lunediSettimanaCorrenteInItalia())

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

  return (
    <div className="mx-auto max-w-5xl space-y-10">
      {/* ------------------------------------------------------------------
          Il momento orchestrato: la banda del giorno.
          E' l'unica cosa in pagina che si muove, e si muove una volta sola.
          ------------------------------------------------------------------ */}
      <section className="space-y-4">
        <div className="flex flex-wrap items-baseline justify-between gap-x-4 gap-y-1">
          <h1 className="text-titolo">Oggi in sala</h1>
          <p className="text-corpo text-muted-foreground first-letter:uppercase">
            {dataEstesaInItalia(oggi)}
          </p>
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
                {/* Il residuo sta qui, come dato secondario accanto alla didascalia, e non come
                    numero grande: da readout duplicherebbe a parole la porzione vuota della
                    banda, che si vede gia'. Tre casi distinti perche' "mancano N" da solo si
                    rompe a zero e non sa dire l'overbooking. */}
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
            <p className="text-sezione">Per oggi non c'e' nessuna fascia oraria</p>
            <p className="text-corpo mt-1 mb-4 text-muted-foreground text-pretty">
              Senza fasce non c'e' un tetto di coperti da riempire, e la sala non accetta
              prenotazioni. Puoi configurarle adesso.
            </p>
            <Button asChild size="sm">
              <Link to="/fasce-orarie">Configura le fasce orarie</Link>
            </Button>
          </div>
        )}
      </section>

      {/* ------------------------------------------------------------------
          Le fasce, una per riga. Nessuna card: la riga E' la banda.
          ------------------------------------------------------------------ */}
      {fasce.length > 0 && (
        <section className="space-y-1">
          <h2 className="text-sezione text-muted-foreground">Le fasce di oggi</h2>
          <ul>
            {fasce.map((fascia, i) => (
              <RigaFascia
                key={fascia.fasciaOrariaId}
                fascia={fascia}
                indice={i}
                inAggiornamento={inAggiornamento}
              />
            ))}
          </ul>
        </section>
      )}

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
                <th className="text-nota py-2 pr-4 text-right font-medium text-muted-foreground">
                  Coperti
                </th>
                <th className="text-nota py-2 text-right font-medium text-muted-foreground">
                  Annullate
                </th>
              </tr>
            </thead>
            <tbody>
              {settimanale.data?.giorni.map((giorno) => (
                <tr key={giorno.data} className="border-b last:border-b-0">
                  <td className="text-corpo py-2.5 pr-4 capitalize">{giorno.giornoNome}</td>
                  <td className="text-corpo py-2.5 pr-4 text-right tabular-nums">
                    {giorno.numeroPrenotazioni}
                  </td>
                  <td className="text-corpo py-2.5 pr-4 text-right tabular-nums">
                    {giorno.numeroCoperti}
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
