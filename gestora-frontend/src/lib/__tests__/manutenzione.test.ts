import { describe, it, expect } from 'vitest'
import { leggiManutenzione } from '@/lib/manutenzione'

/**
 * V2-016: la manutenzione si accende solo con «true», scritto come capita su Vercel. Qualsiasi
 * altro valore, o la variabile assente, lascia il portale aperto.
 */
describe('leggiManutenzione', () => {
  it('variabile assente: portale aperto', () => {
    expect(leggiManutenzione(undefined)).toBe(false)
  })

  it('«true»: portale in manutenzione', () => {
    expect(leggiManutenzione('true')).toBe(true)
  })

  it('maiuscole e spazi non contano', () => {
    expect(leggiManutenzione(' TRUE ')).toBe(true)
  })

  it('«false», vuoto o altri valori: portale aperto', () => {
    expect(leggiManutenzione('false')).toBe(false)
    expect(leggiManutenzione('')).toBe(false)
    expect(leggiManutenzione('1')).toBe(false)
  })
})
