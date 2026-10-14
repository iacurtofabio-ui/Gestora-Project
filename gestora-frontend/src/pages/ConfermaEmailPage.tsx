import { useEffect, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import apiClient from '@/lib/axios'
import { Endpoints } from '@/lib/endpoints'
import { messaggioErroreCaricamento } from '@/lib/apiError'
import { Button } from '@/components/ui/button'
import { PageLoading } from '@/components/PageState'
import { SchermataMessaggio } from '@/components/SchermataMessaggio'

type Esito = { stato: 'in-corso' } | { stato: 'ok' } | { stato: 'errore'; messaggio: string }

const LINK_NON_VALIDO = 'Il link di conferma non è valido. Apri di nuovo il link dell’email.'

/**
 * V2-003 — pagina a cui porta il link dell'email di conferma (`?userId=…&token=…`). Pubblica:
 * chi clicca non ha ancora fatto il login. La conferma parte da sola all'apertura. Cliccare due
 * volte lo stesso link non dà errore: il server risponde ok se l'email è già confermata.
 */
export default function ConfermaEmailPage() {
  const [params] = useSearchParams()
  const userId = params.get('userId')
  const token = params.get('token')

  const [esito, setEsito] = useState<Esito>(
    userId && token ? { stato: 'in-corso' } : { stato: 'errore', messaggio: LINK_NON_VALIDO }
  )

  useEffect(() => {
    if (!userId || !token) return
    let annullato = false

    apiClient
      .post(Endpoints.auth.confermaEmail, { userId, token })
      .then(() => {
        if (!annullato) setEsito({ stato: 'ok' })
      })
      .catch((errore) => {
        if (annullato) return
        setEsito({
          stato: 'errore',
          messaggio: messaggioErroreCaricamento(errore, 'Conferma non riuscita. Riprova.'),
        })
      })

    return () => {
      annullato = true
    }
  }, [userId, token])

  if (esito.stato === 'in-corso') {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <PageLoading />
      </div>
    )
  }

  if (esito.stato === 'ok') {
    return (
      <SchermataMessaggio
        titolo="Email confermata"
        azioni={
          <Button asChild>
            <Link to="/login">Accedi</Link>
          </Button>
        }
      >
        <p>L’account è attivo. Ora puoi accedere e prenotare.</p>
      </SchermataMessaggio>
    )
  }

  return (
    <SchermataMessaggio
      titolo="Non riesco a confermare l’email"
      tono="errore"
      azioni={
        <Button asChild>
          <Link to="/login">Vai all’accesso</Link>
        </Button>
      }
    >
      <p>{esito.messaggio}</p>
      <p>Dalla pagina di accesso puoi farti rimandare un link nuovo.</p>
    </SchermataMessaggio>
  )
}
