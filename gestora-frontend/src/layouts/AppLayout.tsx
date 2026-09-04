import { useEffect } from 'react'
import { Outlet, Link, useNavigate } from 'react-router-dom'
import { useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { useAuth } from '@/hooks/useAuth'
import { onSessionExpired } from '@/lib/session'

const linkClass = 'px-3 py-2 rounded hover:bg-gray-100 text-sm font-medium text-gray-700'

export default function AppLayout() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()
  const queryClient = useQueryClient()

  // REV-025 — scadenza della sessione gestita in modo pulito.
  // Il token e' gia' stato rimosso dall'interceptor: qui si allinea lo stato di React (logout),
  // si svuota la cache delle query per non lasciare i dati del vecchio utente in memoria, si
  // spiega all'utente cosa e' successo e si naviga al login senza ricaricare la pagina.
  // Il gestore vive in AppLayout perche' avvolge tutte le pagine autenticate: sono le uniche da
  // cui puo' arrivare un 401 di sessione scaduta.
  useEffect(
    () =>
      onSessionExpired(() => {
        logout()
        queryClient.clear()
        toast.error('Sessione scaduta. Effettua di nuovo l\'accesso.')
        navigate('/login', { replace: true })
      }),
    // logout e' ricreata a ogni render del provider: la si esclude di proposito, il gestore non
    // deve essere riagganciato di continuo e la funzione e' comunque sempre la stessa nei fatti.
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [navigate, queryClient]
  )

  function handleLogout() {
    logout()
    queryClient.clear()
    navigate('/login', { replace: true })
  }

  const links = [
    ...(user?.roles.includes('Admin') || user?.roles.includes('Staff')
      ? [
        { to: '/dashboard', label: 'Dashboard' },
        { to: '/zone', label: 'Zone' },
        { to: '/postazioni', label: 'Postazioni' },
        { to: '/fasce-orarie', label: 'Fasce Orarie' },
      ]
      : []),
    { to: '/prenotazioni', label: 'Prenotazioni' },
  ]

  return (
    <div className="flex h-screen bg-gray-50">
      <aside className="w-64 bg-white border-r">
        <nav className="flex flex-col p-4 gap-2">
          {links.map((link) => (
            <Link key={link.to} to={link.to} className={linkClass}>
              {link.label}
            </Link>
          ))}
          {user?.roles.includes('Admin') && (
            <Link to="/admin-utenti" className={linkClass}>
              Admin Utenti
            </Link>
          )}
        </nav>
      </aside>
      <div className="flex flex-col flex-1">
        <header className="h-16 bg-white border-b px-6 flex items-center justify-between">
          <span className="text-sm text-gray-600">{user?.email}</span>
          <button onClick={handleLogout} className="text-sm text-red-500 hover:underline">
            Logout
          </button>
        </header>
        <main className="flex-1 overflow-y-auto p-6">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
