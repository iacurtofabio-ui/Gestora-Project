import type { ReactNode } from 'react'
import { EllipsisIcon } from 'lucide-react'
import { Button } from '@/components/ui/button'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'

/**
 * Le azioni di una riga di tabella.
 *
 * IL PROBLEMA CHE RISOLVE
 * Ogni pagina a elenco aveva la stessa colonna Azioni fatta di pulsanti affiancati tutti uguali:
 * due su Zone, due su Postazioni, due su Fasce, **quattro piu' l'eliminazione** su Utenti, cinque
 * su Prenotazioni. Tutti della stessa misura, con l'azione irreversibile a un centimetro da
 * quella che si usa cento volte al giorno. Cinque cose uguali non sono cinque scelte: sono
 * nessuna gerarchia.
 *
 * LA REGOLA
 * 1. Al massimo UNA azione in chiaro, quella che si usa quasi sempre.
 * 2. Tutto il resto nel menu «…»: azioni legittime ma occasionali.
 * 3. L'azione distruttiva sta nel menu, sotto un separatore, in rosso. Non e' un pulsante
 *    fratello degli altri: e' l'unica che non si puo' rifare.
 *
 * L'azione in chiaro non e' un pulsante pieno: il pieno resta all'azione primaria della PAGINA,
 * che e' una sola. Vedi la variante `azione` in ui/button.tsx. Perche' reagisca al passaggio del
 * mouse sull'intera riga, la `<TableRow>` che la contiene deve avere `className="group/riga"`.
 */

export type VoceAzione = {
  etichetta: string
  onSelect: () => void
  disabilitata?: boolean
}

type Props = {
  /** Che cosa si sta agendo, per l'etichetta di accessibilita': "la zona Dehors", "Mario Rossi". */
  descrizione: string
  azionePrimaria?: VoceAzione & { inCorso?: boolean; icona?: ReactNode }
  voci?: VoceAzione[]
  distruttiva?: VoceAzione
}

export function AzioniRiga({ descrizione, azionePrimaria, voci = [], distruttiva }: Props) {
  const vociVisibili = voci.filter(Boolean)
  const haMenu = vociVisibili.length > 0 || distruttiva !== undefined

  // Niente da fare su questa riga: un trattino dice "e' normale", un menu vuoto direbbe
  // "qualcosa non ha funzionato".
  if (!azionePrimaria && !haMenu) {
    return <span className="text-muted-foreground">—</span>
  }

  return (
    <div className="flex items-center justify-end gap-1">
      {azionePrimaria && (
        <Button
          size="sm"
          variant="azione"
          onClick={azionePrimaria.onSelect}
          disabled={azionePrimaria.disabilitata || azionePrimaria.inCorso}
        >
          {azionePrimaria.icona}
          {azionePrimaria.etichetta}
        </Button>
      )}

      {haMenu && (
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button
              size="icon-sm"
              variant="azioneMenu"
              aria-label={`Altre azioni per ${descrizione}`}
            >
              <EllipsisIcon />
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end" className="w-56">
            {vociVisibili.map((voce) => (
              <DropdownMenuItem
                key={voce.etichetta}
                disabled={voce.disabilitata}
                onSelect={voce.onSelect}
              >
                {voce.etichetta}
              </DropdownMenuItem>
            ))}
            {distruttiva && (
              <>
                {vociVisibili.length > 0 && <DropdownMenuSeparator />}
                <DropdownMenuItem
                  variant="destructive"
                  disabled={distruttiva.disabilitata}
                  onSelect={distruttiva.onSelect}
                >
                  {distruttiva.etichetta}
                </DropdownMenuItem>
              </>
            )}
          </DropdownMenuContent>
        </DropdownMenu>
      )}
    </div>
  )
}
