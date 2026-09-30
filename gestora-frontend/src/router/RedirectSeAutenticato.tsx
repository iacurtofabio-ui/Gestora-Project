import { Navigate } from 'react-router-dom'
import { useAuth } from '@/hooks/useAuth'
import { paginaDiCasa } from '@/lib/navigazione'

/**
 * Fase 13 — chi ha già la sessione aperta e digita `/` non deve vedere la vetrina, ma la propria
 * pagina di lavoro. La destinazione è la stessa scelta che fa `LoginPage` dopo l'accesso: il
 * Cliente non ha accesso alla Dashboard, quindi va sulle sue prenotazioni.
 */
export default function RedirectSeAutenticato({ children }: { children: React.ReactNode }) {
  const { user, isAuthenticated } = useAuth()

  if (isAuthenticated && user) {
    return <Navigate to={paginaDiCasa(user.roles)} replace />
  }

  return <>{children}</>
}
