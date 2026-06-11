import { createContext, useContext, useState } from 'react'

  type AuthUser = {
    id: string
    email: string
    role: string
    token: string
  }

  type AuthContextType = {
    user: AuthUser | null
    isAuthenticated: boolean
    login: (token: string) => void
    logout: () => void
  }

  const AuthContext = createContext<AuthContextType | null>(null)

  export function AuthProvider({ children }: { children: React.ReactNode }) {
    const [user, setUser] = useState<AuthUser | null>(() => {
      const token = localStorage.getItem('token')
      if (!token) return null
      const payload = JSON.parse(atob(token.split('.')[1]))
      return { token, id: payload.sub, email: payload.email, role: payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] }
    })

    function login(token: string) {
      localStorage.setItem('token', token)
      const payload = JSON.parse(atob(token.split('.')[1]))
      setUser({ token, id: payload.sub, email: payload.email, role: payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] })
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

  export function useAuth() {
    const context = useContext(AuthContext)
    if (!context) throw new Error('useAuth must be used within AuthProvider')
    return context
  }
