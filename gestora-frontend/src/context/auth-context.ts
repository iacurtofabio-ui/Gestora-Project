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
  /**
   * Restituisce l'utente decodificato dal token, cosi' chi effettua l'accesso non deve
   * ridecodificarlo per conto proprio (REV-014). Solleva un'eccezione se il token e' illeggibile
   * o gia' scaduto.
   */
  login: (token: string) => AuthUser
  logout: () => void
}

export const AuthContext = createContext<AuthContextType | null>(null)
