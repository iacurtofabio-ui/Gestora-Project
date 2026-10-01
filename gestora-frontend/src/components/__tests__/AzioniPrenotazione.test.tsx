import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { AzioniPrenotazione } from '@/components/AzioniPrenotazione'
import { STATI_PRENOTAZIONE, type PrenotazioneDTO } from '@/types/prenotazione'

/**
 * Le azioni di una riga di prenotazione, viste dalla tastiera.
 *
 * Perche' questi test esistono: la colonna Azioni e' passata da cinque pulsanti affiancati a
 * un'azione primaria piu' un menu «…». Un menu e' molto piu' comodo con il mouse e molto piu'
 * fragile con la tastiera: se il fuoco non torna al pulsante dopo la chiusura, chi non usa il
 * mouse si ritrova all'inizio della pagina a ogni Esc, e in una tabella da venti righe e'
 * inutilizzabile. Sono le tre cose che si rompono per prime, quindi sono quelle verificate:
 * l'ordine di Tab, l'apertura da tastiera, e il ritorno del fuoco alla chiusura.
 *
 * Si prova il comportamento visibile, non l'implementazione: nessun assert sulle classi CSS.
 */

// Orologio fermo al 09/09/2026 alle 15:00 di Roma (13:00 UTC): la prenotazione di prova
// (stesso giorno, 19:00-21:00) e' confermabile e annullabile. Si finge solo Date, non i timer,
// altrimenti userEvent resterebbe in attesa.
beforeEach(() => {
  vi.useFakeTimers({ toFake: ['Date'] })
  vi.setSystemTime(new Date('2026-09-09T13:00:00Z'))
})
afterEach(() => {
  vi.useRealTimers()
})

function prenotazione(stato: string, dataPrenotazione = '2026-09-09'): PrenotazioneDTO {
  return {
    id: 1,
    dataPrenotazione,
    numeroCoperti: 4,
    note: null,
    stato,
    nomeUtente: 'rossi@mail.it',
    nomeCliente: 'Rossi',
    oraInizio: '19:00',
    oraFine: '21:00',
    fasciaOrariaId: 1,
    postazioni: [],
    numeroTurno: 1,
  }
}

function monta(
  stato: string,
  ruoli: { isStaff: boolean; isAdmin: boolean },
  dataPrenotazione?: string
) {
  const gestori = {
    onConferma: vi.fn(),
    onCompleta: vi.fn(),
    onModifica: vi.fn(),
    onAnnulla: vi.fn(),
    onElimina: vi.fn(),
  }
  render(
    <AzioniPrenotazione
      prenotazione={prenotazione(stato, dataPrenotazione)}
      isStaff={ruoli.isStaff}
      isAdmin={ruoli.isAdmin}
      inCorso={false}
      {...gestori}
    />
  )
  return gestori
}

const STAFF = { isStaff: true, isAdmin: false }
const ADMIN = { isStaff: true, isAdmin: true }

function trigger() {
  return screen.getByRole('button', { name: /Altre azioni/ })
}

describe('AzioniPrenotazione - una sola azione primaria', () => {
  it('su una prenotazione Attiva offre Conferma, e non Completa', () => {
    monta(STATI_PRENOTAZIONE.ATTIVA, STAFF)

    expect(screen.getByRole('button', { name: 'Conferma' })).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Completa' })).not.toBeInTheDocument()
  })

  it('su una prenotazione Confermata offre Completa, e non Conferma', () => {
    monta(STATI_PRENOTAZIONE.IN_CORSO, STAFF)

    expect(screen.getByRole('button', { name: 'Completa' })).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Conferma' })).not.toBeInTheDocument()
  })

  it('su una prenotazione Completata non propone nessuna azione', () => {
    monta(STATI_PRENOTAZIONE.COMPLETATA, ADMIN)

    // Niente da fare: ne' azione primaria ne' menu. Un menu vuoto sarebbe peggio di nessun menu.
    expect(screen.queryAllByRole('button')).toHaveLength(0)
    expect(screen.getByText('—')).toBeInTheDocument()
  })

  it("su una prenotazione Annullata l'Admin vede solo l'eliminazione, senza azione primaria", async () => {
    const utente = userEvent.setup()
    monta(STATI_PRENOTAZIONE.ANNULLATA, ADMIN)

    expect(screen.queryByRole('button', { name: 'Conferma' })).not.toBeInTheDocument()
    // Un solo pulsante visibile a menu chiuso: l'apri-menu.
    expect(screen.getAllByRole('button')).toHaveLength(1)

    await utente.click(trigger())
    expect(await screen.findByRole('menuitem', { name: 'Elimina definitivamente' })).toBeVisible()
  })

  it("su una prenotazione Attiva l'Admin non ha la voce di eliminazione", async () => {
    const utente = userEvent.setup()
    monta(STATI_PRENOTAZIONE.ATTIVA, ADMIN)

    await utente.click(trigger())
    expect(await screen.findByRole('menuitem', { name: 'Annulla prenotazione' })).toBeVisible()
    expect(screen.queryByRole('menuitem', { name: 'Elimina definitivamente' })).toBeNull()
  })

  it("su una prenotazione Non presentata anche l'Admin non ha nessuna azione", () => {
    monta(STATI_PRENOTAZIONE.NON_PRESENTATA, ADMIN)

    expect(screen.queryAllByRole('button')).toHaveLength(0)
  })

  it('su una prenotazione Non presentata lo Staff non Admin non vede nessuna azione', () => {
    monta(STATI_PRENOTAZIONE.NON_PRESENTATA, STAFF)

    expect(screen.queryAllByRole('button')).toHaveLength(0)
    expect(screen.getByText('—')).toBeInTheDocument()
  })

  it("l'eliminazione non e' mai un pulsante di riga: sta nel menu", async () => {
    const utente = userEvent.setup()
    monta(STATI_PRENOTAZIONE.ANNULLATA, ADMIN)

    // A menu chiuso il solo pulsante visibile e' l'apri-menu.
    expect(screen.getAllByRole('button')).toHaveLength(1)
    expect(screen.queryByText(/Elimina/)).not.toBeInTheDocument()

    await utente.click(trigger())
    expect(await screen.findByRole('menuitem', { name: 'Elimina definitivamente' })).toBeVisible()
  })

  it('lo Staff non Admin non vede la voce di eliminazione', async () => {
    const utente = userEvent.setup()
    monta(STATI_PRENOTAZIONE.ATTIVA, STAFF)

    await utente.click(trigger())

    expect(await screen.findByRole('menuitem', { name: 'Modifica prenotazione' })).toBeVisible()
    expect(screen.queryByRole('menuitem', { name: 'Elimina definitivamente' })).toBeNull()
  })
})

