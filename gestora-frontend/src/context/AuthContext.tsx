import { useState } from 'react'
import { AuthContext, type AuthUser } from './auth-context'
import { decodificaPayloadJwt, tokenScaduto } from '@/lib/jwt'

const CLAIM_RUOLO = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'

function normalizeRoles(claim: unknown): string[] {
  if (!claim) return []
  if (Array.isArray(claim)) return claim.filter((r): r is string => typeof r === 'string')
  return typeof claim === 'string' ? [claim] : []
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
 *
 * REV-050: la decodifica vera e propria sta ora in lib/jwt, unico punto del progetto che legge un
 * token. Qui resta solo la traduzione da payload a utente dell'applicazione.
 */
function leggiUtenteDalToken(token: string): AuthUser | null {
  const payload = decodificaPayloadJwt(token)
  if (!payload) return null
  if (tokenScaduto(payload)) return null
  if (!payload.sub) return null

  return {
    token,
    id: payload.sub,
    email: typeof payload.email === 'string' ? payload.email : '',
    roles: normalizeRoles(payload[CLAIM_RUOLO]),
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
