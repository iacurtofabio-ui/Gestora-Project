# Gestora — cosa resta da fare

Aggiornato il **21/09/2026**. Questo è **l'unico elenco valido** delle cose aperte: se una cosa
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

### `UI-001` — Prova a mano del redesign «Turno»

**Cos'era.** La Fase 13 aveva dato all'app un aspetto suo, ma scritto guardando il codice: da
sistemare spaziature, proporzioni, testi.

**Cos'è diventato.** Il 09/09/2026 il punto è stato assorbito da un lavoro più grande: il
redesign **«Turno»**, tre fasi che hanno rifatto identità visiva, gerarchia e stati su tutte le
pagine. Il dettaglio è nel tracker, foglio *Appunti e Step*, blocco «REDESIGN «TURNO»».

**Cosa resta.** La lista di controllo è in **`GestoraDocs/verifica-redesign.md`** (~90 voci):
dice dove andare, cosa fare e cosa si deve vedere, in ordine di resa (le prime dieci voci sono
quelle che pagano di più).

**Fatto il 21/09/2026 (Fase 8, parziale).** Verificate con Playwright 7 delle 10 «prime voci»
più X0/X1/X6: **11 ✅, 2 ⚠️** (non verificabili da screenshot — l'animazione d'ingresso D2 e
l'evidenziazione delle tendine native X4, disegnate dal sistema operativo), **1 ❌ trovato e
corretto** (T1: con molte fasce configurate il riepilogo di Postazioni spingeva la tabella fuori
dalla prima schermata — ora scorre dentro un\'altezza massima). Esiti scritti voce per voce nel
file. **Restano da fare circa 80 voci**, non è una verifica completa: il resto — in particolare
tutti i controlli a 768px e 375px, il touch vero, i dialoghi di Zone/Fasce/Utenti, gli stati S1 e
S3-S7 — è ancora in carico a Fabio.

Per avere dati su cui provare davvero, dalla cartella `GestoraWebApi`:

```
dotnet run -- --seed-sviluppo
```

Popola il database **locale** con un dataset costruito per rompere il layout (nomi lunghi,
26 fasce, una fascia oltre il tetto, una zona senza tavoli, un utente con tre ruoli). Accesso:
`admin@gestora.local` / `Sviluppo1!`.

**30/09/2026.** Fabio ha rifatto a mano un giro di 30 prove sul portale locale: uscite 5 correzioni
(note, tavoli, disponibilità, eliminazione, seed), poi rifatto il giro: tutto ok. Restano le voci
della checklist non ancora provate.

**Poi**: i difetti che escono diventano voci nel tracker e si sistemano prima del rilascio.

> Quando si tocca un colore va sempre rilanciato `node scripts/contrasto.mjs` dentro
> `gestora-frontend/`: dice se qualche combinazione testo/fondo è diventata illeggibile.

> ⚠️ **Da non confondere con `DOC-001`.** Qui si parla di **come si vede** l'app, e sono cose
> notate adesso. `DOC-001` riguarda il file di appunti che Fabio tiene da settimane su **come
> funziona**, e quel file resta chiuso.

---

> `DOC-001` (formalizzare `AppuntiFix.txt`) è **chiusa il 21/09/2026**: le 13 righe del file sono
> state lette e ognuna è diventata lavoro in una fase di questa chiusura (o una voce qui sotto,
> per quelle non ancora fatte). La tabella riga-per-riga è in `GestoraDocs/CONSEGNA_v1.1.md`,
> sezione Fase 10. `AppuntiFix.txt` **non è stato toccato** (resta il file personale di Fabio).
> L'unica riga non ancora chiusa è la prima, **«SEPARARE FE E BE»**: preparazione in corso, vedi
> `RUNBOOK.md` §9 quando sarà scritto (Fase 11).

---

### `OPS-001` — Reset completo dei due database

*(era `NEW-005`)*

**Cos'è.** Cancellare e ricreare da zero il database locale **e** quello di produzione, per
togliere di mezzo tutti i dati di prova accumulati: la zona "Test concorrenza", i tavoli e le
prenotazioni finte, e in locale gli utenti `testfase6` e `fase13check` (quest'ultimo creato
l'08/09/2026 per provare il tema scuro sulle pagine dietro l'accesso — **solo database locale**,
la produzione non è stata toccata).

**Quando.** **Alla fine**, quando non ci sono più implementazioni né fix da fare. Farlo prima
significa ricreare dati di test e rifarlo daccapo.

**Perché un reset e non una cancellazione mirata.** Perché la cancellazione mirata **si blocca da
sola**, ed è utile capire il motivo:

- una prenotazione in stato `Completata` non si può **né eliminare** (dal 30/09/2026 il server
  accetta l'eliminazione solo per `Annullata`) **né annullare** (le completate vengono
  rifiutate). Stessa cosa per `NonPresentata`: resta nello storico.
- quindi la sua riga di collegamento con il tavolo resta viva
- e il tavolo non si elimina finché esiste **una qualsiasi** riga di collegamento, anche vecchia
- e la zona non si elimina finché ha ancora un tavolo assegnato

Vicolo cieco completo, uscibile solo scrivendo direttamente nel database.

**Procedura**: in `RUNBOOK.md`, sezione *Reset dei database*. Ha tre punti che si dimenticano
sempre — leggila, non andare a memoria.

---

### `SEC-001` — Avviso di sicurezza su AutoMapper 12.0.1

**Cos'è.** `dotnet list package --vulnerable --include-transitive` segnala un avviso di gravità
**High** (`GHSA-rvv3-g6hj-g44x`) sulla versione di AutoMapper usata (dipendenza transitiva di
`AutoMapper.Extensions.Microsoft.DependencyInjection`, riferimento diretto in
`GestoraWebApi.csproj`). Trovato il 21/09/2026, non c'era in precedenza.

**Perché non è stato aggiornato subito.** La versione che risolve l'avviso è **16.2.0** — un
salto di 4 versioni maggiori, non una patch. AutoMapper ha cambiato modello di licenza dalla 13
in poi (uso commerciale a pagamento oltre una soglia di fatturato dell'azienda che lo usa): è una
decisione che riguarda anche Fabio, non solo tecnica, e comunque un salto così grande va provato
con calma, non incluso di corsa in una chiusura.

**Decisione da prendere**: aggiornare (verificando prima le condizioni di licenza per l'uso di
Gestora) oppure restare sulla 12.x accettando l'avviso aperto (non è sfruttabile da remoto,
riguarda un dettaglio interno della libreria).

---

## 🟡 Pulizie senza fretta

Nessuna di queste tocca l'applicazione che gira. Si possono fare in qualsiasi momento.

| Sigla | Cosa | Nota |
|---|---|---|
| `OPS-002` | Cancellare la cartella `Personali\Gestora_BACKUP_20260903` | Copia di sicurezza fatta prima di riscrivere la storia di Git. Non serve più |
| `OPS-003` | Lanciare `git gc --prune=now` a computer appena riavviato | Restano oggetti orfani **solo in locale** da una riscrittura interrotta. È solo spazio su disco |
| `OPS-004` | Riallineare il tag `v1.0.0` fra il PC e GitHub | In locale punta a un commit, su GitHub a un altro: effetto della riscrittura della storia. Il contenuto è identico, cambia solo il codice del commit. Si sistema con `git push --force origin refs/tags/v1.0.0` |

> `OPS-005` (devtools fra le dipendenze di sviluppo) è **chiusa il 21/09/2026**, Fase 9.

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
