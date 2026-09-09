# Gestora — cosa resta da fare

Aggiornato il **09/09/2026**. Questo è **l'unico elenco valido** delle cose aperte: se una cosa
non è scritta qui, non è in programma.

Il foglio *Fix e Bug* del tracker resta il registro dettagliato dei difetti; questo file è la
vista d'insieme che si guarda per decidere cosa fare.

> Le sigle vecchie (`REV-xxx`, `NEW-xxx`, `FIX-xxx`) restano valide come riferimento storico.
> Sono spiegate in `docs/archivio/REVISIONE_END_TO_END.md` e nel tracker.

---

## 🔵 Da fare prima

### `CAP-001` — Il tetto dei coperti non è garantito

**Cos'è.** Il limite di coperti di una fascia è controllato **solo dal codice applicativo**
(`PrenotazioniService.ValidatePrenotazioneAsync`). Nel database non c'è niente che lo imponga: a
differenza del tavolo, protetto dall'indice unico `UX_PrenotazionePostazione_Slot`, i coperti non
hanno un vincolo equivalente.

**Perché è un problema.** Il controllo è una `SUM` sulle prenotazioni esistenti seguita da un
`INSERT`. PostgreSQL gira in `READ COMMITTED` e `BeginTransactionAsync()` non chiede un livello
diverso: due prenotazioni simultanee sulla stessa fascia possono entrambe leggere «48 su 52»,
entrambe passare il controllo per 4 coperti, ed entrambe scrivere. Risultato: 56 su 52.

È la stessa corsa che per i tavoli era stata chiusa con l'indice unico. Per i coperti manca.

**Emerso il 09/09/2026** verificando perché il seed di sviluppo aveva prodotto una fascia con 61
coperti su 52. Il seed è stato corretto (scriveva saltando il servizio), ma la verifica ha fatto
trovare il buco vero.

**Cosa fare — tre punti.**
1. **Rendere il tetto un vincolo vero.** La strada più semplice è bloccare la riga della fascia
   (`SELECT ... FOR UPDATE`) prima della `SUM`, dentro la transazione: serializza le prenotazioni
   sulla stessa fascia, che è esattamente ciò che serve.
2. **Spostare `ValidatePrenotazioneAsync` dentro la transazione anche in `UpdateAsync`.** In
   `AddAsync` sta già dentro; in modifica sta fuori (riga ~161), quindi lì la finestra è più larga.
   Allineamento a costo zero.
3. **Smettere di nascondere lo sforamento.** `DashboardService` riga 101 calcola
   `Math.Max(0, MaxCoperti - copertiPrenotati)`: uno sforamento si legge «0 disponibili», identico
   a una fascia esattamente piena. Un dato incoerente va mostrato, non schiacciato — altrimenti se
   il punto 1 fallisce nessuno se ne accorge.

**Come provarlo.** `dotnet run -- --seed-sviluppo --stato-incoerente` scrive lo stato incoerente
di proposito (saltando il servizio), per vedere come reagisce l'interfaccia.

---


### `UI-001` — Prova a mano del redesign «Turno»

**Cos'era.** La Fase 13 aveva dato all'app un aspetto suo, ma scritto guardando il codice: da
sistemare spaziature, proporzioni, testi.

**Cos'è diventato.** Il 09/09/2026 il punto è stato assorbito da un lavoro più grande: il
redesign **«Turno»**, tre fasi che hanno rifatto identità visiva, gerarchia e stati su tutte le
pagine. Il dettaglio è nel tracker, foglio *Appunti e Step*, blocco «REDESIGN «TURNO»».

**Cosa resta.** La verifica a schermo, che nessuno ha ancora fatto — il redesign è stato
scritto e misurato, non guardato. È in carico a Fabio, con la lista di controllo in
**`GestoraDocs/verifica-redesign.md`**: dice dove andare, cosa fare e cosa si deve vedere, ed è
in ordine di resa (le prime dieci voci sono quelle che pagano di più).

Per avere dati su cui provare davvero, dalla cartella `GestoraWebApi`:

