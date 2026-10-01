import { describe, it, expect } from 'vitest'
import { AxiosError, AxiosHeaders } from 'axios'
import { erroriPerCampo, messaggioErrore, messaggioErroreLogin } from '@/lib/apiError'
import type { ApiErrorResponse } from '@/types/apiError'

/**
 * REV-047 / REV-041 — l'helper che ha sostituito 26 copie della stessa logica. Vale la pena
 * coprirlo proprio perche' ora un suo errore si vedrebbe ovunque nell'applicazione.
 */
function erroreConRisposta(stato: number, dati: ApiErrorResponse): AxiosError<ApiErrorResponse> {
  const errore = new AxiosError<ApiErrorResponse>('richiesta fallita')
  errore.response = {
    status: stato,
    statusText: '',
    data: dati,
    headers: new AxiosHeaders(),
    config: { headers: new AxiosHeaders() },
  }
  return errore
}

describe('messaggioErrore', () => {
  it('preferisce gli errori di validazione, uniti in un solo messaggio', () => {
    const errore = erroreConRisposta(400, {
      statusCode: 400,
      message: 'Errore di validazione',
      errors: [
        { field: 'numeroCoperti', error: 'I coperti devono essere almeno 1.' },
        { field: 'dataPrenotazione', error: 'La data non puo essere nel passato.' },
      ],
    })

    expect(messaggioErrore(errore, 'ripiego')).toBe(
      'I coperti devono essere almeno 1., La data non puo essere nel passato.'
    )
  })

  it('usa il messaggio del server quando non ci sono errori di campo', () => {
    const errore = erroreConRisposta(409, {
      statusCode: 409,
      message: 'Non ci sono postazioni libere per questa fascia.',
    })

    expect(messaggioErrore(errore, 'ripiego')).toBe(
      'Non ci sono postazioni libere per questa fascia.'
    )
  })

  it('usa il ripiego quando la risposta non porta alcun messaggio', () => {
    const errore = erroreConRisposta(500, { statusCode: 500 } as ApiErrorResponse)

    expect(messaggioErrore(errore, 'Errore durante la creazione')).toBe(
      'Errore durante la creazione'
    )
  })

  it('ignora un array errors vuoto e non produce una stringa vuota', () => {
    const errore = erroreConRisposta(400, {
      statusCode: 400,
      message: 'Richiesta non valida',
      errors: [],
    })

    expect(messaggioErrore(errore, 'ripiego')).toBe('Richiesta non valida')
  })

  it('distingue il server irraggiungibile dall operazione fallita', () => {
    // Senza response la richiesta non e' mai arrivata: dire "errore durante la creazione"
    // manderebbe fuori strada, perche' la creazione non e' stata nemmeno tentata.
    const errore = new AxiosError<ApiErrorResponse>('Network Error')

    expect(messaggioErrore(errore, 'Errore durante la creazione')).toBe(
      'Server non raggiungibile. Controlla la connessione e riprova.'
    )
  })
})

describe('messaggioErroreLogin', () => {
  // Prima ogni errore diventava "email o password errate", anche l'account bloccato:
  // l'utente riprovava all'infinito una password giusta.
  const vuoto = {} as ApiErrorResponse

  it('credenziali errate su 401', () => {
    expect(messaggioErroreLogin(erroreConRisposta(401, vuoto))).toMatch(/non corrispondono/)
  })

  it('account bloccato su 423', () => {
    expect(messaggioErroreLogin(erroreConRisposta(423, vuoto))).toMatch(/bloccato/)
  })

  it('troppi tentativi su 429', () => {
    expect(messaggioErroreLogin(erroreConRisposta(429, vuoto))).toMatch(/Troppi tentativi/)
  })

  it('errore del server su 500, non credenziali errate', () => {
    expect(messaggioErroreLogin(erroreConRisposta(500, vuoto))).not.toMatch(/non corrispondono/)
  })

  it('server non raggiungibile senza risposta', () => {
    expect(messaggioErroreLogin(new AxiosError('Network Error'))).toMatch(/non raggiungibile/)
  })
})

describe('erroriPerCampo', () => {
  // Registrazione: "email gia' registrata" va sotto il campo Email, non in un messaggio generico.
  it('assegna gli errori ai campi del form senza distinguere maiuscole e minuscole', () => {
    const errore = erroreConRisposta(400, {
      statusCode: 400,
      message: 'Registrazione non riuscita',
      errors: [
        { field: 'email', error: "L'email e' gia' registrata." },
        { field: 'Username', error: 'Il nome utente e gia in uso.' },
        { field: '', error: 'Errore generico.' },
      ],
    })

    const { perCampo, altri } = erroriPerCampo(errore, ['username', 'email', 'password'] as const)

    expect(perCampo.email).toBe("L'email e' gia' registrata.")
    expect(perCampo.username).toBe('Il nome utente e gia in uso.')
    expect(perCampo.password).toBeUndefined()
    expect(altri).toEqual(['Errore generico.'])
  })
})
