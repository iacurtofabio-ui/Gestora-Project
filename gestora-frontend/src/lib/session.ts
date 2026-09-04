/**
 * REV-025 — ponte fra l'interceptor Axios (che vive fuori da React) e l'albero dei componenti.
 *
 * Prima la scadenza della sessione veniva gestita con `window.location.href = '/login'`: un
 * ricaricamento a pagina intera, senza alcun messaggio, che faceva sembrare l'app crashata.
 * Qui l'interceptor si limita a segnalare l'evento; chi e' dentro React decide come reagire
 * (logout, pulizia della cache, toast, navigazione).
 */
type SessionExpiredHandler = () => void

let handler: SessionExpiredHandler | null = null

/** Registra il gestore. Restituisce la funzione di deregistrazione, comoda in useEffect. */
export function onSessionExpired(nuovoHandler: SessionExpiredHandler): () => void {
    handler = nuovoHandler
    return () => {
        if (handler === nuovoHandler) handler = null
    }
}

/**
 * Restituisce false se nessuno e' in ascolto: in quel caso il chiamante deve ripiegare sul
 * vecchio comportamento (redirect duro), per non lasciare l'utente su una pagina morta.
 */
export function notifySessionExpired(): boolean {
    if (!handler) return false
    handler()
    return true
}
