import { Link } from 'react-router-dom'
import { useAuth } from '@/hooks/useAuth'
import { Button } from '@/components/ui/button'

// REV-080: prima era un vicolo cieco, senza alcun link di ritorno — l'unica via d'uscita era
// modificare l'URL a mano. La home dipende dal ruolo: Staff/Admin partono dalla Dashboard, il
// Cliente non ci ha accesso e riparte da Prenotazioni.
export default function UnauthorizedPage() {
  const { user } = useAuth()
  const isStaffOrAdmin = user?.roles.includes('Admin') || user?.roles.includes('Staff')
  const home = isStaffOrAdmin ? '/dashboard' : '/prenotazioni'

  return (
    <div className="min-h-screen flex items-center justify-center p-6 bg-gray-50">
      <div className="bg-white border rounded-lg p-8 max-w-md w-full text-center">
        <h1 className="text-lg font-semibold mb-2">Accesso non autorizzato</h1>
        <p className="text-sm text-gray-600 mb-6">
          Non hai i permessi necessari per accedere a questa pagina.
        </p>
        <Button asChild>
          <Link to={home}>Torna alla home</Link>
        </Button>
      </div>
    </div>
  )
}
