import { Link } from 'react-router-dom'
import { useAuth } from '@/hooks/useAuth'
import { paginaDiCasa } from '@/lib/navigazione'
import { Button } from '@/components/ui/button'
import { SchermataMessaggio } from '@/components/SchermataMessaggio'

/**
 * Nessuna rotta `*` esisteva: un indirizzo sbagliato o un vecchio link finiva sull'error boundary
 * generico di React Router, con lo stesso tono di un errore vero. Un 404 non lo è: è solo un
 * indirizzo che non esiste, e non serve autenticazione per dirlo.
 */
export default function NotFoundPage() {
  const { user, isAuthenticated } = useAuth()
  const destinazione = isAuthenticated ? paginaDiCasa(user!.roles) : '/'

  return (
    <SchermataMessaggio
      titolo="Questa pagina non esiste"
      azioni={
        <Button asChild>
          <Link to={destinazione}>
            {isAuthenticated ? 'Torna alle tue pagine' : 'Torna alla pagina del locale'}
          </Link>
        </Button>
      }
    >
      <p>L'indirizzo non corrisponde a nessuna pagina di Gestora.</p>
    </SchermataMessaggio>
  )
}
