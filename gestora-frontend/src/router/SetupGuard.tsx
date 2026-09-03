import { Navigate } from 'react-router-dom'
import { useSetupStato } from '@/hooks/useSetup'

type Props = {
  children: React.ReactNode
}

/**
 * REV-007 — finché l'installazione non ha un amministratore, le pagine pubbliche (login e
 * registrazione) rimandano alla schermata di primo avvio: senza un Admin non c'è nulla da fare
 * nell'app. Appena l'Admin esiste la guardia diventa trasparente e non si vede mai più.
 */
export default function SetupGuard({ children }: Props) {
  const { data, isPending, isError } = useSetupStato()

  if (isPending) return null

  // In errore (backend irraggiungibile) si prosegue: il login mostrerà l'errore vero, invece
  // di far credere che l'installazione sia da configurare.
  if (!isError && data && !data.setupCompletato) {
    return <Navigate to="/setup" replace />
  }

  return <>{children}</>
}
