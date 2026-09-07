import { Loader2Icon } from 'lucide-react'
import { messaggioErroreCaricamento } from '@/lib/apiError'

/**
 * REV-074 — prima ogni pagina, al primo caricamento, sostituiva l'intera vista con
 * `<div>Caricamento...</div>`: header, filtri e pulsanti sparivano insieme alla tabella. Va
 * mostrato solo al posto del contenuto che sta ancora arrivando, con l'intestazione della
 * pagina già a schermo.
 */
export function PageLoading() {
  return (
    <div className="flex items-center justify-center gap-2 p-10 text-sm text-gray-500">
      <Loader2Icon className="h-4 w-4 animate-spin" />
      Caricamento...
    </div>
  )
}

/**
 * NEW-006 — prima ogni pagina mostrava lo stesso "Errore nel caricamento" a prescindere dalla
 * causa: server spento, permessi mancanti o database in errore erano indistinguibili. Riusa
 * `messaggioErroreCaricamento`, già scritto e coperto da test in Fase 8/9 per gli errori delle
 * mutation.
 */
export function PageError({ error, fallback }: { error: unknown; fallback: string }) {
  return (
    <div className="p-10 text-center text-sm text-red-600">
      {messaggioErroreCaricamento(error, fallback)}
    </div>
  )
}
