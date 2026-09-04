import { useRouteError, isRouteErrorResponse } from 'react-router-dom'
import ErrorScreen from '@/components/ErrorScreen'

/**
 * REV-014 — errorElement delle route.
 *
 * Senza questo, un'eccezione dentro una pagina viene raccolta dall'error boundary interno di
 * React Router, che mostra "Unexpected Application Error!" con lo stack trace: illeggibile per
 * l'utente e, in produzione, anche inopportuno. L'ErrorBoundary a classe in main.tsx non entra
 * in gioco qui, perche' React Router intercetta prima.
 */
export default function RouteErrorPage() {
    const errore = useRouteError()

    console.error('Errore non gestito in una route:', errore)

    let messaggio: string | undefined
    if (isRouteErrorResponse(errore)) {
        messaggio = `${errore.status} ${errore.statusText}`
    } else if (errore instanceof Error) {
        messaggio = errore.message
    }

    return <ErrorScreen messaggio={messaggio} />
}
