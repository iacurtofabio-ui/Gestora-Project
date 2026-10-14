import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { AxiosError, AxiosHeaders } from 'axios'
import ConfermaEmailPage from '@/pages/ConfermaEmailPage'
import apiClient from '@/lib/axios'
import { Endpoints } from '@/lib/endpoints'

/**
 * V2-003 — la pagina a cui porta il link dell'email. Il server è finto: si prova che il link
 * venga letto bene, che la conferma parta da sola e che l'esito sia chiaro in tutti e tre i casi.
 */
vi.mock('@/lib/axios', () => ({ default: { post: vi.fn() } }))

const post = vi.mocked(apiClient.post)

function apri(indirizzo: string) {
  return render(
    <MemoryRouter initialEntries={[indirizzo]}>
      <ConfermaEmailPage />
    </MemoryRouter>
  )
}

describe('ConfermaEmailPage', () => {
  beforeEach(() => {
    post.mockReset()
  })

  it('conferma da sola con i valori del link e invita ad accedere', async () => {
    post.mockResolvedValue({ data: 'Email confermata. Ora puoi accedere.' })

    apri('/conferma-email?userId=utente-1&token=abc_DEF-123')

    expect(await screen.findByText('Email confermata')).toBeInTheDocument()
    expect(post).toHaveBeenCalledWith(Endpoints.auth.confermaEmail, {
      userId: 'utente-1',
      token: 'abc_DEF-123',
    })
    expect(screen.getByRole('link', { name: 'Accedi' })).toHaveAttribute('href', '/login')
  })

  it('mostra il messaggio del server quando il link non è valido o scaduto', async () => {
    const errore = new AxiosError('richiesta fallita')
    errore.response = {
      status: 400,
      statusText: '',
      data: { statusCode: 400, message: 'Il link di conferma non è valido o è scaduto.' },
      headers: new AxiosHeaders(),
      config: { headers: new AxiosHeaders() },
    }
    post.mockRejectedValue(errore)

    apri('/conferma-email?userId=utente-1&token=scaduto')

    expect(await screen.findByText('Non riesco a confermare l’email')).toBeInTheDocument()
    expect(screen.getByText('Il link di conferma non è valido o è scaduto.')).toBeInTheDocument()
  })

  it('senza i valori nel link non chiama il server e lo dice', () => {
    apri('/conferma-email')

    expect(post).not.toHaveBeenCalled()
    expect(screen.getByText('Non riesco a confermare l’email')).toBeInTheDocument()
    expect(screen.getByText(/link di conferma non è valido/)).toBeInTheDocument()
  })
})
