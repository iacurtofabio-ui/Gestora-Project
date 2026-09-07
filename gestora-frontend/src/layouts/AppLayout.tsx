import { useEffect, useState } from 'react'
import { Outlet, NavLink, useNavigate, useLocation } from 'react-router-dom'
import { useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { MenuIcon, XIcon } from 'lucide-react'
import { useAuth } from '@/hooks/useAuth'
import { onSessionExpired } from '@/lib/session'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'

function linkClass({ isActive }: { isActive: boolean }) {
  // REV-081: prima la sidebar non segnalava in che pagina ci si trovasse.
  return cn(
    'px-3 py-2 rounded text-sm font-medium',
    isActive ? 'bg-gray-100 text-gray-900' : 'text-gray-700 hover:bg-gray-100'
  )
}

export default function AppLayout() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const queryClient = useQueryClient()
  // REV-071: la sidebar era fissa (w-64, sempre a schermo): su smartphone occupava la metà dello
  // schermo utile. Su mobile parte chiusa e si apre come pannello sopra il contenuto; da md in su
  // resta sempre visibile, come prima.
  const [menuAperto, setMenuAperto] = useState(false)

  // Un cambio di pagina chiude il pannello mobile: altrimenti restava aperto sopra la pagina
  // appena raggiunta. Aggiornamento durante il render (non in un effetto): e' il pattern che
  // React consiglia per "adeguare lo stato quando cambia un input", niente giro in piu' di commit.
  const [pathnamePrecedente, setPathnamePrecedente] = useState(location.pathname)
  if (location.pathname !== pathnamePrecedente) {
    setPathnamePrecedente(location.pathname)
    setMenuAperto(false)
  }

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
        toast.error("Sessione scaduta. Effettua di nuovo l'accesso.")
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

  const sidebarContent = (
    <nav className="flex flex-col p-4 gap-2">
      {links.map((link) => (
        <NavLink key={link.to} to={link.to} className={linkClass}>
          {link.label}
        </NavLink>
      ))}
      {user?.roles.includes('Admin') && (
        <NavLink to="/admin-utenti" className={linkClass}>
          Admin Utenti
        </NavLink>
      )}
    </nav>
  )

  return (
    <div className="flex h-screen bg-gray-50">
      {/* Sidebar da desktop: sempre visibile, come prima. */}
      <aside className="hidden md:block w-64 bg-white border-r shrink-0">{sidebarContent}</aside>

      {/* Pannello mobile: overlay scuro + sidebar sopra il contenuto, apribile dall'header. */}
      {menuAperto && (
        <div className="md:hidden fixed inset-0 z-40 flex">
          <div className="fixed inset-0 bg-black/40" onClick={() => setMenuAperto(false)} />
          <aside className="relative z-50 w-64 bg-white border-r h-full flex flex-col">
            <div className="h-16 flex items-center justify-between px-4 border-b">
              <span className="font-semibold text-gray-800">Gestora</span>
              <Button
                variant="ghost"
                size="icon-sm"
                onClick={() => setMenuAperto(false)}
                aria-label="Chiudi menu"
              >
                <XIcon className="h-5 w-5" />
              </Button>
            </div>
            {sidebarContent}
          </aside>
        </div>
      )}

      <div className="flex flex-col flex-1 min-w-0">
        <header className="h-16 bg-white border-b px-4 md:px-6 flex items-center justify-between gap-4">
          <div className="flex items-center gap-3 min-w-0">
            <Button
              variant="ghost"
              size="icon-sm"
              className="md:hidden"
              onClick={() => setMenuAperto(true)}
              aria-label="Apri menu"
            >
              <MenuIcon className="h-5 w-5" />
            </Button>
            {/* REV-081: prima l'header mostrava solo l'email, senza il ruolo — a colpo d'occhio
                non si distingueva un Cliente da uno Staff. */}
            <div className="min-w-0">
              <p className="text-sm text-gray-800 truncate">{user?.email}</p>
              <p className="text-xs text-gray-400 truncate">{user?.roles.join(', ')}</p>
            </div>
          </div>
          <button onClick={handleLogout} className="text-sm text-red-500 hover:underline shrink-0">
            Logout
          </button>
        </header>
        <main className="flex-1 overflow-y-auto p-4 md:p-6">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
