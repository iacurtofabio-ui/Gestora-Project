import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import PrenotazioneModal from '@/components/PrenotazioneModal'
import { useAuth } from '@/hooks/useAuth'

/**
 * V2-007 — limite online nel form di prenotazione: il Cliente che prenota da solo arriva a 20
 * persone (oltre: «contatta il ristorante» e pulsante spento); lo Staff, che prende anche le
 * tavolate al telefono, no.
 *
 * Gli hook sono finti: qui conta solo cosa vede chi compila il form.
 */
vi.mock('@/hooks/useFasceOrarie', () => ({
  useFascePerGiorno: () => ({ data: [] }),
}))

vi.mock('@/hooks/useZone', () => ({
  useZoneAttive: () => ({ data: [] }),
}))

vi.mock('@/hooks/usePrenotazioni', () => ({
  useCreaPrenotazione: () => ({ mutate: vi.fn(), isPending: false }),
  useModificaPrenotazione: () => ({ mutate: vi.fn(), isPending: false }),
}))

vi.mock('@/hooks/useDisponibilita', () => ({
  useCheckDisponibilita: () => ({ data: undefined, isLoading: false }),
  useLimitiPrenotazione: () => ({
    data: { maxCopertiPerPrenotazione: 50, maxCopertiPrenotazioneOnline: 20 },
  }),
}))

vi.mock('@/hooks/useAuth', () => ({
  useAuth: vi.fn(),
}))

const auth = vi.mocked(useAuth)

function utenteCon(ruoli: string[]) {
  auth.mockReturnValue({
    user: { id: '1', email: 'utente@gestora.it', roles: ruoli, token: 't' },
  } as unknown as ReturnType<typeof useAuth>)
}

const INVITO = /Per prenotazioni superiori a 20 persone contatta direttamente il\s+ristorante/

async function scriviCoperti(valore: string) {
  render(<PrenotazioneModal isOpen onClose={() => {}} />)
  await userEvent.type(screen.getByLabelText('Numero di coperti'), valore)
}

describe('PrenotazioneModal - limite online dei coperti', () => {
  beforeEach(() => {
    auth.mockReset()
  })

  it('Cliente oltre 20: invito a contattare il ristorante e pulsante disattivato', async () => {
    utenteCon(['Cliente'])

    await scriviCoperti('21')

    expect(screen.getByText(INVITO)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Crea prenotazione' })).toBeDisabled()
  })

  it('Cliente a 20: nessun invito', async () => {
    utenteCon(['Cliente'])

    await scriviCoperti('20')

    expect(screen.queryByText(INVITO)).not.toBeInTheDocument()
  })

  it('Staff oltre 20: nessun invito, la tavolata al telefono si inserisce', async () => {
    utenteCon(['Staff'])

    await scriviCoperti('30')

    expect(screen.queryByText(INVITO)).not.toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Crea prenotazione' })).toBeEnabled()
  })
})
