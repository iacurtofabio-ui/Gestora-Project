import { Outlet, Link } from 'react-router-dom'
import { useAuth } from '@/context/AuthContext'

const linkClass = 'px-3 py-2 rounded hover:bg-gray-100 text-sm font-medium text-gray-700'

export default function AppLayout() {
  const { user, logout } = useAuth()

  const links = [
    ...(user?.role === 'Admin' || user?.role === 'Staff'
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
          {user?.role === 'Admin' && (
            <Link to="/admin-utenti" className={linkClass}>
              Admin Utenti
            </Link>
          )}
        </nav>
      </aside>
      <div className="flex flex-col flex-1">
        <header className="h-16 bg-white border-b px-6 flex items-center justify-between">
          <span className="text-sm text-gray-600">{user?.email}</span>
          <button onClick={logout} className="text-sm text-red-500 hover:underline">
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
