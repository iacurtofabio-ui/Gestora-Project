  import { Navigate } from 'react-router-dom'

  interface Props {
    allowedRoles: string[]
    children: React.ReactNode
  }

  export default function ProtectedRoute({ allowedRoles, children }: Props) {
    const token = localStorage.getItem('token')

    if (!token) {
      return <Navigate to="/login" replace />
    }

    const payload = JSON.parse(atob(token.split('.')[1]))
    const userRole = payload.role

    if (!allowedRoles.includes(userRole)) {
      return <Navigate to="/unauthorized" replace />
    }

    return <>{children}</>
  }