describe('AzioniPrenotazione - tastiera', () => {
  it("l'ordine di Tab e' azione primaria, poi apri-menu", async () => {
    const utente = userEvent.setup()
    monta(STATI_PRENOTAZIONE.ATTIVA, ADMIN)

    await utente.tab()
    expect(screen.getByRole('button', { name: 'Conferma' })).toHaveFocus()

    await utente.tab()
    expect(trigger()).toHaveFocus()
  })

  it('il menu si apre da tastiera, senza mouse', async () => {
    const utente = userEvent.setup()
    monta(STATI_PRENOTAZIONE.ATTIVA, ADMIN)

    trigger().focus()
    await utente.keyboard('{Enter}')

    expect(await screen.findByRole('menuitem', { name: 'Modifica prenotazione' })).toBeVisible()
  })

  it('Esc chiude il menu e riporta il fuoco sul pulsante che lo ha aperto', async () => {
    const utente = userEvent.setup()
    monta(STATI_PRENOTAZIONE.ATTIVA, ADMIN)

    await utente.click(trigger())
    expect(await screen.findByRole('menuitem', { name: 'Modifica prenotazione' })).toBeVisible()

    await utente.keyboard('{Escape}')

    // Le due meta' della stessa verifica: il menu se ne va E il fuoco non si perde. Senza la
    // seconda, in una tabella da venti righe ogni Esc rimanderebbe a inizio pagina.
    expect(screen.queryByRole('menuitem', { name: 'Modifica prenotazione' })).toBeNull()
    expect(trigger()).toHaveFocus()
  })

  it('scegliendo una voce con le frecce parte la sua azione e il menu si chiude', async () => {
    const utente = userEvent.setup()
    const gestori = monta(STATI_PRENOTAZIONE.ATTIVA, ADMIN)

    // Si apre con il clic e non con Invio di proposito: aprendo da tastiera Radix evidenzia gia'
    // la prima voce, quindi una freccia in giu' porterebbe sulla seconda. Aprendo con il mouse
    // nessuna voce e' evidenziata, e la prima freccia arriva sulla prima voce - che e' il
    // percorso da verificare qui.
    await utente.click(trigger())
    await screen.findByRole('menuitem', { name: 'Modifica prenotazione' })

    await utente.keyboard('{ArrowDown}{Enter}')

    expect(gestori.onModifica).toHaveBeenCalledTimes(1)
    expect(gestori.onAnnulla).not.toHaveBeenCalled()
    expect(screen.queryByRole('menuitem', { name: 'Modifica prenotazione' })).toBeNull()
  })
})

describe('AzioniPrenotazione - niente azioni che il backend rifiuterebbe', () => {
  it('su una prenotazione Attiva di domani non offre Conferma, ma si puo annullare', async () => {
    const utente = userEvent.setup()
    monta(STATI_PRENOTAZIONE.ATTIVA, STAFF, '2026-09-10')

    expect(screen.queryByRole('button', { name: 'Conferma' })).not.toBeInTheDocument()
    await utente.click(trigger())
    expect(await screen.findByRole('menuitem', { name: 'Annulla prenotazione' })).toBeVisible()
  })

  it('su una prenotazione Attiva di ieri non offre ne Conferma ne Annulla', async () => {
    const utente = userEvent.setup()
    monta(STATI_PRENOTAZIONE.ATTIVA, STAFF, '2026-09-08')

    expect(screen.queryByRole('button', { name: 'Conferma' })).not.toBeInTheDocument()
    await utente.click(trigger())
    expect(await screen.findByRole('menuitem', { name: 'Modifica prenotazione' })).toBeVisible()
    expect(screen.queryByRole('menuitem', { name: 'Annulla prenotazione' })).toBeNull()
  })

  it('a fascia finita (oggi dopo le 21:00) non si annulla piu una prenotazione In corso', async () => {
    vi.setSystemTime(new Date('2026-09-09T20:00:00Z')) // 22:00 a Roma
    monta(STATI_PRENOTAZIONE.IN_CORSO, STAFF)

    expect(screen.getByRole('button', { name: 'Completa' })).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /Altre azioni/ })).not.toBeInTheDocument()
  })
})
