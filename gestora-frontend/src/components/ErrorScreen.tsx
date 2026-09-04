/**
 * REV-014 — schermata di errore condivisa.
 *
 * La usano in due: l'ErrorBoundary a classe, che copre quello che sta *fuori* dal router
 * (AuthProvider compreso, il punto in cui un dato di sessione corrotto faceva pagina bianca), e
 * RouteErrorPage, che copre gli errori *dentro* le route. Servono entrambi: React Router ha un
 * proprio error boundary che intercetta gli errori delle pagine prima che arrivino a quello di
 * React, e senza un errorElement mostrerebbe la sua schermata di sviluppo con lo stack trace.
 */
export default function ErrorScreen({ messaggio }: { messaggio?: string }) {
    function ripartiDaLogin() {
        localStorage.removeItem('token')
        window.location.href = '/login'
    }

    return (
        <div className="min-h-screen flex items-center justify-center p-6 bg-gray-50">
            <div className="bg-white border rounded-lg p-6 max-w-md w-full">
                <h1 className="text-lg font-semibold mb-2">Si e' verificato un errore</h1>
                <p className="text-sm text-gray-600 mb-4">
                    L'applicazione non e' riuscita a mostrare questa pagina. Puoi riprovare a
                    caricarla oppure ripartire dall'accesso.
                </p>
                {messaggio && (
                    <p className="text-xs text-gray-400 mb-4 break-words">{messaggio}</p>
                )}
                <div className="flex gap-2 justify-end">
                    <button
                        type="button"
                        onClick={() => window.location.reload()}
                        className="px-4 py-2 text-sm border rounded"
                    >
                        Ricarica
                    </button>
                    <button
                        type="button"
                        onClick={ripartiDaLogin}
                        className="px-4 py-2 text-sm bg-blue-500 text-white rounded"
                    >
                        Torna al login
                    </button>
                </div>
            </div>
        </div>
    )
}
