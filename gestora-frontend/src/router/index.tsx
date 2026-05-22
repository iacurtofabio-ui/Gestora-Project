  import { createBrowserRouter } from 'react-router-dom'
  import ProtectedRoute from './ProtectedRoute'
  import LoginPage from '@/pages/LoginPage'
  import AppLayout from '@/layouts/AppLayout'
  import { Button } from '@/components/ui/button'

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
        element: (
          <ProtectedRoute allowedRoles={['Admin', 'Staff']}>
            <AppLayout />
          </ProtectedRoute>
        ),
        children: [
          { path: '/dashboard', element: <DashboardPage /> },
          { path: '/zone', element: <ZonePage /> },
          { path: '/postazioni', element: <PostazionePage /> },
          { path: '/fasce/orarie', element: <FasceOrariePage /> },
          { path: '/prenotazioni', element: <PrenotazioniPage /> },     
        ],
      },

      {
        element: (
          <ProtectedRoute allowedRoles={['Admin']}>
            <AppLayout />
          </ProtectedRoute>
        ),
        children: [
          { path: '/admin-utenti', element: <AdminUtentiPage /> },
        ],
      },

            {
        element: (
          <ProtectedRoute allowedRoles={['Admin', 'Staff', 'Cliente']}>
            <AppLayout />
          </ProtectedRoute>
        ),
        children: [
          { path: '/prenotazioni/nuova', element: <NuovaPrenotazionePage /> },
        ],
      },
  ])