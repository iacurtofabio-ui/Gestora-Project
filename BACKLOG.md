# Gestora — cosa resta da fare

Aggiornato il **01/10/2026**. Questo è **l'unico elenco valido** delle cose aperte: se una cosa
non è scritta qui, non è in programma.

Il foglio *Fix e Bug* del tracker resta il registro dettagliato dei difetti; questo file è la
vista d'insieme che si guarda per decidere cosa fare.

> Le sigle vecchie (`REV-xxx`, `NEW-xxx`, `FIX-xxx`) restano valide come riferimento storico.
> Sono spiegate in `docs/archivio/REVISIONE_END_TO_END.md` e nel tracker.

> `OPS-006` (migrazione da Railway ad Azure/Neon) è **chiusa il 18/09/2026**: verifica
> end-to-end fatta (setup Admin, 3 ruoli, prenotazione con assegnazione tavolo, job Quartz),
> progetto Railway eliminato. Dettaglio in `CLAUDE.md` §1 e `docs/archivio/STORICO_FASI.md`.

---

## 🔵 Da fare prima

> `CAP-001` (tetto dei coperti non garantito) è **chiusa il 18/09/2026**: lock `FOR UPDATE`
> sulla riga della fascia dentro la transazione, validazione di `UpdateAsync` spostata dentro la
> transazione, sforamento esposto in dashboard (`copertiOltreIlTetto`). Prova manuale in
> `RUNBOOK.md` §2. Dettaglio in `GestoraDocs/CONSEGNA_v1.1.md`.

---

> `UI-001` (prova a mano del redesign «Turno») è **chiusa il 01/10/2026**: checklist di
> `GestoraDocs/verifica-redesign.md` terminata.


> `DOC-001` (formalizzare `AppuntiFix.txt`) è **chiusa il 21/09/2026**: le 13 righe del file sono
> state lette e ognuna è diventata lavoro in una fase di questa chiusura (o una voce qui sotto,
> per quelle non ancora fatte). La tabella riga-per-riga è in `GestoraDocs/CONSEGNA_v1.1.md`,
> sezione Fase 10. `AppuntiFix.txt` **non è stato toccato** (resta il file personale di Fabio).
> L'unica riga non ancora chiusa è la prima, **«SEPARARE FE E BE»**: preparazione in corso, vedi
> `RUNBOOK.md` §10.

---

> `OPS-001` (reset dei due database) è **chiusa il 01/10/2026**: locale con `dotnet ef database drop`,
> Neon con `DROP SCHEMA public CASCADE` (il ruolo `neondb_owner` può non riuscire a cancellare il database
> stesso), poi migration e `quartz_postgres.sql`. Admin ricreato dalla schermata di primo avvio, `/health` ok.
> Il `pg_dump` locale (v17) non funziona con il server Neon (v18): per un backup serve il client 18
> oppure un branch Neon. Procedura in `RUNBOOK.md` §3.


> `SEC-001` (AutoMapper 12.0.1, avviso `GHSA-rvv3-g6hj-g44x`) è **chiusa il 01/10/2026**:
> deciso di **non aggiornare**. La 16.2.0 che risolve l'avviso ha una licenza a pagamento oltre una
> soglia di fatturato, e non si vuole pagare nessuna licenza. Si resta sulla 12.x accettando
> l'avviso (non sfruttabile da remoto). Da riaprire solo se si cambia libreria di mapping.

---

## 🟡 Pulizie senza fretta

Nessuna di queste tocca l'applicazione che gira. Si possono fare in qualsiasi momento.

| Sigla | Cosa | Nota |
|---|---|---|
| `OPS-003` | Lanciare `git gc --prune=now` a computer appena riavviato | Restano oggetti orfani **solo in locale** da una riscrittura interrotta. È solo spazio su disco |
| `OPS-004` | Riallineare il tag `v1.0.0` fra il PC e GitHub | In locale punta a un commit, su GitHub a un altro: effetto della riscrittura della storia. Il contenuto è identico, cambia solo il codice del commit. Si sistema con `git push --force origin refs/tags/v1.0.0` |

> `OPS-005` (devtools fra le dipendenze di sviluppo) è **chiusa il 21/09/2026**, Fase 9.
> `OPS-002` (cartella `Gestora_BACKUP_20260903`) è **chiusa il 01/10/2026**: cancellata.

---

## ⚪ Idee per la v2.0

Recuperate dalla roadmap di revisione, dove erano state messe **fuori** dalla v1 con decisione
esplicita. Non sono impegni: sono la lista da cui pescare se il progetto riparte.

**Prenotazioni**
- Turnover del tavolo: durata della seduta, due turni nella stessa fascia
- Lista d'attesa quando la fascia è piena
- No-show come stato vero, con storico e regole per cliente
- Overbooking controllato per fascia
- Zona come preferenza con ripiego, invece che come vincolo
- Chiusure straordinarie e orari speciali

**Sala**
- Creazione automatica dei tavoli in base ai coperti richiesti *(era la decisione 7)*
- Unione e separazione tavoli come azione manuale dello Staff
- Campo `PostiCapotavola` sul singolo tavolo, se un giorno serve precisione piena sul bonus
  testate anche per le unioni miste

**Utenti e comunicazione**
- Email di conferma e promemoria
- Recupero password autonomo
- Sessione con rinnovo automatico del token
- Autenticazione con cookie `HttpOnly` + protezione CSRF, al posto del token in `localStorage`.
  È una riprogettazione, non un fix: il token in `localStorage` non è un bug (vedi `RUNBOOK.md`
  §9), ma un cookie `HttpOnly` toglie anche la possibilità teorica di leggerlo da JavaScript

**Altro**
- Export CSV/PDF dei report — ⚠️ oggi **non esiste**, esistono solo i due endpoint della
  dashboard. Il tracker lo dava per fatto: corretto l'08/09/2026 (era la segnalazione `REV-084`)
- Gestione di più locali sullo stesso impianto
- Vincolo "una prenotazione al giorno" per il Cliente a livello di database *(era la decisione
  10)*: serve una colonna che distingua chi ha creato la prenotazione. Da riprendere insieme alla
  progettazione dell'app dedicata al cliente, non da sola

---

## 🚫 Deciso di NON fare

Perché torni fuori ogni volta che qualcuno rilegge il codice.

**`REV-056` — allineare il nome "fascia oraria" ovunque.** Oggi lo stesso concetto si chiama in
quattro modi diversi fra namespace, classe, indirizzo dell'endpoint e tabella. Sistemarlo del
tutto vorrebbe dire cambiare l'indirizzo di un endpoint, cioè rompere tutti i collegamenti del
frontend, per un guadagno di sola leggibilità. Sistemate solo cartelle e namespace in Fase 9,
l'indirizzo resta com'è.

**`REV-004` — vincolo "una prenotazione al giorno" nel database.** È la decisione di prodotto 10:
resta un controllo dell'applicazione per tutta la v1. Il rischio residuo è accettato perché il
canale self-service del cliente non è ancora quello vero. **Non copre** il rischio del doppio
tavolo, che è invece già risolto con un vincolo vero nel database.

**Test automatici di concorrenza.** Il database finto usato nei test non applica gli indici
unici, e introdurre un database vero nei test è sproporzionato. La verifica resta manuale.
