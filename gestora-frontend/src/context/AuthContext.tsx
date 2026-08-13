import { useState } from 'react'
import { AuthContext, type AuthUser } from './auth-context'

function normalizeRoles(claim: string | string[] | undefined): string[] {
  if (!claim) return []
  return Array.isArray(claim) ? claim : [claim]
}

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(() => {
    const token = localStorage.getItem('token')
    if (!token) return null
    const payload = JSON.parse(atob(token.split('.')[1]))
    return {
      token, id: payload.sub, email: payload.email, roles: normalizeRoles(payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'])
    }
  })

  function login(token: string) {
    localStorage.setItem('token', token)
    const payload = JSON.parse(atob(token.split('.')[1]))
    setUser({
      token, id: payload.sub, email: payload.email, roles: normalizeRoles(payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'])
    })
  }

  function logout() {
    localStorage.removeItem('token')
    setUser(null)
  }

  return (
    <AuthContext.Provider value={{ user, isAuthenticated: !!user, login, logout }}>
      {children}
    </AuthContext.Provider>
  )
}