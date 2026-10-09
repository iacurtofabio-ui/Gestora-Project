import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { act, render, screen } from '@testing-library/react'
import { AxiosError, AxiosHeaders } from 'axios'
import { AuthProvider } from '@/context/AuthContext'
import { useAuth } from '@/hooks/useAuth'
import apiClient from '@/lib/axios'

/**
 * V2-010 — AuthProvider rinnova il token poco prima della scadenza, finche' la pagina e' aperta.
 * Prima lo Staff veniva buttato fuori ogni 60 minuti, anche a meta' di un form.
 *
 * L'orologio e i timer sono finti; la chiamata al backend e' simulata.
 */
vi.mock('@/lib/axios', () => ({
  default: { post: vi.fn() },
}))

const post = vi.mocked(apiClient.post)

const ADESSO = Date.UTC(2026, 9, 9, 18, 0, 0)
const MINUTO = 60 * 1000

/** Token con la scadenza data. La firma e' finta: il browser non la verifica mai. */
function tokenCheScadeA(istante: number): string {
  const base64url = (o: unknown) =>
    btoa(JSON.stringify(o)).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '')
  const payload = { sub: 'utente-1', email: 'staff@gestora.it', exp: istante / 1000 }
  return `${base64url({ alg: 'HS256', typ: 'JWT' })}.${base64url(payload)}.firma`
}

function MostraToken() {
  const { user, logout } = useAuth()
  return (
    <>
      <span data-testid="token">{user?.token ?? 'nessuno'}</span>
      <button onClick={logout}>Esci</button>
    </>
  )
}

function avvia(token: string) {
  localStorage.setItem('token', token)
  render(
    <AuthProvider>
      <MostraToken />
    </AuthProvider>
  )
}

describe('rinnovo del token', () => {
  beforeEach(() => {
    vi.useFakeTimers()
    vi.setSystemTime(ADESSO)
    post.mockReset()
    localStorage.clear()
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('10 minuti prima della scadenza chiede un token nuovo e lo usa', async () => {
    const primo = tokenCheScadeA(ADESSO + 60 * MINUTO)
    const nuovo = tokenCheScadeA(ADESSO + 110 * MINUTO)
    post.mockResolvedValue({ data: { token: nuovo } })
    avvia(primo)

    await act(() => vi.advanceTimersByTimeAsync(50 * MINUTO - 1))
    expect(post).not.toHaveBeenCalled()

    await act(() => vi.advanceTimersByTimeAsync(1))
    expect(post).toHaveBeenCalledWith('/AuthenticationUser/rinnova-token')
    expect(screen.getByTestId('token')).toHaveTextContent(nuovo)
    expect(localStorage.getItem('token')).toBe(nuovo)
  })

  it('al limite delle 12 ore la scadenza non si allunga: non richiede piu', async () => {
    const token = tokenCheScadeA(ADESSO + 15 * MINUTO)
    post.mockResolvedValue({ data: { token: tokenCheScadeA(ADESSO + 15 * MINUTO) } })
    avvia(token)

    await act(() => vi.advanceTimersByTimeAsync(5 * MINUTO))
    await act(() => vi.advanceTimersByTimeAsync(9 * MINUTO))

    expect(post).toHaveBeenCalledTimes(1)
    expect(screen.getByTestId('token')).toHaveTextContent(token)
  })

  it('se il server non risponde riprova dopo un minuto', async () => {
    const nuovo = tokenCheScadeA(ADESSO + 80 * MINUTO)
    post.mockRejectedValueOnce(new AxiosError('Network Error')).mockResolvedValue({ data: { token: nuovo } })
    avvia(tokenCheScadeA(ADESSO + 20 * MINUTO))

    await act(() => vi.advanceTimersByTimeAsync(10 * MINUTO))
    expect(post).toHaveBeenCalledTimes(1)

    await act(() => vi.advanceTimersByTimeAsync(MINUTO))
    expect(post).toHaveBeenCalledTimes(2)
    expect(screen.getByTestId('token')).toHaveTextContent(nuovo)
  })

  it('su 401 non riprova: la sessione e finita, ci pensa l intercettore', async () => {
    const risposta401 = {
      status: 401, statusText: 'Unauthorized', data: '', headers: {},
      config: { headers: new AxiosHeaders() },
    }
    post.mockRejectedValue(new AxiosError('401', 'ERR_BAD_REQUEST', undefined, undefined, risposta401))
    avvia(tokenCheScadeA(ADESSO + 20 * MINUTO))

    await act(() => vi.advanceTimersByTimeAsync(15 * MINUTO))

    expect(post).toHaveBeenCalledTimes(1)
  })

  it('dopo il logout non rinnova piu', async () => {
    avvia(tokenCheScadeA(ADESSO + 60 * MINUTO))

    act(() => screen.getByRole('button', { name: 'Esci' }).click())
    await act(() => vi.advanceTimersByTimeAsync(60 * MINUTO))

    expect(post).not.toHaveBeenCalled()
  })
})
