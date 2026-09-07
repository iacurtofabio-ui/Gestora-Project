import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import PrenotazioneModal from '@/components/PrenotazioneModal'
import { useFascePerGiorno } from '@/hooks/useFasceOrarie'
import type { FasciaOrariaDTO } from '@/types/fasciaOraria'

/**
 * REV-047 — scelta della fascia oraria.
 *
 * E' il punto piu' delicato del form: le fasce dipendono dal giorno della settimana della data
 * scelta, e il giorno si ricava dalla data con una conversione che in questo progetto ha gia'
 * causato un bug di fuso orario (REV-016, dove il calcolo in ora locale faceva scivolare il giorno).
 * Qui si verifica il comportamento visibile - quali fasce vengono chieste e cosa resta selezionato
 * cambiando data - non il modo in cui e' implementato.
 *
 * Gli hook sono sostituiti da finti: servono a decidere cosa risponde il backend, non a provarlo.
 */
vi.mock('@/hooks/useFasceOrarie', () => ({
  useFascePerGiorno: vi.fn(),
}))

vi.mock('@/hooks/useZone', () => ({
  useZoneAttive: () => ({ data: [{ id: 1, nome: 'Sala', attiva: true }] }),
}))

vi.mock('@/hooks/usePrenotazioni', () => ({
  useCreaPrenotazione: () => ({ mutate: vi.fn(), isPending: false }),
  useModificaPrenotazione: () => ({ mutate: vi.fn(), isPending: false }),
}))

// NEW-002: il semaforo di disponibilita' non e' oggetto di questi test, che riguardano solo la
// scelta della fascia in base al giorno. Nessun dato disponibile => nessun suffisso sulle option,
// che restano confrontabili con il loro testo esatto.
vi.mock('@/hooks/useDisponibilita', () => ({
  useCheckDisponibilita: () => ({ data: undefined, isLoading: false }),
}))

vi.mock('@/hooks/useAuth', () => ({
  useAuth: () => ({ user: { id: '1', email: 'staff@gestora.it', roles: ['Staff'], token: 't' } }),
}))

const fascePerGiorno = vi.mocked(useFascePerGiorno)

function fascia(
  id: number,
  orarioInizio: string,
  orarioFine: string,
  giornoSettimana = 1
): FasciaOrariaDTO {
  return { id, orarioInizio, orarioFine, giornoSettimana, attiva: true, maxCoperti: 50 }
}

const PRANZO = fascia(1, '12:00', '15:00')
const CENA = fascia(2, '19:00', '23:00')
const BRUNCH = fascia(3, '10:00', '13:00')

/** Lunedi 7 settembre 2026 (giorno 1) e domenica 13 settembre 2026 (giorno 0). */
const LUNEDI = '2026-09-07'
const DOMENICA = '2026-09-13'

function selectFascia() {
  return screen.getAllByRole('combobox')[0] as HTMLSelectElement
}

describe('PrenotazioneModal - scelta della fascia oraria', () => {
  beforeEach(() => {
    fascePerGiorno.mockReset()
    fascePerGiorno.mockReturnValue({ data: [PRANZO, CENA] } as ReturnType<typeof useFascePerGiorno>)
  })

  it('finche non c e una data la fascia non e selezionabile', () => {
    render(<PrenotazioneModal isOpen onClose={() => {}} />)

    expect(selectFascia()).toBeDisabled()
    expect(screen.getByText('-- Seleziona prima una data --')).toBeInTheDocument()
    // Nessun giorno scelto: non si interroga il backend per le fasce.
    expect(fascePerGiorno).toHaveBeenCalledWith(undefined)
  })

  it('chiede le fasce del giorno della settimana corrispondente alla data scelta', async () => {
    const utente = userEvent.setup()
    render(<PrenotazioneModal isOpen onClose={() => {}} />)

    await utente.type(screen.getByLabelText('Data'), LUNEDI)

    // 7 settembre 2026 e' un lunedi: giorno 1. Se la conversione usasse l ora locale invece di UTC,
    // qui comparirebbe 0 (domenica) - il difetto di REV-016.
    expect(fascePerGiorno).toHaveBeenLastCalledWith(1)
  })

  it('mostra solo le fasce restituite per quel giorno', async () => {
    const utente = userEvent.setup()
    render(<PrenotazioneModal isOpen onClose={() => {}} />)

    await utente.type(screen.getByLabelText('Data'), LUNEDI)

    expect(screen.getByRole('option', { name: '12:00 - 15:00' })).toBeInTheDocument()
    expect(screen.getByRole('option', { name: '19:00 - 23:00' })).toBeInTheDocument()
    expect(screen.queryByRole('option', { name: '10:00 - 13:00' })).not.toBeInTheDocument()
  })

  it('azzera la fascia gia scelta quando si cambia giorno', async () => {
    const utente = userEvent.setup()
    render(<PrenotazioneModal isOpen onClose={() => {}} />)
    const campoData = screen.getByLabelText('Data') as HTMLInputElement

    await utente.type(campoData, LUNEDI)
    await utente.selectOptions(selectFascia(), '2')
    expect(selectFascia().value).toBe('2')

    // Il giorno cambia ma la cena resta fra le fasce offerte anche la domenica: e' voluto.
    // Se qui si togliesse la cena dalla lista, la select si svuoterebbe da sola per assenza
    // dell option corrispondente, e il test passerebbe anche senza alcun azzeramento - misurando
    // un effetto del DOM invece del comportamento che interessa (verificato rompendo il codice:
    // con la lista diversa il test restava verde).
    fascePerGiorno.mockReturnValue({ data: [BRUNCH, CENA] } as ReturnType<typeof useFascePerGiorno>)
    await utente.clear(campoData)
    await utente.type(campoData, DOMENICA)

    expect(fascePerGiorno).toHaveBeenLastCalledWith(0)
    // L option con id 2 esiste ancora: se il valore e' vuoto e' perche' il form l ha azzerato.
    expect(screen.getByRole('option', { name: '19:00 - 23:00' })).toBeInTheDocument()
    expect(selectFascia().value).toBe('')
  })

  it('avvisa quando per quel giorno non c e nessuna fascia attiva', async () => {
    const utente = userEvent.setup()
    fascePerGiorno.mockReturnValue({ data: [] } as unknown as ReturnType<typeof useFascePerGiorno>)
    render(<PrenotazioneModal isOpen onClose={() => {}} />)

    await utente.type(screen.getByLabelText('Data'), LUNEDI)

    // Giorno di chiusura: meglio dirlo, invece di lasciare una select vuota che sembra un errore.
    expect(screen.getByText('Nessuna fascia oraria attiva per questo giorno.')).toBeInTheDocument()
  })
})
