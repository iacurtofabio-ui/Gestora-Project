import { SchermataMessaggio } from '@/components/SchermataMessaggio'

/**
 * REV-017 — schermata mostrata quando manca l'indirizzo del backend.
 *
 * Il primo tentativo era un `throw` a livello di modulo in lib/axios.ts: il messaggio finiva in
 * console, ma l'eccezione impediva il caricamento dell'intero bundle e la pagina restava
 * **bianca** - lo stesso sintomo che la Fase 6 doveva eliminare, e nessun Error Boundary puo'
 * intercettarlo perche' l'app non arriva nemmeno a montarsi. Qui invece l'errore e' un dato:
 * l'app non parte, ma al suo posto si vede cosa manca e come rimediare.
 *
 * Chi legge questa schermata sta configurando l'applicazione, non prendendo prenotazioni: e'
 * l'unico punto del progetto in cui il testo puo' essere tecnico.
 */
function Codice({ children }: { children: string }) {
  return <code className="rounded-sm bg-muted px-1 py-0.5 text-foreground">{children}</code>
}

export default function ConfigurazioneMancante() {
  return (
    <SchermataMessaggio tono="errore" titolo="Manca l'indirizzo del server">
      <p>
        Gestora non sa a chi chiedere i dati: la variabile <Codice>VITE_API_URL</Codice> non e'
        impostata. Finche' manca, nessuna schermata puo' funzionare.
      </p>
      <p className="text-foreground">Come rimediare</p>
      <ul className="list-disc space-y-2 pl-5">
        <li>
          In locale: aggiungi <Codice>VITE_API_URL=http://localhost:5099/api</Codice> al file{' '}
          <Codice>.env.local</Codice> e riavvia il server di sviluppo.
        </li>
        <li>
          In produzione: impostala fra le variabili di progetto dell'hosting e ricostruisci il
          frontend. Il valore viene incorporato al momento della build, non letto a ogni avvio —
          per questo cambiarlo senza ricostruire non ha effetto.
        </li>
      </ul>
    </SchermataMessaggio>
  )
}
