import { useEffect, useState } from 'react'
import { isAxiosError } from 'axios'
import { AuthContext, type AuthUser } from './auth-context'
import { decodificaPayloadJwt, tokenScaduto } from '@/lib/jwt'
import apiClient from '@/lib/axios'
import { Endpoints } from '@/lib/endpoints'
import { RIPROVA_RINNOVO_MS, ritardoRinnovo } from '@/lib/rinnovoSessione'

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

  // V2-010 — rinnovo del token poco prima della scadenza, finche' la pagina e' aperta. Riparte a
  // ogni token nuovo (login, rinnovo) e si ferma al logout.
  const token = user?.token
  useEffect(() => {
    if (!token) return
    const scadenza = decodificaPayloadJwt(token)?.exp
    const ritardo = ritardoRinnovo(scadenza)
    if (ritardo === null || scadenza === undefined) return
    const scadenzaAttuale = scadenza

    let annullato = false
    let timer = setTimeout(rinnova, ritardo)

    async function rinnova() {
      try {
        const { data } = await apiClient.post<{ token: string }>(Endpoints.auth.rinnovaToken)
        if (annullato) return
        // Al limite delle 12 ore dal login il server non puo' allungare la scadenza: si tiene il
        // token che c'e' e non si richiede piu'. Alla scadenza si torna al login.
        const nuovaScadenza = decodificaPayloadJwt(data.token)?.exp
        if (nuovaScadenza === undefined || nuovaScadenza <= scadenzaAttuale) return
        const utente = leggiUtenteDalToken(data.token)
        if (!utente) return
        localStorage.setItem('token', data.token)
        setUser(utente)
      } catch (errore) {
        // 401 = sessione finita: ci pensa gia' l'intercettore (avviso e ritorno al login).
        if (annullato || (isAxiosError(errore) && errore.response?.status === 401)) return
        // Rete o server momentaneamente giu': si riprova, finche' il token vale ancora.
        if (scadenzaAttuale * 1000 - Date.now() > RIPROVA_RINNOVO_MS) {
          timer = setTimeout(rinnova, RIPROVA_RINNOVO_MS)
        }
      }
    }

    return () => {
      annullato = true
      clearTimeout(timer)
    }
  }, [token])

  function login(token: string) {
    const utente = leggiUtenteDalToken(token)
    if (!utente) {
      localStorage.removeItem('token')
      setUser(null)
      throw new Error("Il token ricevuto dal server non e' valido o e' gia' scaduto.")
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
