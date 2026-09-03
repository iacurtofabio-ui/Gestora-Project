import { createBrowserRouter, Navigate } from 'react-router-dom'
import ProtectedRoute from './ProtectedRoute'
import SetupGuard from './SetupGuard'
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

export const router = createBrowserRouter([
  { path: '/', element: <Navigate to="/login" replace /> },
  // REV-007: finche' non esiste un amministratore, l'unica pagina raggiungibile e' /setup.
  { path: '/setup', element: <SetupPage /> },
  { path: '/login', element: <SetupGuard><LoginPage /></SetupGuard> },
  { path: '/register', element: <SetupGuard><RegisterPage /></SetupGuard> },
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
      { path: '/fasce-orarie', element: <FasciaOrariaPage /> },
    ],
  },
  {
    element: (
      <ProtectedRoute allowedRoles={['Admin', 'Staff', 'Cliente']}>
        <AppLayout />
      </ProtectedRoute>
    ),
    children: [
      { path: '/prenotazioni', element: <PrenotazionePage /> },
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
  }
])