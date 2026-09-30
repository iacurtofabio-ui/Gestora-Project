import { describe, it, expect, vi } from 'vitest'
import { render } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import PrenotazionePage from '@/pages/PrenotazionePage'
import { usePrenotazioni } from '@/hooks/usePrenotazioni'

/**
 * FASE 7 — i filtri vivono nell'URL, non solo nello stato del componente.
 *
 * Il comportamento da provare: aprendo la pagina con un indirizzo tipo
 * `/prenotazioni?data=...&stato=...&fascia=...`, quei valori arrivano davvero alla chiamata dei
 * dati, non solo ai campi del filtro a schermo — e' quello che rende un link condivisibile.
 */
vi.mock('@/hooks/usePrenotazioni', () => ({
  usePrenotazioni: vi.fn(() => ({
    data: { items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 1 },
    isLoading: false,
    isError: false,
    error: null,
  })),
  useConfermaPrenotazione: () => ({ mutate: vi.fn(), isPending: false, variables: undefined }),
  useCompletaPrenotazione: () => ({ mutate: vi.fn(), isPending: false, variables: undefined }),
  useAnnullaPrenotazione: () => ({ mutate: vi.fn(), isPending: false }),
  useDeletePrenotazione: () => ({ mutate: vi.fn(), isPending: false }),
}))

vi.mock('@/components/PrenotazioneModal', () => ({
  default: () => null,
}))

vi.mock('@/hooks/useAuth', () => ({
  useAuth: () => ({ user: { id: '1', email: 'staff@gestora.it', roles: ['Staff'], token: 't' } }),
}))

const prenotazioniMock = vi.mocked(usePrenotazioni)

describe('PrenotazionePage - filtri nell URL', () => {
  it('legge data, stato e fascia dai parametri e li passa alla chiamata dei dati', () => {
    render(
      <MemoryRouter
        initialEntries={['/prenotazioni?data=2026-09-20&stato=Attiva&fascia=3&pagina=2']}
      >
        <PrenotazionePage />
      </MemoryRouter>
    )

    expect(prenotazioniMock).toHaveBeenCalledWith(
      expect.objectContaining({
        data: '2026-09-20',
        stato: 'Attiva',
        fasciaOrariaId: 3,
        page: 2,
      })
    )
  })

  it('senza parametri chiede tutti gli stati, nessuna data e la prima pagina', () => {
    render(
      <MemoryRouter initialEntries={['/prenotazioni']}>
        <PrenotazionePage />
      </MemoryRouter>
    )

    expect(prenotazioniMock).toHaveBeenCalledWith(
      expect.objectContaining({
        data: undefined,
        stato: undefined,
        fasciaOrariaId: undefined,
        page: 1,
      })
    )
  })
})
