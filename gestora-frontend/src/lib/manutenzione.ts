/**
 * V2-016 — «portale in manutenzione». Con `VITE_MANUTENZIONE=true` (variabile del progetto su
 * Vercel) l'app non si monta: ogni pagina mostra solo la schermata di manutenzione e nessuna
 * chiamata parte verso il backend, che in quel periodo risponde comunque 503.
 *
 * Come `VITE_API_URL`, il valore viene incorporato al momento della build: cambiarlo su Vercel
 * richiede una nuova pubblicazione.
 */
export function leggiManutenzione(valore: string | undefined): boolean {
  return valore?.trim().toLowerCase() === 'true'
}

export const inManutenzione = leggiManutenzione(import.meta.env.VITE_MANUTENZIONE)
