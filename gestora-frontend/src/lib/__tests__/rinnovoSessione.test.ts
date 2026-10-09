import { describe, it, expect } from 'vitest'
import { MARGINE_RINNOVO_MS, ritardoRinnovo } from '@/lib/rinnovoSessione'

/** V2-010 — quando parte il rinnovo del token. */
const ADESSO = Date.UTC(2026, 9, 9, 18, 0, 0)

describe('ritardoRinnovo', () => {
  it('aspetta fino a 10 minuti prima della scadenza', () => {
    const exp = (ADESSO + 60 * 60 * 1000) / 1000

    expect(ritardoRinnovo(exp, ADESSO)).toBe(60 * 60 * 1000 - MARGINE_RINNOVO_MS)
  })

  it('rinnova subito se si e gia dentro il margine', () => {
    const exp = (ADESSO + 5 * 60 * 1000) / 1000

    expect(ritardoRinnovo(exp, ADESSO)).toBe(0)
  })

  it('non rinnova un token senza scadenza', () => {
    expect(ritardoRinnovo(undefined, ADESSO)).toBeNull()
  })
})
