import { describe, it, expect } from 'vitest'
import { decodificaPayloadJwt, tokenScaduto } from '@/lib/jwt'

/**
 * REV-047 / REV-014 — questi casi erano stati provati a mano in Fase 6 aprendo il browser con un
 * token corrotto in localStorage. Erano la causa della schermata bianca da cui nemmeno /login era
 * raggiungibile: qui diventano automatici, cosi' il giorno in cui qualcuno "semplifica" la lettura
 * del token togliendo il try/catch, il test lo dice subito.
 */

/** Costruisce un token con il payload dato. La firma e' finta: qui non viene mai verificata. */
function tokenCon(payload: Record<string, unknown>): string {
  const base64url = (o: unknown) =>
    btoa(JSON.stringify(o)).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '')
  return `${base64url({ alg: 'HS256', typ: 'JWT' })}.${base64url(payload)}.firma-non-verificata`
}

describe('decodificaPayloadJwt', () => {
  it('legge il payload di un token ben formato', () => {
    const payload = decodificaPayloadJwt(tokenCon({ sub: 'utente-1', email: 'a@b.it' }))

    expect(payload).not.toBeNull()
    expect(payload?.sub).toBe('utente-1')
    expect(payload?.email).toBe('a@b.it')
  })

  it('restituisce null su un token che non ha la forma di un JWT', () => {
    expect(decodificaPayloadJwt('abc')).toBeNull()
  })

  it('restituisce null su un token con le tre parti ma il payload illeggibile', () => {
    expect(decodificaPayloadJwt('a.b.c')).toBeNull()
  })

  it('restituisce null su un token troncato a meta', () => {
    const intero = tokenCon({ sub: 'utente-1' })
    expect(decodificaPayloadJwt(intero.slice(0, intero.length / 2))).toBeNull()
  })

  it('restituisce null sulla stringa vuota', () => {
    expect(decodificaPayloadJwt('')).toBeNull()
  })

  it('legge il payload con caratteri base64url (- e _) senza padding', () => {
    // Un payload abbastanza lungo da produrre '-' o '_' nella codifica: e' il caso che una
    // decodifica base64 "normale" sbaglia, ed e' proprio quello che arriva da ASP.NET Identity.
    const payload = decodificaPayloadJwt(tokenCon({ sub: 'utente-1', nota: '~~~???>>><<<ÿÿ' }))
    expect(payload?.sub).toBe('utente-1')
  })
})

describe('tokenScaduto', () => {
  const ADESSO = new Date('2026-09-07T10:00:00Z').getTime()

  it('considera scaduto un token con exp nel passato', () => {
    expect(tokenScaduto({ exp: ADESSO / 1000 - 60 }, ADESSO)).toBe(true)
  })

  it('non considera scaduto un token con exp nel futuro', () => {
    expect(tokenScaduto({ exp: ADESSO / 1000 + 60 }, ADESSO)).toBe(false)
  })

  it('considera scaduto un token che scade esattamente adesso', () => {
    expect(tokenScaduto({ exp: ADESSO / 1000 }, ADESSO)).toBe(true)
  })

  it('non considera scaduto un token senza claim exp', () => {
    // Senza `exp` non c'e' nulla da verificare lato client: decide il backend.
    expect(tokenScaduto({ sub: 'utente-1' }, ADESSO)).toBe(false)
  })
})
