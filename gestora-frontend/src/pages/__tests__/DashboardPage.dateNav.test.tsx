import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import DashboardPage from '@/pages/DashboardPage'
import { useDashboardGiornaliera, useDashboardSettimanale } from '@/hooks/useDashboard'

/**
 * FASE 7 — navigazione per giorno.
 *
 * Il comportamento da provare e' semplice ma facile da rompere in silenzio: il pulsante
 * "giorno successivo" deve davvero cambiare la data chiesta al backend (la queryKey degli hook),
 * non solo un numero a schermo. Gli hook sono finti: qui non si prova la dashboard vera, si prova
 * che il clic sposti la data di un giorno esatto.
 */
vi.mock('@/hooks/useDashboard', () => ({
  useDashboardGiornaliera: vi.fn(),
  useDashboardSettimanale: vi.fn(),
}))

vi.mock('@/hooks/usePrenotazioni', () => ({
  usePrenotazioni: () => ({ data: { items: [], totalCount: 0, page: 1, pageSize: 100, totalPages: 1 } }),
  useConfermaPrenotazione: () => ({ mutate: vi.fn(), isPending: false }),
  useCompletaPrenotazione: () => ({ mutate: vi.fn(), isPending: false }),
  useAnnullaPrenotazione: () => ({ mutate: vi.fn(), isPending: false }),
  useDeletePrenotazione: () => ({ mutate: vi.fn(), isPending: false }),
}))

vi.mock('@/hooks/useAuth', () => ({
  useAuth: () => ({ user: { id: '1', email: 'admin@gestora.it', roles: ['Admin'], token: 't' } }),
}))

const giornaliera = vi.mocked(useDashboardGiornaliera)
const settimanale = vi.mocked(useDashboardSettimanale)

const RISPOSTA_VUOTA = {
  isLoading: false,
  isError: false,
  isFetching: false,
  error: null,
  dataUpdatedAt: 0,
  refetch: vi.fn(),
  data: {
    data: '2026-09-09',
    totalePrenotazioni: 0,
    prenotazioniAttive: 0,
    prenotazioniInCorso: 0,
    prenotazioniCompletate: 0,
    prenotazioniAnnullate: 0,
    totaleCopertiPrenotati: 0,
    totalePostazioniAttive: 0,
    postazioniOccupate: 0,
    postazioniLibere: 0,
    copertiPerFascia: [],
  },
}

const RISPOSTA_SETTIMANALE_VUOTA = {
  isLoading: false,
  isError: false,
  error: null,
  refetch: vi.fn(),
  data: {
    dataInizio: '2026-09-07',
    dataFine: '2026-09-13',
    totalePrenotazioni: 0,
    totaleCoperti: 0,
    tassoAnnullamento: 0,
    tassoNoShow: 0,
    giorni: [],
  },
}

describe('DashboardPage - navigazione per giorno', () => {
  it('il pulsante "giorno successivo" chiede al backend il giorno dopo', async () => {
    const utente = userEvent.setup()
    // @ts-expect-error -- risposta parziale, bastano i campi che il componente legge
    giornaliera.mockReturnValue(RISPOSTA_VUOTA)
    // @ts-expect-error -- idem
    settimanale.mockReturnValue(RISPOSTA_SETTIMANALE_VUOTA)

    render(
      <MemoryRouter>
        <DashboardPage />
      </MemoryRouter>
    )

    const chiamateIniziali = giornaliera.mock.calls.length
    const dataIniziale = giornaliera.mock.calls[chiamateIniziali - 1][0]

    await utente.click(screen.getByRole('button', { name: 'Giorno successivo' }))

    const dataDopoClic = giornaliera.mock.calls[giornaliera.mock.calls.length - 1][0]
    const atteso = new Date(`${dataIniziale}T00:00:00Z`)
    atteso.setUTCDate(atteso.getUTCDate() + 1)

    expect(dataDopoClic).toBe(atteso.toISOString().split('T')[0])
  })
})
