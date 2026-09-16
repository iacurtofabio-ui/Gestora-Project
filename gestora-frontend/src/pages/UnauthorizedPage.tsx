import { Link } from 'react-router-dom'
import { useAuth } from '@/hooks/useAuth'
import { Button } from '@/components/ui/button'
import { SchermataMessaggio } from '@/components/SchermataMessaggio'

/**
 * REV-080: prima era un vicolo cieco, senza alcun link di ritorno — l'unica via d'uscita era
 * modificare l'URL a mano. La home dipende dal ruolo: Staff/Admin partono dalla Dashboard, il
 * Cliente non ci ha accesso e riparte da Prenotazioni.
 *
 * Il tono e' neutro e non rosso: non e' un guasto, e' una porta che per questo ruolo non si apre.
 */
export default function UnauthorizedPage() {
  const { user } = useAuth()
  const isStaffOrAdmin = user?.roles.includes('Admin') || user?.roles.includes('Staff')
  const home = isStaffOrAdmin ? '/dashboard' : '/prenotazioni'

  return (
    <SchermataMessaggio
      titolo="Questa pagina non e' aperta al tuo ruolo"
      azioni={
        <Button asChild>
          <Link to={home}>Torna alle tue pagine</Link>
        </Button>
      }
    >
      <p>
        Serve un ruolo diverso da quello che hai adesso
        {user?.roles.length ? ` (${user.roles.join(', ')})` : ''}. Se ti serve accedere, chiedi a
        un amministratore di aggiungerti il ruolo giusto.
      </p>
    </SchermataMessaggio>
  )
}
