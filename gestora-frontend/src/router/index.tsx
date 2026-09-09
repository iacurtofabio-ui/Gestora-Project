import { createBrowserRouter } from 'react-router-dom'
import ProtectedRoute from './ProtectedRoute'
import SetupGuard from './SetupGuard'
import RedirectSeAutenticato from './RedirectSeAutenticato'
import LandingPage from '@/pages/LandingPage'
import LoginPage from '@/pages/LoginPage'
import RegisterPage from '@/pages/RegisterPage'
import AppLayout from '@/layouts/AppLayout'
import PrenotazionePage from '@/pages/PrenotazionePage'
import DashboardPage from '@/pages/DashboardPage'
import ZonePage from '@/pages/ZonePage'
import PostazionePage from '@/pages/PostazionePage'
import FasciaOrariaPage from '@/pages/FasciaOrariaPage'
import AdminUtentiPage from '@/pages/AdminUtentiPage'
import UnauthorizedPage from '@/pages/UnauthorizedPage'
import SetupPage from '@/pages/SetupPage'
import RouteErrorPage from './RouteErrorPage'

// REV-014: senza errorElement, un'eccezione dentro una pagina finisce all'error boundary interno
// di React Router, che mostra "Unexpected Application Error!" con lo stack trace. Va agganciato a
// ogni route di primo livello: l'ErrorBoundary di main.tsx copre solo cio' che sta fuori dal
// router (AuthProvider), non le pagine.
const errorElement = <RouteErrorPage />

export const router = createBrowserRouter([
  // Fase 13: la radice era un rimando secco a /login, quindi chi apriva il link trovava un form
  // di accesso senza credenziali. Ora c'e' una vetrina pubblica. Restano pero' due condizioni
  // gia' presenti prima: senza un Admin si va comunque al primo avvio (SetupGuard), e chi ha gia'
  // la sessione aperta va direttamente alla sua pagina invece che alla vetrina.
  {
    path: '/',
    element: (
      <SetupGuard>
        <RedirectSeAutenticato>
          <LandingPage />
        </RedirectSeAutenticato>
      </SetupGuard>
    ),
    errorElement,
  },
  // REV-007: finche' non esiste un amministratore, l'unica pagina raggiungibile e' /setup.
  { path: '/setup', element: <SetupPage />, errorElement },
  {
    path: '/login',
    element: (
      <SetupGuard>
        <LoginPage />
      </SetupGuard>
    ),
    errorElement,
  },
  {
    path: '/register',
    element: (
      <SetupGuard>
        <RegisterPage />
      </SetupGuard>
    ),
    errorElement,
  },
  { path: '/unauthorized', element: <UnauthorizedPage />, errorElement },

  {
    element: (
      <ProtectedRoute allowedRoles={['Admin', 'Staff']}>
        <AppLayout />
      </ProtectedRoute>
    ),
    errorElement,
    children: [
      { path: '/dashboard', element: <DashboardPage /> },
      { path: '/zone', element: <ZonePage /> },
      { path: '/postazioni', element: <PostazionePage /> },
      { path: '/fasce-orarie', element: <FasciaOrariaPage /> },
    ],
  },
  {
    element: (
      <ProtectedRoute allowedRoles={['Admin', 'Staff', 'Cliente']}>
        <AppLayout />
      </ProtectedRoute>
    ),
    errorElement,
    children: [{ path: '/prenotazioni', element: <PrenotazionePage /> }],
  },
  {
    element: (
      <ProtectedRoute allowedRoles={['Admin']}>
        <AppLayout />
      </ProtectedRoute>
    ),
    errorElement,
    children: [{ path: '/admin-utenti', element: <AdminUtentiPage /> }],
  },
])
