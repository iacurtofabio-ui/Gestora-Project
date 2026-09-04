/**
 * REV-017 — schermata mostrata quando manca l'indirizzo del backend.
 *
 * Il primo tentativo era un `throw` a livello di modulo in lib/axios.ts: il messaggio finiva in
 * console, ma l'eccezione impediva il caricamento dell'intero bundle e la pagina restava
 * **bianca** - lo stesso sintomo che la Fase 6 doveva eliminare, e nessun Error Boundary puo'
 * intercettarlo perche' l'app non arriva nemmeno a montarsi. Qui invece l'errore e' un dato:
 * l'app non parte, ma al suo posto si vede cosa manca e come rimediare.
 */
export default function ConfigurazioneMancante() {
    return (
        <div className="min-h-screen flex items-center justify-center p-6 bg-gray-50">
            <div className="bg-white border rounded-lg p-6 max-w-lg w-full">
                <h1 className="text-lg font-semibold mb-2">Configurazione mancante</h1>
                <p className="text-sm text-gray-600 mb-4">
                    L'applicazione non sa a quale server rivolgersi: la variabile d'ambiente{' '}
                    <code className="bg-gray-100 px-1 rounded">VITE_API_URL</code> non e' impostata.
                    Finche' manca, nessuna funzione puo' funzionare.
                </p>
                <p className="text-sm text-gray-600 mb-2">Come rimediare:</p>
                <ul className="text-sm text-gray-600 list-disc pl-5 space-y-1">
                    <li>
                        in sviluppo, aggiungere{' '}
                        <code className="bg-gray-100 px-1 rounded">VITE_API_URL=http://localhost:5099/api</code>{' '}
                        al file <code className="bg-gray-100 px-1 rounded">.env.local</code> e riavviare;
                    </li>
                    <li>
                        in produzione, impostarla fra le variabili di progetto dell'hosting e
                        ricostruire il frontend: il valore viene incorporato al momento della build,
                        non letto a ogni avvio.
                    </li>
                </ul>
            </div>
        </div>
    )
}
