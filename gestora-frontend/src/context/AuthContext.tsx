import { useState } from 'react'
import { AuthContext, type AuthUser } from './auth-context'

const CLAIM_RUOLO = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'

function normalizeRoles(claim: string | string[] | undefined): string[] {
  if (!claim) return []
  return Array.isArray(claim) ? claim : [claim]
}

/**
 * REV-014 — lettura difensiva del token.
 *
 * Prima il payload veniva decodificato con `JSON.parse(atob(...))` senza alcuna protezione,
 * dentro l'inizializzatore di useState del provider che avvolge l'intero router: un token
 * troncato o manomesso in localStorage (bastava una scrittura parziale) faceva esplodere il
 * primo render, e l'app restava una schermata bianca da cui nemmeno /login era raggiungibile.
 * L'unico rimedio era svuotare localStorage dai devtools.
 *
 * Ora un token illeggibile viene semplicemente scartato: si riparte come utente anonimo, cioe'
 * dal login, che e' esattamente quello che serve. In piu' si scarta anche il token gia' scaduto
 * (claim `exp`), senza aspettare il primo 401 dal backend (REV-025).
 */
function leggiUtenteDalToken(token: string): AuthUser | null {
  try {
    const payloadBase64 = token.split('.')[1]
    if (!payloadBase64) return null

    // Il payload JWT e' base64url: '-' e '_' al posto di '+' e '/', e senza padding.
    const base64 = payloadBase64.replace(/-/g, '+').replace(/_/g, '/')
    const payload = JSON.parse(atob(base64))

    // `exp` e' in secondi dall'epoca UTC: nessun problema di fuso, e' un istante assoluto.
    if (typeof payload.exp === 'number' && payload.exp * 1000 <= Date.now()) return null
    if (!payload.sub) return null

    return {
      token,
      id: payload.sub,
      email: payload.email,
      roles: normalizeRoles(payload[CLAIM_RUOLO]),
    }
  } catch {
    return null
  }
}

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(() => {
    const token = localStorage.getItem('token')
    if (!token) return null

    const utente = leggiUtenteDalToken(token)
    // Token inutilizzabile: si ripulisce subito, altrimenti resterebbe li' a far fallire ogni
    // avvio successivo e a farsi allegare come Authorization da ogni chiamata.
    if (!utente) localStorage.removeItem('token')
    return utente
  })

  function login(token: string) {
    const utente = leggiUtenteDalToken(token)
    if (!utente) {
      localStorage.removeItem('token')
      setUser(null)
      throw new Error('Il token ricevuto dal server non e\' valido o e\' gia\' scaduto.')
    }
    localStorage.setItem('token', token)
    setUser(utente)
    return utente
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
