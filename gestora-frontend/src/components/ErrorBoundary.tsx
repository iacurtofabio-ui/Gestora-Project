import { Component, type ErrorInfo, type ReactNode } from 'react'
import ErrorScreen from '@/components/ErrorScreen'

type Props = { children: ReactNode }
type State = { errore: Error | null }

/**
 * REV-014 — rete di sicurezza contro la schermata bianca, per quello che sta *fuori* dal router:
 * in pratica AuthProvider, che e' proprio il punto in cui un token corrotto in localStorage
 * faceva fallire il primo render lasciando la pagina vuota e irrecuperabile.
 *
 * Gli errori *dentro* le route non passano di qui: li intercetta l'error boundary interno di
 * React Router, per cui esiste RouteErrorPage agganciata come errorElement.
 *
 * Un Error Boundary deve essere un componente a classe: React non offre un equivalente con gli
 * hook, e' l'unico caso in cui in questo progetto si usa una classe.
 */
export default class ErrorBoundary extends Component<Props, State> {
    state: State = { errore: null }

    static getDerivedStateFromError(errore: Error): State {
        return { errore }
    }

    componentDidCatch(errore: Error, info: ErrorInfo) {
        // In produzione la console del browser e' l'unico posto dove questo errore resta
        // visibile: il backend non lo vede, non essendoci nessuna chiamata di segnalazione.
        console.error('Errore non gestito nel render:', errore, info.componentStack)
    }

    render() {
        if (!this.state.errore) return this.props.children
        return <ErrorScreen messaggio={this.state.errore.message} />
    }
}