```
dotnet run -- --seed-sviluppo
```

Popola il database **locale** con un dataset costruito per rompere il layout (nomi lunghi,
26 fasce, una fascia oltre il tetto, una zona senza tavoli, un utente con tre ruoli). Accesso:
`admin@gestora.local` / `Sviluppo1!`.

**Poi**: i difetti che escono diventano voci nel tracker e si sistemano prima del rilascio.

> Quando si tocca un colore va sempre rilanciato `node scripts/contrasto.mjs` dentro
> `gestora-frontend/`: dice se qualche combinazione testo/fondo è diventata illeggibile.

> ⚠️ **Da non confondere con `DOC-001`.** Qui si parla di **come si vede** l'app, e sono cose
> notate adesso. `DOC-001` riguarda il file di appunti che Fabio tiene da settimane su **come
> funziona**, e quel file resta chiuso.

---

### `DOC-001` — Formalizzare il file di appunti d'uso

**Cos'è.** Usando l'app tutti i giorni, Fabio ha annotato dei miglioramenti in un file personale
(`AppuntiFix.txt`, nella cartella principale). Sono cose emerse dall'uso vero, quindi
probabilmente le più utili di tutto quello che resta.

**Perché non è già stato fatto.** Per decisione dell'08/09/2026 il file **non si apre** finché
non parte la fase dedicata: la Fase 11 doveva restare una chiusura pulita, non diventare un
contenitore.

**Come si fa.**
1. Fabio passa il file, si leggono i punti uno per uno
2. Ognuno diventa una voce nel tracker con una sigla e una priorità
3. Si separano in due gruppi, perché costano molto diversamente:
   - **solo frontend** — si sistemano in fretta
   - **backend + frontend** — vanno pianificate
4. Si decide cosa entra nella prima fase di implementazione

**Nessun codice si scrive in quella sessione**: si decide soltanto.

**Due punti già noti**, citati a voce e da non perdere:
- leggibilità delle fasce orarie — *solo frontend*
- coperti per tavolo: il dato esiste già nel database ma il server non lo manda al frontend —
  *backend + frontend*

Il merito non si discute adesso: si decide quando si apre il file.

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

- una prenotazione in stato `Completata` non si può **né eliminare** (il server accetta solo
  `Attiva` e `Annullata`) **né annullare** (le completate vengono rifiutate)
- quindi la sua riga di collegamento con il tavolo resta viva
- e il tavolo non si elimina finché esiste **una qualsiasi** riga di collegamento, anche vecchia
- e la zona non si elimina finché ha ancora un tavolo assegnato

Vicolo cieco completo, uscibile solo scrivendo direttamente nel database.

**Procedura**: in `RUNBOOK.md`, sezione *Reset dei database*. Ha tre punti che si dimenticano
sempre — leggila, non andare a memoria.

---

## 🟡 Pulizie senza fretta

Nessuna di queste tocca l'applicazione che gira. Si possono fare in qualsiasi momento.

| Sigla | Cosa | Nota |
|---|---|---|
| `OPS-002` | Cancellare la cartella `Personali\Gestora_BACKUP_20260903` | Copia di sicurezza fatta prima di riscrivere la storia di Git. Non serve più |
| `OPS-003` | Lanciare `git gc --prune=now` a computer appena riavviato | Restano oggetti orfani **solo in locale** da una riscrittura interrotta. È solo spazio su disco |
| `OPS-004` | Riallineare il tag `v1.0.0` fra il PC e GitHub | In locale punta a un commit, su GitHub a un altro: effetto della riscrittura della storia. Il contenuto è identico, cambia solo il codice del commit. Si sistema con `git push --force origin refs/tags/v1.0.0` |
| `OPS-005` | Spostare `@tanstack/react-query-devtools` fra le dipendenze di sviluppo | Oggi sta fra quelle di produzione, dove non dovrebbe. Guadagno nullo (dal pacchetto pubblicato è già assente) e c'è un rischio: se un giorno Vercel installasse saltando le dipendenze di sviluppo, la build si romperebbe. Da fare solo se si tocca comunque quella parte |

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
