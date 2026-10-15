import { Logo } from '@/components/Logo'
import { SchermataMessaggio } from '@/components/SchermataMessaggio'

/**
 * V2-016 — l'unica schermata del portale mentre è in manutenzione (vedi `lib/manutenzione.ts`).
 * La legge chiunque apra l'indirizzo, quindi niente termini tecnici: cosa succede e che si torna.
 */
export default function PortaleInManutenzione() {
  return (
    <SchermataMessaggio titolo="Gestora è in manutenzione">
      <Logo />
      <p>Stiamo preparando una nuova versione del portale. Torna a trovarci presto.</p>
    </SchermataMessaggio>
  )
}
