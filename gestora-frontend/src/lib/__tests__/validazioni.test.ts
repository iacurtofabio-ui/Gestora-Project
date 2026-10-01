import { describe, it, expect } from 'vitest'
import { interoObbligatorio, usernameSchema } from '@/lib/validazioni'

describe('usernameSchema', () => {
  it('accetta nome e cognome separati da uno spazio', () => {
    expect(usernameSchema.safeParse('Fabio Iacurto').success).toBe(true)
  })

  it("accetta apostrofi e lettere accentate (D'Angelo, Niccolò)", () => {
    expect(usernameSchema.safeParse("D'Angelo").success).toBe(true)
    expect(usernameSchema.safeParse('Niccolò Rossi').success).toBe(true)
  })

  it('toglie da solo gli spazi in testa e in coda', () => {
    expect(usernameSchema.parse('  Fabio Iacurto ')).toBe('Fabio Iacurto')
  })

  it('rifiuta due spazi di seguito', () => {
    expect(usernameSchema.safeParse('Fabio  Iacurto').success).toBe(false)
  })

  it('rifiuta caratteri che il backend non ammette', () => {
    expect(usernameSchema.safeParse('Fabio<Iacurto>').success).toBe(false)
  })
})

describe('interoObbligatorio', () => {
  // Campo numerico vuoto con valueAsNumber = NaN: prima Zod v4 mostrava il suo messaggio inglese.
  it('su un campo vuoto mostra il messaggio italiano', () => {
    const risultato = interoObbligatorio('Numero obbligatorio').safeParse(Number.NaN)
    expect(risultato.success).toBe(false)
    expect(risultato.error?.issues[0].message).toBe('Numero obbligatorio')
  })

  it('rifiuta i decimali', () => {
    expect(interoObbligatorio('Numero obbligatorio').safeParse(2.5).success).toBe(false)
  })

  it('accetta un intero valido', () => {
    expect(interoObbligatorio('Numero obbligatorio').safeParse(4).success).toBe(true)
  })
})
