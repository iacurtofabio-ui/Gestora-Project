/**
 * REV-050 — lettura del token JWT in un punto solo.
 *
 * La decodifica del payload era scritta a mano piu' volte nel progetto (AuthContext e LoginPage):
 * ogni copia era un punto in cui dimenticare il try/catch, ed e' esattamente quello che era
 * successo con REV-014, dove un token corrotto in localStorage lasciava la pagina bianca. Qui la
 * lettura e' difensiva per costruzione: qualunque cosa non sia un token valido diventa `null`, e
 * chi chiama tratta `null` come "utente anonimo".
 *
 * Nota di sicurezza: questa e' una lettura, non una verifica. La firma non viene controllata (non
 * si puo' lato client, la chiave sta sul server) - serve solo a sapere chi mostrare in interfaccia
 * e quali voci di menu accendere. L'autorizzazione vera resta quella del backend a ogni chiamata.
 */
export type PayloadJwt = {
  sub?: string
  email?: string
  exp?: number
  [claim: string]: unknown
}

export function decodificaPayloadJwt(token: string): PayloadJwt | null {
  try {
    const payloadBase64 = token.split('.')[1]
    if (!payloadBase64) return null

    // Il payload JWT e' base64url: '-' e '_' al posto di '+' e '/', e senza padding.
    const base64 = payloadBase64.replace(/-/g, '+').replace(/_/g, '/')
    const payload: unknown = JSON.parse(atob(base64))

    if (typeof payload !== 'object' || payload === null) return null
    return payload as PayloadJwt
  } catch {
    return null
  }
}

/** `exp` e' in secondi dall'epoca UTC: nessun problema di fuso, e' un istante assoluto. */
export function tokenScaduto(payload: PayloadJwt, adesso: number = Date.now()): boolean {
  return typeof payload.exp === 'number' && payload.exp * 1000 <= adesso
}
