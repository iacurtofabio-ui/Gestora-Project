import { describe, it, expect, vi, beforeEach } from 'vitest'
import { fireEvent, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { VerificaDisponibilita } from '@/components/landing/VerificaDisponibilita'
import { useCheckDisponibilita } from '@/hooks/useDisponibilita'
import type { FasciaDisponibilitaDTO, MotivoDisponibilita } from '@/types/disponibilita'

/**
 * V2-007 — pagina pubblica: una fascia non prenotabile mostra UNA sola informazione, scelta dal
 * motivo del backend. Prima mostrava sempre «Disponibilità residua: N» accanto a «Pieno», che si
 * contraddicevano quando a mancare erano i tavoli (es. 50 persone, tetto 60, due tavoli da 2).
 *
 * Gli hook sono finti: qui si decide cosa risponde il backend, non lo si prova.
 */
vi.mock('@/hooks/useDisponibilita', () => ({
  useCheckDisponibilita: vi.fn(),
  useLimitiPrenotazione: () => ({
    data: { maxCopertiPerPrenotazione: 50, maxCopertiPrenotazioneOnline: 20 },
  }),
}))

const checkDisponibilita = vi.mocked(useCheckDisponibilita)

function fascia(
  id: number,
  motivo: MotivoDisponibilita,
  postiResiduiFascia = 0
): FasciaDisponibilitaDTO {
  return {
    fasciaOrariaId: id,
    orarioInizio: `${10 + id}:00:00`,
    orarioFine: `${11 + id}:00:00`,
    maxCoperti: 60,
    postiResiduiFascia,
    totalePostiDisponibili: postiResiduiFascia,
    totaleCapienza: 4,
    disponibilePerRichiesta: motivo === 'Libera',
    messaggio: motivo === 'Libera' ? null : 'testo per lo staff',
    motivo,
  }
}

function rispondeCon(...fasce: FasciaDisponibilitaDTO[]) {
  checkDisponibilita.mockReturnValue({
    data: { fasce },
    isFetching: false,
    isError: false,
  } as unknown as ReturnType<typeof useCheckDisponibilita>)
}

async function cerca(persone: string) {
  render(
    <MemoryRouter>
      <VerificaDisponibilita />
    </MemoryRouter>
  )
  fireEvent.change(screen.getByLabelText('Giorno'), { target: { value: '2026-11-10' } })
  const campoPersone = screen.getByLabelText('Persone')
  await userEvent.clear(campoPersone)
  await userEvent.type(campoPersone, persone)
  await userEvent.click(screen.getByRole('button', { name: 'Vedi i turni liberi' }))
}

describe('VerificaDisponibilita - stato di una fascia non prenotabile', () => {
  beforeEach(() => {
    checkDisponibilita.mockReset()
  })

  it('tavoli insufficienti: «Pieno per N persone», senza i posti residui accanto (il bug)', async () => {
    // Come il caso segnalato (tetto ampio, pochi tavoli), ma entro il limite online di 20.
    rispondeCon(fascia(1, 'TavoliInsufficienti', 58))

    await cerca('15')

    expect(screen.getByText('Pieno per 15 persone')).toBeInTheDocument()
    expect(screen.queryByText(/Disponibilità residua/)).not.toBeInTheDocument()
    expect(screen.queryByText(/58/)).not.toBeInTheDocument()
  })

  it('posti insufficienti: dice quanti posti restano', async () => {
    rispondeCon(fascia(1, 'PostiInsufficienti', 3), fascia(2, 'PostiInsufficienti', 1))

    await cerca('4')

    expect(screen.getByText('Restano 3 posti')).toBeInTheDocument()
    expect(screen.getByText('Resta 1 posto')).toBeInTheDocument()
  })

  it('tetto esaurito: «Pieno»', async () => {
    rispondeCon(fascia(1, 'TettoEsaurito'))

    await cerca('2')

    expect(screen.getByText('Pieno')).toBeInTheDocument()
  })

  it('fascia già finita: «Turno concluso», e in fondo non dice «siamo al completo»', async () => {
    rispondeCon(fascia(1, 'Terminata'), fascia(2, 'Terminata'))

    await cerca('2')

    expect(screen.getAllByText('Turno concluso')).toHaveLength(2)
    expect(screen.getByText(/turni di oggi sono conclusi/)).toBeInTheDocument()
    expect(screen.queryByText(/siamo al completo/)).not.toBeInTheDocument()
  })

  it('oltre il limite online (20) invita a contattare il ristorante', async () => {
    rispondeCon(fascia(1, 'Libera', 40))
    render(
      <MemoryRouter>
        <VerificaDisponibilita />
      </MemoryRouter>
    )

    const campoPersone = screen.getByLabelText('Persone')
    await userEvent.clear(campoPersone)
    await userEvent.type(campoPersone, '21')

    expect(
      screen.getByText('Per prenotazioni superiori a 20 persone contatta direttamente il ristorante.')
    ).toBeInTheDocument()
    expect(campoPersone).toHaveAttribute('max', '20')
  })

  it('una fascia libera resta «Libero» e propone di prenotare', async () => {
    rispondeCon(fascia(1, 'Libera', 20), fascia(2, 'TettoEsaurito'))

    await cerca('2')

    expect(screen.getByText('Libero')).toBeInTheDocument()
    expect(screen.getByText('Pieno')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Prenota ora' })).toBeInTheDocument()
  })
})
