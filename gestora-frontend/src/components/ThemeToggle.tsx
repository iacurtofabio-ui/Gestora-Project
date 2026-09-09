import { CheckIcon, MonitorIcon, MoonIcon, SunIcon } from 'lucide-react'
import { useTheme } from '@/hooks/useTheme'
import { Button } from '@/components/ui/button'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import type { Tema } from '@/context/theme-context'

const VOCI: { valore: Tema; etichetta: string; Icona: typeof SunIcon }[] = [
  { valore: 'chiaro', etichetta: 'Chiaro', Icona: SunIcon },
  { valore: 'scuro', etichetta: 'Scuro', Icona: MoonIcon },
  { valore: 'sistema', etichetta: 'Come il sistema', Icona: MonitorIcon },
]

/**
 * Interruttore del tema. È un menu a tre voci e non un semplice interruttore acceso/spento,
 * perché "come il sistema" è uno stato a sé: chi lo sceglie vuole che l'app segua il dispositivo,
 * non che resti bloccata su una delle due.
 */
export function ThemeToggle() {
  const { tema, temaEffettivo, impostaTema } = useTheme()
  const IconaAttuale = temaEffettivo === 'scuro' ? MoonIcon : SunIcon

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button variant="ghost" size="icon-sm" aria-label="Cambia tema">
          <IconaAttuale className="size-4" />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end">
        {VOCI.map(({ valore, etichetta, Icona }) => (
          <DropdownMenuItem
            key={valore}
            onSelect={() => impostaTema(valore)}
            // La voce scelta si distingue con il peso e con la spunta, non con una
            // campitura: lo sfondo di una voce di menu vuol dire "sei qui con il puntatore",
            // e usarlo anche per "questa e' la scelta attiva" fa dire due cose allo stesso
            // segnale. Stessa regola del Select, vedi ui/select.tsx.
            className={tema === valore ? 'font-medium' : ''}
          >
            <Icona className="size-4" />
            {etichetta}
            {/* La spunta indica la scelta dell'utente, non il tema in vigore: con "come il
                sistema" le due possono non coincidere, ed e' giusto cosi'. */}
            {tema === valore && <CheckIcon className="ml-auto size-4" />}
          </DropdownMenuItem>
        ))}
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
