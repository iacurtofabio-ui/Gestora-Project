import { Loader2Icon, TriangleAlertIcon } from 'lucide-react'
import { messaggioErroreCaricamento } from '@/lib/apiError'
import { Skeleton } from '@/components/ui/skeleton'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'

/**
 * REV-074 — prima ogni pagina, al primo caricamento, sostituiva l'intera vista con
 * `<div>Caricamento...</div>`: header, filtri e pulsanti sparivano insieme alla tabella. Va
 * mostrato solo al posto del contenuto che sta ancora arrivando, con l'intestazione della
 * pagina gia' a schermo.
 *
 * Resta per i casi in cui non si sa che forma avra' il contenuto. Dove la forma e' nota conviene
 * uno scheletro (vedi sotto).
 */
export function PageLoading() {
  return (
    <div className="text-corpo flex items-center justify-center gap-2 p-10 text-muted-foreground">
      <Loader2Icon className="size-4 animate-spin" />
      Caricamento...
    </div>
  )
}

/**
 * NEW-006 — il messaggio distingue gia' la causa (server spento, permessi, errore del database)
 * riusando `messaggioErroreCaricamento`.
 *
 * Direzione «Turno» — prima era una riga di testo rosso centrata in mezzo al vuoto: sembrava un
 * messaggio di sistema, non una parte dell'interfaccia. Ora e' un blocco allineato a sinistra
 * come tutto il resto, con il colore usato sul bordo e sull'icona invece che sul testo (il rosso
 * su fondo chiaro si legge peggio del testo normale, e qui c'e' da leggere).
 *
 * L'errore dice che cosa e' successo e, quando si puo', che cosa fare: `onRiprova` aggiunge il
 * modo di rimediare senza ricaricare la pagina.
 */
export function PageError({
  error,
  fallback,
  onRiprova,
  className,
}: {
  error: unknown
  fallback: string
  onRiprova?: () => void
  className?: string
}) {
  return (
    <div
      role="alert"
      className={cn(
        'm-4 flex items-start gap-3 rounded-lg border border-destructive/30 bg-destructive/5 p-4',
        className
      )}
    >
      <TriangleAlertIcon className="mt-0.5 size-4 shrink-0 text-destructive" />
      <div className="min-w-0 flex-1 space-y-2">
        <p className="text-sezione text-foreground">Non riesco a caricare i dati</p>
        <p className="text-corpo text-muted-foreground">
          {messaggioErroreCaricamento(error, fallback)}
        </p>
        {onRiprova && (
          <Button size="sm" variant="outline" onClick={onRiprova}>
            Riprova
          </Button>
        )}
      </div>
    </div>
  )
}

/**
 * Scheletro di una tabella.
 *
 * Direzione «Turno» — `colonne` accetta le LARGHEZZE reali, non piu' solo un numero. Uno
 * scheletro fatto di barre tutte uguali promette una forma e ne consegna un'altra: quando i dati
 * arrivano la pagina si riassesta, che e' esattamente il salto che lo scheletro doveva evitare.
 * Passando `['7rem','1fr','8rem']` le barre stanno dove staranno le colonne.
 *
 * Il numero semplice resta ammesso per le pagine non ancora ridisegnate.
 */
export function TableSkeleton({
  righe = 6,
  colonne = 5,
}: {
  righe?: number
  colonne?: number | string[]
}) {
  const larghezze = Array.isArray(colonne) ? colonne : Array.from({ length: colonne }, () => '1fr')
  const griglia = { gridTemplateColumns: larghezze.join(' ') }

  return (
    <div className="space-y-2" aria-hidden="true">
      <div className="grid gap-4 border-b pb-3" style={griglia}>
        {larghezze.map((_, i) => (
          <Skeleton key={i} className="h-3 rounded-xs" />
        ))}
      </div>
      {Array.from({ length: righe }).map((_, r) => (
        <div key={r} className="grid items-center gap-4 py-1.5" style={griglia}>
          {larghezze.map((_, c) => (
            <Skeleton key={c} className="h-4 rounded-xs" />
          ))}
        </div>
      ))}
    </div>
  )
}

/**
 * Scheletro della Dashboard, nella forma nuova: la banda grande in cima, l'elenco delle fasce,
 * i due numeri della settimana. Le altezze sono quelle vere, cosi' il contenuto non fa saltare
 * la pagina quando arriva.
 */
export function DashboardSkeleton() {
  return (
    <div className="space-y-8" aria-hidden="true">
      <div className="space-y-6">
        <Skeleton className="h-7 w-56 rounded-xs" />
        <div className="space-y-3">
          <Skeleton className="h-12 w-40 rounded-xs" />
          <Skeleton className="h-3 w-full rounded-xs" />
        </div>
      </div>

      <div className="space-y-3">
        <Skeleton className="h-4 w-24 rounded-xs" />
        {Array.from({ length: 4 }).map((_, i) => (
          <div key={i} className="flex items-center gap-4 py-2">
            <Skeleton className="h-4 w-24 shrink-0 rounded-xs" />
            <Skeleton className="h-2 flex-1 rounded-xs" />
            <Skeleton className="h-4 w-20 shrink-0 rounded-xs" />
          </div>
        ))}
      </div>

      <div className="grid grid-cols-2 gap-px overflow-hidden rounded-xl border bg-border sm:grid-cols-4">
        {Array.from({ length: 4 }).map((_, i) => (
          <div key={i} className="space-y-2 bg-card p-4">
            <Skeleton className="h-3 w-24 rounded-xs" />
            <Skeleton className="h-7 w-12 rounded-xs" />
          </div>
        ))}
      </div>
    </div>
  )
}

/**
 * L'errore di sistema dentro un form: credenziali rifiutate, server irraggiungibile, conflitto.
 *
 * Non e' un errore di validazione — quelli stanno sotto il campo che li ha causati, e li scrive
 * gia' react-hook-form. Questo riguarda l'invio nel suo insieme, quindi sta sopra il pulsante,
 * dove si guarda dopo aver premuto.
 *
 * Prima era una riga di testo rosso indistinguibile da un errore di campo: stessa dimensione,
 * stesso colore, in mezzo agli altri. Qui ha un contorno, cosi' si vede che parla di tutto il
 * form e non dell'ultimo campo.
 */
export function ErroreForm({ messaggio }: { messaggio?: string }) {
  if (!messaggio) return null

  return (
    <p
      role="alert"
      className="text-corpo flex items-start gap-2 rounded-md border border-destructive/30 bg-destructive/5 p-3"
    >
      <TriangleAlertIcon className="mt-0.5 size-4 shrink-0 text-destructive" />
      <span className="text-pretty">{messaggio}</span>
    </p>
  )
}
