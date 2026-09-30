import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '@/hooks/useAuth'
import { paginaDiCasa } from '@/lib/navigazione'
import { Button } from '@/components/ui/button'
import { SchermataMessaggio } from '@/components/SchermataMessaggio'

/**
 * REV-080: prima era un vicolo cieco, senza alcun link di ritorno - l'unica via d'uscita era
 * modificare l'URL a mano. La home dipende dal ruolo (`paginaDiCasa`).
 *
 * Il tono e' neutro e non rosso: non e' un guasto, e' una porta che per questo ruolo non si apre.
 *
 * Caso a parte: l'utente **senza nessun ruolo** (l'Admin glieli ha tolti tutti). Per lui non c'e'
 * una pagina di casa: "Torna alle tue pagine" lo rimandava a /prenotazioni, ProtectedRoute lo
 * rispediva qui, e cosi' all'infinito. L'unica uscita sensata e' il logout.
 */
export default function UnauthorizedPage() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()
  const senzaRuolo = !user || user.roles.length === 0

  function esci() {
    logout()
    navigate('/login', { replace: true })
  }

  if (senzaRuolo) {
    return (
      <SchermataMessaggio
        titolo="Il tuo account non ha ancora un ruolo"
        azioni={<Button onClick={esci}>Esci</Button>}
      >
        <p>Chiedi a un amministratore di assegnartene uno, poi accedi di nuovo.</p>
      </SchermataMessaggio>
    )
  }

  return (
    <SchermataMessaggio
      titolo="Questa pagina non e' aperta al tuo ruolo"
      azioni={
        <Button asChild>
          <Link to={paginaDiCasa(user.roles)}>Torna alle tue pagine</Link>
        </Button>
      }
    >
      <p>
        Serve un ruolo diverso da quello che hai adesso ({user.roles.join(', ')}). Se ti serve
        accedere, chiedi a un amministratore di aggiungerti il ruolo giusto.
      </p>
    </SchermataMessaggio>
  )
}
