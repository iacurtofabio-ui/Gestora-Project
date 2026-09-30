import { describe, it, expect } from 'vitest'
import { paginaDiCasa } from '@/lib/navigazione'

/**
 * `paginaDiCasa` unifica una scelta che prima era duplicata in tre punti (LoginPage,
 * RedirectSeAutenticato, UnauthorizedPage) con esiti diversi per lo stesso caso limite: un
 * utente autenticato a cui l'Admin ha tolto tutti i ruoli. Due lo mandavano a /dashboard, uno
 * a /prenotazioni, e da lì ProtectedRoute lo rispediva a /unauthorized: un ciclo senza uscita.
 */
describe('paginaDiCasa', () => {
  it('un utente senza nessun ruolo non ha una pagina di casa: va a /unauthorized', () => {
    expect(paginaDiCasa([])).toBe('/unauthorized')
  })

  it('Admin va alla dashboard', () => {
    expect(paginaDiCasa(['Admin'])).toBe('/dashboard')
  })

  it('Staff va alla dashboard', () => {
    expect(paginaDiCasa(['Staff'])).toBe('/dashboard')
  })

  it('Cliente va alle prenotazioni', () => {
    expect(paginaDiCasa(['Cliente'])).toBe('/prenotazioni')
  })

  it('un utente con più ruoli, incluso Staff, va alla dashboard', () => {
    expect(paginaDiCasa(['Cliente', 'Staff'])).toBe('/dashboard')
  })
})
