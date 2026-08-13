import { createContext } from 'react'

export type AuthUser = {
  id: string
  email: string
  roles: string[]
  token: string
}

export type AuthContextType = {
  user: AuthUser | null
  isAuthenticated: boolean
  login: (token: string) => void
  logout: () => void
}

export const AuthContext = createContext<AuthContextType | null>(null)
