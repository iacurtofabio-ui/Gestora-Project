  import { createBrowserRouter } from 'react-router-dom'
  import ProtectedRoute from './ProtectedRoute'
  import LoginPage from '@/pages/LoginPage'

  const placeholder = (name: string) => () => <div>{name}</div>

  const DashboardPage = placeholder('Dashboard')
  const ZonePage = placeholder('Zone')
  const PostazionePage = placeholder('Postazioni')
  const FasceOrariePage = placeholder('Fasce Orarie')
  const PrenotazioniPage = placeholder('Prenotazioni')
  const NuovaPrenotazionePage = placeholder('Nuova Prenotazione')
  const AdminUtentiPage = placeholder('Admin Utenti')
  const UnauthorizedPage = placeholder('Unauthorized')

  export const router = createBrowserRouter([
    { path: '/login', element: <LoginPage /> },
    { path: '/unauthorized', element: <UnauthorizedPage /> },
    {
      path: '/dashboard',
      element: (
        <ProtectedRoute allowedRoles={['Admin', 'Staff']}>
          <DashboardPage />
        </ProtectedRoute>
      ),
    },
    {
      path: '/zone',
      element: (
        <ProtectedRoute allowedRoles={['Admin', 'Staff']}>
          <ZonePage />
        </ProtectedRoute>
      ),
    },
        {
      path: '/postazioni',
      element: (
        <ProtectedRoute allowedRoles={['Admin', 'Staff']}>
          <PostazionePage />
        </ProtectedRoute>
      ),
    },
        {
      path: '/fasce/orarie',
      element: (
        <ProtectedRoute allowedRoles={['Admin', 'Staff']}>
          <FasceOrariePage />
        </ProtectedRoute>
      ),
    },
        {
      path: '/prenotazioni',
      element: (
        <ProtectedRoute allowedRoles={['Admin', 'Staff']}>
          <PrenotazioniPage />
        </ProtectedRoute>
      ),
    },
        {
      path: '/prenotazioni/nuova',
      element: (
        <ProtectedRoute allowedRoles={['Admin', 'Staff', 'Cliente']}>
          <NuovaPrenotazionePage />
        </ProtectedRoute>
      ),
    },
        {
      path: '/admin-utenti',
      element: (
        <ProtectedRoute allowedRoles={['Admin']}>
          <AdminUtentiPage />
        </ProtectedRoute>
      ),
    },
  ])