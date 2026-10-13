import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import PrenotazioneModal from '@/components/PrenotazioneModal'
import { useAuth } from '@/hooks/useAuth'

/**
 * V2-013 — il nome del cliente non può superare i 200 caratteri della colonna: il form lo dice
 * subito, invece di far arrivare la richiesta al server per un «errore interno».
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

const MESSAGGIO = 'Il nome non può superare 200 caratteri'

async function inviaConNome(nome: string) {
  vi.mocked(useAuth).mockReturnValue({
    user: { id: '1', email: 'staff@gestora.it', roles: ['Staff'], token: 't' },
  } as unknown as ReturnType<typeof useAuth>)

  render(<PrenotazioneModal isOpen onClose={() => {}} />)
  await userEvent.click(screen.getByLabelText('Nome cliente (facoltativo)'))
  await userEvent.paste(nome)
  await userEvent.click(screen.getByRole('button', { name: 'Crea prenotazione' }))
}

describe('PrenotazioneModal - lunghezza del nome cliente', () => {
  it('oltre 200 caratteri: messaggio sotto il campo', async () => {
    await inviaConNome('x'.repeat(201))

    expect(await screen.findByText(MESSAGGIO)).toBeInTheDocument()
  })

  it('200 caratteri: nessun messaggio', async () => {
    await inviaConNome('x'.repeat(200))

    expect(screen.queryByText(MESSAGGIO)).not.toBeInTheDocument()
  })
})
