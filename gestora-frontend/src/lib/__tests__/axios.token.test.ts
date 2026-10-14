import { describe, it, expect, beforeEach } from 'vitest'
import type { InternalAxiosRequestConfig } from 'axios'
import apiClient from '@/lib/axios'
import { Endpoints } from '@/lib/endpoints'

/**
 * V2-003 — quali richieste portano il token. Con `register` senza token l'Admin che crea uno Staff
 * arriverebbe al server come anonimo: account da confermare e nessuna esenzione dal limite.
 */
async function intestazioneInviata(url: string): Promise<unknown> {
  let inviata: unknown
  await apiClient.post(url, {}, {
    adapter: async (config: InternalAxiosRequestConfig) => {
      inviata = config.headers.Authorization
      return { data: '', status: 200, statusText: 'OK', headers: {}, config }
    },
  })
  return inviata
}

describe('token nelle richieste', () => {
  beforeEach(() => {
    localStorage.setItem('token', 'abc')
  })

  it('register porta il token: il server riconosce l’Admin', async () => {
    expect(await intestazioneInviata(Endpoints.auth.register)).toBe('Bearer abc')
  })

  it.each([Endpoints.auth.login, Endpoints.auth.confermaEmail, Endpoints.auth.reinviaConferma])(
    '%s non porta il token',
    async (url) => {
      expect(await intestazioneInviata(url)).toBeUndefined()
    }
  )
})
