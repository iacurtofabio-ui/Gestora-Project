import axios from 'axios'
import { notifySessionExpired } from '@/lib/session'

// REV-017 - senza l'indirizzo del backend Axios userebbe URL relativi: le chiamate finirebbero
// sull'host del frontend, che risponde con l'HTML della SPA, e l'utente vedrebbe errori di
// parsing incomprensibili invece della causa vera (variabile d'ambiente non configurata).
//
// La mancanza viene segnalata come *dato*, non con un throw a livello di modulo: quello
// impedirebbe il caricamento del bundle e lascerebbe la pagina bianca, senza che nessun Error
// Boundary possa intervenire. Chi monta l'app (main.tsx) mostra la schermata di spiegazione.
const baseURL = import.meta.env.VITE_API_URL

export const configurazioneMancante = !baseURL

if (configurazioneMancante) {
    console.error(
        "Configurazione mancante: la variabile d'ambiente VITE_API_URL non e' impostata. " +
        "Impostarla con l'indirizzo del backend (es. https://.../api) nel file .env.local in " +
        'sviluppo o nelle variabili di progetto in produzione, poi ricostruire il frontend.'
    )
}

const apiClient = axios.create({ baseURL: baseURL ?? '' })

apiClient.interceptors.request.use((config) => {
    const token = localStorage.getItem('token')
    if (token) {
        config.headers.Authorization = `Bearer ${token}`
    }
    return config
})

apiClient.interceptors.response.use(
    (response) => response,
    (error) => {
        // Il redirect automatico serve per la sessione scaduta (token presente ma rifiutato),
        // non per un 401 su una richiesta anonima come il login stesso: altrimenti una password
        // sbagliata provoca un reload a pagina intera invece di mostrare l'errore nel form.
        const hadToken = Boolean(error.config?.headers?.Authorization)
        if (error.response?.status === 401 && hadToken) {
            localStorage.removeItem('token')
            // REV-025: uscita pulita gestita da React (toast + navigazione). Il reload duro resta
            // solo come rete di sicurezza, se nessun componente e' in ascolto.
            if (!notifySessionExpired()) {
                window.location.href = '/login'
            }
        }
        return Promise.reject(error)
    }
)

export default apiClient
