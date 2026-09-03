# Roadmap delle sistemazioni — Gestora

Data: 28/08/2026 · Revisione architetturale: 31/08/2026 · Fonte: `REVISIONE_END_TO_END.md`
Obiettivo: chiudere la v1 in modo pulito, senza difetti noti aperti.

**Legenda**
- 🧑 **Fabio** — task che devi fare tu
- 🤖 **Claude** — task che faccio io

**Tracciabilità**: ogni segnalazione di `REVISIONE_END_TO_END.md` ha un ID `REV-001`…`REV-097`
(numerati il 31/08/2026). Le fasi sotto richiamano l'ID quando il collegamento è puntuale. Le
segnalazioni di priorità bassa più numerose (naming, refusi, duplicazioni minori) sono coperte a
blocco per fase invece che riga per riga — sono troppe per essere utili citate una a una — ma
nessuna resta orfana: il dettaglio completo di ognuna è in `REVISIONE_END_TO_END.md` §3.

---

## Definition of Done

Una fase è chiusa solo quando **tutti** questi punti sono veri, non solo il primo:

1. `dotnet test` e `npm run build` verdi
2. Ogni ID REV assegnato alla fase è chiuso, oppure riassegnato ad altra fase/backlog v2.0 con
   motivazione scritta — mai lasciato in sospeso senza decisione
3. I task 🧑 della fase sono stati eseguiti e l'esito è scritto qui o nel tracker
4. Tracker Excel (tutti i fogli) e blocco di stato di `CLAUDE.md` aggiornati
5. Commit e push fatti

Senza il punto 3, una fase con task Fabio resta formalmente aperta anche se tutto il codice è
scritto e funzionante — "Completato" ha avuto tre significati diversi in questo progetto
(REV-087), non deve succedere di nuovo qui.

---

## Decisioni prese — 28/08/2026 (integrate il 31/08/2026)

Queste scelte sono già state fatte e valgono per tutto il percorso. Non si riaprono.

1. **Capienza della fascia oraria** = numero massimo di **coperti**, non di prenotazioni. Il campo
   viene rinominato `MaxCoperti` ovunque. Risolve REV-005.
2. **Modifiche al database**: restano manuali. Preparo io la migration, la applichi tu quando
   decidi. Le fasi che ne richiedono una sono la 2 e la 3 — seguono la procedura descritta più
   sotto.
3. **Assegnazione dei tavoli**: cambia logica. L'unità base è il tavolo da 2; i tavoli si uniscono
   in base ai coperti richiesti. **Il bonus di 2 posti per le testate si applica solo quando
   l'unione è composta esclusivamente da tavoli da 2 posti** (2 tavoli = 6 posti, 3 tavoli = 8
   posti — somma delle capienze + 2). Per qualunque unione che include almeno un tavolo di
   capienza diversa da 2, la capacità è la **somma semplice**, senza bonus — vedi nota sotto.
4. **Criterio di scelta**: sempre la soluzione con **meno posti sprecati**, tavolo singolo o
   unione che sia. Se resta libero solo un tavolo grande, viene assegnato comunque: rifiutare un
   cliente è peggio che sprecare posti. La ricerca della combinazione migliore resta vincolata a
   **massimo 4 tavoli per unione** (limite fisico realistico di una sala, e anche il modo per
   evitare che la ricerca esploda in combinazioni su una sala grande).
5. **Capienza dei tavoli**: qualsiasi numero da 1 in su. Sparisce il vincolo che oggi ammette solo
   2, 4 e 8, così ogni locale mappa la sala com'è davvero.
6. **Primo amministratore**: creato da una schermata di primo avvio, non più da un endpoint
   pubblico.
7. **Creazione automatica delle postazioni**: rinviata al backlog v2.0. Insieme alla decisione 5
   (capienza libera, sparisce il vincolo {2,4,8}) risolve anche la contraddizione mai chiusa tra
   "creazione dinamica delle postazioni" e capienza fissa a {2,4,8} — REV-090.
8. **Chi comanda sulla disponibilità**: il tetto della fascia. Si possono creare quanti tavoli si
   vuole, ma è `MaxCoperti` a dire quando la fascia è esaurita. I tavoli servono solo ad assegnare
   fisicamente il posto, non a definire la capienza. Nessun blocco di coerenza fra i due numeri.
9. **Riepilogo sala**: la pagina Postazioni mostra in cima il quadro d'insieme — tavoli attivi,
   posti totali e, per ogni fascia, se i tavoli coprono il tetto dichiarato. È solo informativo.
10. **"Una prenotazione al giorno" per il Cliente resta un controllo applicativo, non un vincolo a
    database, per tutta la v1.** Gestora oggi è pensata per l'uso di gestore/dipendenti; il
    self-service del Cliente non è ancora il canale operativo reale — arriverà con una futura
    app/API dedicata. Il rischio residuo di race condition su questo specifico vincolo (REV-004)
    è accettato come basso finché resta così, e va riaperto insieme alla progettazione della
    futura interfaccia cliente, non isolatamente prima. **Non copre** la race condition sul doppio
    tavolo (REV-003), che resta nella Fase 3 con un vincolo reale a database.

> **Nota sulla decisione 3 (geometria del bonus testate)**: il bonus delle due testate ha senso
> solo unendo tavoli identici da 2 posti in fila — è la lettura letterale della decisione ("l'unità
> base è il tavolo da 2"). Un tavolo che dichiara già una capienza propria più grande (es. 6)
> include già le sue testate: unirlo ad altri tavoli fa perdere, non guadagnare, posti alle
> giunzioni interne. Estendere il bonus a quel caso significherebbe promettere online posti che in
> sala non esistono. Se in futuro serve precisione piena su unioni miste, si introduce un campo
> `PostiCapotavola` sulla singola postazione — backlog v2.0, non ora.

---

## Procedura per le migration in produzione (aggiunta 31/08/2026)

Le Fasi 2a e 3 applicano modifiche di schema su un database di produzione con dati reali, a mano.
Due di queste modifiche sono **breaking**: il rename `MaxPrenotazioni`→`MaxCoperti` e il nuovo
indice unique per la Fase 3. Railway fa deploy rolling: nell'intervallo fra "migration applicata"
e "nuova versione online", la versione vecchia dell'app interroga uno schema che non corrisponde
più e restituisce 500 su tutto ciò che tocca quella tabella.

Per il volume di traffico di Gestora oggi, una finestra di manutenzione dichiarata è la soluzione
giusta (un pattern più complesso come expand/contract — doppia scrittura durante la transizione —
sarebbe sovradimensionato). Procedura, sempre uguale per ogni migration breaking di questa
roadmap:

1. **`pg_dump` di backup** — non negoziabile, anche per una migration "banale"
2. Applicare la migration
3. Deploy della nuova versione dell'app
4. Verifica (`/health`, una chiamata autenticata reale sull'area toccata)
5. Comunicazione implicita di fine finestra (nessun downtime annunciato serve per un progetto
   personale, ma il passaggio 1→4 va fatto in sequenza stretta, non a step separati nel tempo)

---

## Come leggere questa roadmap

Le fasi vanno in ordine: ognuna poggia su quella prima. Ogni fase si chiude secondo la Definition
of Done sopra: io aggiorno tracker e documenti di progetto, tu fai commit e push.

I tuoi task sono pochi e concentrati: li trovi tutti raccolti nel riepilogo finale.

---

## Fase 0 — Preparazione

*Partire dal punto giusto.* ✅ **Chiusa.**

🧑 **Fabio**
- Posizionarsi sul branch `dev` e verificare `git status`
- Allineare il database locale: `dotnet ef database update`
- Committare `JobsController.cs`, che oggi esiste solo sul tuo PC (REV-097 — fatto, `3d614b1`)
- **Controllare che valore hai messo in produzione nel campo capienza delle fasce orarie.** Se
  avevi scritto "10" pensando a 10 prenotazioni, oggi il locale accetta 10 persone in tutto:
  vanno corretti prima di andare avanti

🤖 **Claude**
- Nessun task

**Chiusura**: branch pulito, database locale allineato, capienze di produzione verificate.

---

## Fase 1 — Fondamenta di deploy + hardening rapido

*Rendere il rilascio riproducibile e non più affidato al pannello di Railway. Assorbe anche 3 fix
di sicurezza a costo quasi zero (REV-008, REV-009, REV-013) che non hanno dipendenza dal resto
della Fase 4 — non ha senso lasciarli aspettare lì solo perché condividono un titolo con la
schermata di primo avvio, che invece richiede lavoro frontend.*

🤖 **Claude**
- Portare nel repository la configurazione di build e deploy, oggi presente solo nel pannello
  Railway: se lo perdi, non è ricostruibile (REV-011)
- Far controllare al health check anche la raggiungibilità del database, così un rilascio con
  database disallineato risulta fallito invece che "sano" (REV-012)
- Aggiungere all'avvio un avviso esplicito quando il database non è allineato al codice: visto che
  le migration restano manuali, questo è ciò che ti evita di scoprirlo dal primo errore
- Correggere la migration `StatoAsEnum`, oggi vuota e senza effetto (REV-035)
- Allineare le versioni dei pacchetti disallineate e rimuovere quelli inutilizzati (REV-036)
- Smettere di restituire al client `exception.Message`: oggi le 500 fanno leak di messaggi interni
  Npgsql (REV-009)
- Bloccare l'account dopo N tentativi di accesso falliti e limitare le chiamate ripetute alla
  pagina di login (REV-008)
- Applicare la stessa policy password anche al reset fatto dall'Admin, oggi aggirabile (REV-013)

🧑 **Fabio**
- Su Railway: togliere il comando di build personalizzato, ora che la configurazione sta nel
  repository
- Verificare che il rilascio vada a buon fine e che `/health` risponda

**Chiusura**: un push su `main` produce un rilascio corretto senza configurazione nascosta, e i 3
fix di sicurezza a costo zero sono in produzione.

---

## Fase 2 — Logica di prenotazione e assegnazione tavoli

*La fase più importante. Non è solo una correzione: cambia il modo in cui il sistema assegna i
tavoli. Per ogni intervento scrivo prima il test che dimostra il problema, poi la soluzione — è
lo stesso principio di una "rete di sicurezza" prima di riscrivere, applicato dentro ogni
checkpoint invece che come fase a sé.*

> **Nota di processo**: la fase è grossa — nuovo algoritmo di assegnazione + rename di campo con
> migration su dati reali + una decina di fix correlati ma distinti. Per restare bisectable
> (poter isolare quale cambiamento ha causato un'eventuale regressione), è divisa in **tre
> checkpoint sequenziali**, ognuno con commit separato e `dotnet test` verde prima di passare al
> successivo. Non sono sotto-fasi con propria chiusura verso Fabio: la chiusura della Fase 2
> resta unica, in fondo.

### Checkpoint 2a — Rename `MaxPrenotazioni` → `MaxCoperti`

*Solo rinomina, comportamento invariato. Isolato apposta: se qualcosa si rompe dopo, non deve
essere confuso con un problema del nuovo algoritmo.*

🤖 **Claude**
- Rinominare `MaxPrenotazioni` in `MaxCoperti` in tutto il progetto, database compreso (migration
  dedicata), e mettere un'etichetta chiara nel form (oggi il campo non ne ha nessuna) — REV-005

🧑 **Fabio**
- Applicare la migration in produzione seguendo la **procedura per le migration** descritta in
  testa al documento (backup, migration, deploy, verifica) — solo dopo aver verificato in Fase 0
  che i valori esistenti abbiano già il significato "coperti", non "prenotazioni"

**Chiusura checkpoint**: `dotnet test` verde, nessun comportamento cambiato, solo il nome.

### Checkpoint 2b — Nuovo algoritmo di assegnazione

*Il cuore della fase. Sviluppato e testato in isolamento prima di essere agganciato al resto.*

🤖 **Claude**
- Calcolare la capienza di un'unione di tavoli come somma delle capienze, aggiungendo il bonus di
  **2 posti solo se l'unione è composta esclusivamente da tavoli da 2 posti** — per ogni altra
  combinazione, somma semplice senza bonus (decisione 3 aggiornata)
- Scegliere sempre la combinazione con meno posti sprecati, valutando insieme tavolo singolo e
  unioni fino a un massimo di 4 tavoli (decisione 4) — oggi il tavolo singolo vince sempre, anche
  quando spreca, e la ricerca non ha alcun limite dichiarato
- Non occupare più un tavolo da 8 per 2 persone quando esistono alternative migliori
- Registrare quanti posti vengono realmente usati su ogni tavolo, oggi mai salvato (REV-001, il
  campo che lo abilita)
- Togliere il vincolo che ammette solo tavoli da 2, 4 e 8: capienza libera da 1 in su

🧑 **Fabio**
- Provare: prenotazione da 2 con solo tavoli grandi liberi, prenotazione da 8 che unisce 3 tavoli,
  prenotazione che unisce un tavolo da 2 e uno da 6 (verificare che non scatti il bonus testate)

**Chiusura checkpoint**: `dotnet test` verde, nuovo algoritmo coperto da test e agganciato al
posto di quello vecchio.

### Checkpoint 2c — Unificazione disponibilità/assegnazione + fix correlati

*Tutto ciò che dipende dal nuovo algoritmo essendo già in campo, e i fix di logica minori che
condividono lo stesso terreno.*

> **Stato — 01/09/2026: codice completo, 3 commit su `dev`** (`89f3693` unificazione + REV-002/
> 034/006/024, `d11a144` riepilogo sala, + orologio unico da committare). `dotnet test` 57/57,
> frontend `tsc`/`build`/`eslint` puliti. **Nessuna migration in tutto il 2c.** Mancano solo i
> test manuali di Fabio qui sotto per chiudere formalmente la Fase 2 (DoD punto 3).
>
> Note di implementazione:
> - `DisponibilitaService` chiama direttamente `AssegnazioneTavoli.TrovaMigliorCombinazione` (il
>   wrapper `TrovaCombinazioniDisponibili` è stato rimosso). `check-disponibilita` non ha
>   consumatori frontend, quindi `FasciaDisponibilitaDTO` è stato esteso (`MaxCoperti`,
>   `PostiResiduiFascia`, `Messaggio`) senza impatto.
> - REV-024 applicato anche a `PostazioneAssignmentService` (assegnazione reale), non solo alla
>   disponibilità, così i due percorsi restano allineati.
> - REV-034: il Cliente su prenotazione altrui riceve **401** (fa logout lato frontend). Il fix
>   401→403 è **REV-025, Fase 6** — non anticipato qui per non allargare lo scope.
> - Orologio: nuovo `Common/IClock` (`SystemClock` singleton). Nessuna colonna `timestamptz` da
>   convertire (le entità di prenotazione usano `DateOnly`/`TimeOnly`) → nessuna migration. I
>   ~30 `DateTime.Now` nelle stringhe di log dei controller restano — rumore di logging, non
>   orologio di dominio; candidati Fase 9.
> - Debito residuo: secondo costruttore fantasma di `PrenotazioniService` (REV-010, Fase 9).

🤖 **Claude**
- **Disponibilità sempre piena**: l'endpoint pubblico oggi risponde "tutto libero" in ogni caso;
  usa il dato dei posti occupati che finalmente viene salvato (checkpoint 2b) — REV-001
- **Staff bloccato**: permettere ad Admin e Staff di modificare la prenotazione di un cliente,
  come previsto dai ruoli — REV-002
- Permettere al Cliente di leggere il dettaglio della propria prenotazione, oggi riservato ad
  Admin/Staff — REV-034
- Far usare alla verifica di disponibilità la stessa logica dell'assegnazione: oggi sono due
  algoritmi diversi che possono dare risposte opposte
- Basare i posti residui sul tetto della fascia e non sulla somma dei tavoli: è il tetto a
  decidere quando la fascia è esaurita (decisione 8)
- Escludere zone e tavoli disattivati dalla verifica di disponibilità — REV-024
- Dare un messaggio chiaro quando il tetto non è esaurito ma i tavoli liberi non bastano: oggi
  l'utente legge "non ci sono postazioni libere" mentre l'app dice che c'è ancora disponibilità
- Aggiungere in cima alla pagina Postazioni il riepilogo della sala: tavoli attivi, posti totali e
  copertura del tetto per ogni fascia (decisione 9)
- Registrare nel log attività anche la modifica di una prenotazione, oggi l'unica azione non
  tracciata — REV-006
- Un solo orologio per tutto il progetto: niente più mix di orario locale, UTC e ora italiana —
  tutto in UTC nel database, conversione a Europe/Rome solo al confine (validator, dashboard,
  frontend) — REV-016, REV-092

🧑 **Fabio** — test manuali per la chiusura della Fase 2 (5)

1. **Disponibilità, tetto esaurito** — `POST /api/Prenotazione/check-disponibilita` per una data
   e una fascia che ha già `MaxCoperti` coperti prenotati: la fascia deve tornare
   `disponibilePerRichiesta=false` con `messaggio` che parla di **capienza massima**.
2. **Disponibilità, tetto libero ma tavoli pieni** — stessa chiamata su una fascia col tetto
   ancora lontano ma senza combinazione di tavoli liberi per i coperti richiesti (es. locale con
   pochi tavoli grandi già occupati): `disponibilePerRichiesta=false` con `messaggio` che parla
   di **tavoli**, non di capienza.
3. **Staff modifica prenotazione cliente (REV-002)** — **da testare via Swagger/Postman**: il
   frontend non ha (ancora) un pulsante "Modifica" per le prenotazioni, solo Conferma/Completa/
   Annulla. Da account **Staff**: `PUT /api/Prenotazione/update-prenotazione?id={id}` con un body
   `PrenotazioneCreateDTO` valido, su una prenotazione il cui `UserId` è di un altro utente →
   prima dava **401**, ora deve dare **200**. (Gap noto: `update-prenotazione` è oggi una
   capacità solo-backend — nessuna UI la usa. Decidere in Fase 6 se aggiungere un modal di
   modifica o accettare "annulla + ricrea" per la v1.)
4. **Cliente legge il proprio dettaglio (REV-034)** — da account **Cliente**, `GET
   get-prenotazione?id=` sulla propria prenotazione → 200; su una prenotazione altrui → 401.
5. **Riepilogo sala + orologio** — pagina Postazioni (Admin/Staff): la card "Riepilogo sala" in
   cima mostra tavoli attivi, posti totali e copertura del tetto per fascia. E: aprire la
   Dashboard **tra mezzanotte e le 2 di notte** (o falsificando l'orario) → deve mostrare il
   giorno corretto in ora italiana, non quello precedente (REV-016).

**Chiusura Fase 2**: ✅ **CHIUSA 01/09/2026.** Codice completo (checkpoint 2a+2b il 31/08,
checkpoint 2c il 01/09, 4 commit su `dev`), `dotnet test` 57/57, nessuna migration nel 2c. I 5
test manuali di Fabio **tutti passati** (2 e 3 il 01/09 via UI + Postman, 1/4/5 il 01/09). Restano
da committare `ROADMAP_REVISIONE.md` e `TrackAttività_Gestora.xlsx` — vanno con il primo commit
della Fase 3.

---

## Fase 3 — Prenotazioni simultanee

*Impedire che due clienti prenotino lo stesso tavolo nello stesso momento. Un concurrency token
sulla riga della prenotazione non basta: due `INSERT` distinti sullo stesso tavolo sono righe
diverse, nessun conflitto da rilevare a quel livello. La garanzia reale va messa a database.*

> **Decisioni prese il 01/09/2026 (analisi Sonnet, implementazione Opus):**
>
> - **Forma dell'indice: A2 — unique index PIENO, non parziale.** Lo slot è `(PostazioneId,
>   DataPrenotazione, FasciaOrariaId)` ma `Data` e `FasciaOrariaId` vivono su `Prenotazione` →
>   si **denormalizzano `DataPrenotazione` + `FasciaOrariaId` sulla join `PrenotazioniPostazioni`**
>   (2 colonne nuove, niente flag `Annullata`). L'indice è un semplice
>   `CREATE UNIQUE INDEX "UX_PrenotazionePostazione_Slot" ON "PrenotazioniPostazioni"
>   ("PostazioneId","DataPrenotazione","FasciaOrariaId")` — **nessun `WHERE`**.
> - **`AnnullaPrenotazioneAsync` elimina le righe `PrenotazioniPostazioni`** della prenotazione
>   (oggi le lascia): una prenotazione annullata libera il tavolo, che è anche più corretto.
>   Verificato che Dashboard e disponibilità filtrano già `Stato != Annullata`, nessuno dipende
>   da quelle righe. Effetto collaterale: il dettaglio di una prenotazione annullata mostra lista
>   tavoli vuota — accettabile. (Deviazione consapevole dal testo originale "indice parziale":
>   eliminando le righe su annulla il filtro diventa inutile. Non è una delle 10 decisioni
>   vincolanti.)
> - **Errore `23505` → nuova `ConflictException`** in `Infrastructure/Exceptions/`, mappata a
>   **409** nel middleware. Si stringe contestualmente la regola `InvalidOperationException → 409`
>   (anticipa **REV-026**): d'ora in poi il 409 è solo `ConflictException`.
> - **Transazione dentro `context.Database.CreateExecutionStrategy().ExecuteAsync(...)`** perché
>   `EnableRetryOnFailure` è attivo (vedi `GestoraWebApi/CLAUDE.md`).
> - **Test di concorrenza**: il provider InMemory **non applica** gli unique index → il test
>   "due prenotazioni simultanee" ha bisogno di Postgres vero. Opzioni da valutare all'inizio:
>   Testcontainers (nuova dipendenza di test) **oppure** spezzare in (a) unit test sulla
>   traduzione `23505`→`ConflictException` con `DbUpdateException`/`PostgresException` costruita a
>   mano + (b) un test d'integrazione separato a parte. Decidere prima di scrivere.

🤖 **Claude (Opus)**
- Racchiudere creazione e modifica della prenotazione in un'unica operazione atomica — REV-003
- Denormalizzare `DataPrenotazione` + `FasciaOrariaId` su `PrenotazioniPostazioni` e aggiungere
  l'**unique index pieno** `UX_PrenotazionePostazione_Slot` (migration): è il database stesso a
  rifiutare la seconda riga, non dipende dal livello di isolamento della transazione
- Far sì che `AnnullaPrenotazioneAsync` elimini le righe join della prenotazione
- Tradurre il codice errore Postgres `23505` in `ConflictException` → `409` leggibile, con un
  retry lato client dove ha senso; stringere `InvalidOperationException → 409` nel middleware
- Scrivere il/i test di concorrenza (vedi nota sulle opzioni sopra)

> **Nota**: "una prenotazione al giorno per Cliente" (REV-004) **non** riceve un vincolo a
> database in questa fase — per decisione 10, resta un controllo applicativo, rischio residuo
> accettato. Questa fase chiude solo la race condition sul doppio tavolo (REV-003).

🧑 **Fabio**
- Applicare la migration in produzione seguendo la **procedura per le migration** in testa al
  documento
- Provare a prenotare lo stesso tavolo da due browser diversi contemporaneamente

**Chiusura**: la doppia prenotazione sullo stesso tavolo non è più possibile, né in locale né in
produzione. ✅ **FASE 3 CHIUSA il 03/09/2026**: codice e verifiche completi il 02/09, chiusura
formale il 03/09 con commit e push (`a07fda0` su `dev`, `b09e583` su `main` — DoD punto 5).
La pulizia dei dati di test in produzione è stata **volutamente rimandata**: la produzione resta
l'ambiente di test fino a fine progetto, si farà una pulizia generale del database prima della
consegna (tracciata come **NEW-005** nel foglio "Fix e Bug").

> **Esito dei task 🧑 (Definition of Done, punto 3) — 02/09/2026**
>
> - **Locale**: migration applicata con `dotnet ef database update`, indice verificato con
>   `\d "PrenotazioniPostazioni"`. Le tre prove manuali passate: creazione, annullo che libera
>   il tavolo (righe join a 0), modifica sullo stesso slot senza falso conflitto. Conferma sui
>   dati: l'unica prenotazione presente era annullata e la migration ne ha cancellato le righe.
> - **Produzione**: backup mirato con `\copy` di `Prenotazioni` (20 righe) e
>   `PrenotazioniPostazioni` (33 righe) — il `pg_dump` completo non era possibile, il servizio
>   Postgres su Railway non ha TCP proxy pubblico e l'host interno non è raggiungibile da fuori.
>   Pre-check duplicati: 0. Righe di prenotazioni annullate da cancellare: 14 → 33 − 14 = **19
>   righe finali, come previsto**, nessuna con slot nullo. Indice creato, migration registrata in
>   `__EFMigrationsHistory`. Poi merge `dev`→`main` e deploy Railway.
> - **Test di concorrenza superato**: due `POST /crea-prenotazione` realmente simultanee (script
>   `test-concorrenza.ps1`, due `SendAsync` su `HttpClient` senza attesa reciproca) su una zona di
>   test con un solo tavolo da 2 → **`201` + `409` "Il tavolo è stato appena assegnato a
>   un'altra prenotazione"**. Il controllo applicativo aveva visto il tavolo libero per entrambe:
>   a fermare la seconda è stato l'unique index. È la dimostrazione diretta di REV-003.
>
> **Incidente da ricordare**: lo script SQL generato da `dotnet ef migrations script` ha il BOM
> UTF-8 in testa, e psql lo attacca alla prima istruzione → `START TRANSACTION` fallisce con
> `syntax error at or near "START"` e **il resto dello script gira in autocommit**, senza
> rollback automatico. È successo in produzione: il risultato è stato corretto, ma senza rete di
> sicurezza. Il file in `Scripts/` è stato ripulito dal BOM e porta la nota in testa.
>
> **Chiusura formale — fatto il 03/09/2026**
> 1. **Commit e push** eseguiti (DoD punto 5): `a07fda0` su `dev`, `b09e583` su `main`.
> 2. **Sicurezza del repository**: i tre backup con i dati delle prenotazioni erano finiti nel
>    commit `15f98a0` di un repository **pubblico**. Storia riscritta con `git-filter-repo`
>    (`--invert-paths`, 80 commit riparsati) e force push su `dev` e `main`; regole `.gitignore`
>    aggiunte perché non si ripeta. Dati confermati inventati → nessuna richiesta di purga a
>    GitHub Support. Da sapere: il vecchio commit resta raggiungibile **per SHA diretto** finché
>    GitHub non fa garbage collection — con dati sensibili il force push da solo non basta.
> 3. **`JwtSettings__Secret` ruotato** su Railway (segreto da 64 byte generato in clipboard, mai
>    a video): il token Admin esposto in chat il 02/09 è invalidato. `/health` → `Healthy`,
>    login verificato.
> 4. **Pulizia dei dati di test in produzione: rimandata per scelta** (NEW-005). Zona "Test
>    concorrenza", tavolo da 2 e prenotazione del 09/09 restano in produzione, che continua a
>    fare anche da ambiente di test. Quando si farà, l'ordine è obbligato per via dei vincoli:
>    prima le prenotazioni (Admin, `DELETE /delete-prenotazione`, ammesso solo su stato Attiva o
>    Annullata), poi le postazioni, poi le zone — finché una postazione ha righe in
>    `PrenotazioniPostazioni` non è modificabile né eliminabile (REV-099).
>
> **Emersi durante i test, registrati come REV-098 e REV-099** (vedi `REVISIONE_END_TO_END.md`):
> la modifica prenotazione non esiste nel frontend, e un tavolo con prenotazioni storiche non è
> più modificabile né disattivabile.

> **Stato al 02/09/2026 — codice completo, `dotnet test` 70/70 e `npm run build` verdi.**
> Restano i task 🧑 (migration in produzione + prova da due browser): senza il loro esito scritto
> la fase è formalmente aperta (Definition of Done, punto 3).
>
> Cosa è stato fatto:
> - `PrenotazionePostazione` porta le due colonne denormalizzate `DataPrenotazione` +
>   `FasciaOrariaId` e l'unique index pieno `UX_PrenotazionePostazione_Slot`. Migration
>   `20260902081401_AggiungiSlotPrenotazionePostazione`, **scritta a mano**: lo scaffolding di EF
>   riempiva le colonne con `0001-01-01`/`0` e poi falliva la creazione dell'indice. Ordine
>   vincolante: colonne nullable → backfill dallo slot della prenotazione → cancellazione delle
>   righe delle annullate → `NOT NULL` → indice.
> - `AddAsync`, `UpdateAsync` e `AnnullaPrenotazioneAsync` girano dentro
>   `CreateExecutionStrategy().ExecuteAsync(...)` + transazione esplicita. In `UpdateAsync` i
>   DELETE delle vecchie righe sono salvati **prima** degli INSERT: EF non garantisce
>   quest'ordine dentro una singola `SaveChanges` e l'indice rifiuterebbe una modifica che riusa
>   lo stesso tavolo.
> - `AnnullaPrenotazioneAsync` elimina le righe join → l'annullata libera il tavolo.
> - `ConflictException` + `DbExceptionTranslator` (`Infrastructure/Exceptions/`): un `23505` su
>   quel preciso constraint diventa 409 leggibile; un `23505` di un altro indice resta 500.
> - **REV-026 chiuso qui** (anticipato dalla Fase 7): 37 `InvalidOperationException` di dominio
>   convertite in 5 service — 34 in `ConflictException` (409 invariato) e 3 in `NotFoundException`
>   (`"Fascia oraria con id X non esiste"`, `"La zona con ID X non esiste"`, `"Zona non trovata"`:
>   erano 409, ora 404 — l'unico cambio di contratto della fase). Mappatura
>   `InvalidOperationException → 409` rimossa dal middleware.
> - **REV-032 parziale**: l'audit log di creazione/modifica/annullo è ora dentro la stessa
>   transazione della scrittura. Le altre scritture che loggano (Zone, Postazioni, Fasce) restano
>   alla Fase 7.
> - Test: 6 su `DbExceptionTranslator` + 7 su `PrenotazioniService` (slot valorizzato in
>   creazione e modifica, 23505 sullo slot → 409, 23505 di altro vincolo non tradotto, righe join
>   cancellate sull'annullo, fallimento dell'audit log che fa fallire l'operazione). Il test
>   "due prenotazioni simultanee" vero **non** esiste: l'InMemory non applica gli unique index e
>   si è deciso di non introdurre Testcontainers — la prova end-to-end è il test manuale di Fabio.
> - Per la produzione: `GestoraWebApi/Scripts/20260902_AggiungiSlotPrenotazionePostazione.sql`,
>   generato con `dotnet ef migrations script --idempotent`. E' tutto in una transazione e
>   aggiorna anche `__EFMigrationsHistory`: se il passo dell'indice fallisce per dati duplicati,
>   il database torna com'era e EF non considera la migration applicata.
> - Frontend: nessuna modifica necessaria. L'interceptor Axios non intercetta il 409 e
>   `usePrenotazioni.ts` mostra già `data.message` in un toast, quindi il messaggio di conflitto
>   arriva all'utente così com'è.

---

## Rilascio intermedio — v1.0.1 (proposta, facoltativa)

*A fine Fase 4 il grosso del valore di questa roadmap è già in produzione: fondamenta di deploy,
logica di dominio corretta, concorrenza chiusa, sicurezza. Aspettare la Fase 11 per il primo
merge su `main` sarebbe un cambio di abitudine rispetto a come il progetto ha sempre rilasciato
finora (incrementale) — e un merge unico dopo sette fasi in più è più difficile da verificare.*

Decisione da prendere **a quel punto**, non ora: se le Fasi 1-4 sono verdi e verificate, valuta
un merge `dev`→`main` con tag `v1.0.1`, prima di proseguire con test/pulizia/UX (Fasi 5-11). Non è
un obbligo — se preferisci un unico rilascio finale, si salta e si prosegue.

---

## Fase 4 — Sicurezza e primo avvio

*Chiudere i punti che oggi lasciano il prodotto esposto (i 3 fix a costo zero sono già stati
spostati in Fase 1, non richiedono di aspettare la schermata di primo avvio).*

🤖 **Claude**
- **Schermata di primo avvio**: se non esiste ancora nessun amministratore, l'app mostra una
  pagina dedicata per crearlo; appena esiste, la pagina sparisce da sola. L'endpoint pubblico
  attuale viene rimosso — REV-007
- Togliere l'email dai log di accesso — REV-070
- Distinguere "non autenticato" da "non autorizzato": oggi un permesso negato provoca il logout —
  REV-025
- Impedire al cliente di scrivere il campo riservato a Staff e Admin — REV-033

🧑 **Fabio**
- Niente

> Nota: la schermata di primo avvio resta raggiungibile solo finché non esiste un Admin. In
> produzione l'Admin c'è già, quindi è di fatto chiusa da subito.

**Chiusura**: nessun endpoint sensibile raggiungibile senza autenticazione.

---

## Fase 5 — Test del backend

*Coprire il percorso che il prodotto esiste per fare. Oggi i test verdi non toccano la
prenotazione.*

🤖 **Claude**
- Test su creazione e modifica di una prenotazione: capienza, giorno corretto, data passata,
  limite giornaliero, preavviso minimo del cliente — REV-051
- Test sull'assegnazione reale del tavolo, nuova logica compresa (oggi è coperto solo un metodo
  gemello, non quello usato) — REV-052
- Test sulla verifica di disponibilità e sulla dashboard — REV-053
- Test sui ruoli: chi può fare cosa — REV-053
- Test sui due processi automatici notturni (verificati manualmente in produzione il 27-28/08,
  qui restano da coprire con test automatici) — REV-053, REV-054

🧑 **Fabio**
- Niente

**Chiusura**: il flusso di prenotazione è coperto end-to-end e tutti i test passano.

---

## Fase 6 — Bug del frontend

*I problemi che l'utente incontra davvero.*

🤖 **Claude**
- **Schermata bianca**: oggi un dato di sessione corrotto blocca l'app in modo irrecuperabile,
  nemmeno il login è raggiungibile — REV-014
- **Scelta della zona**: il campo non è collegato al modulo, si prenota "nessuna preferenza"
  credendo di aver scelto una zona — REV-015
- **Modal di modifica prenotazione** (NEW-001, aperto 01/09/2026): oggi non esiste alcun pulsante
  "Modifica" per le prenotazioni, solo Conferma/Completa/Annulla — `update-prenotazione` è una
  capacità solo-backend e il fix REV-002 (Admin/Staff modificano la prenotazione di un cliente)
  non è raggiungibile dall'app. Riusare il `PrenotazioneModal` esistente: prop `prenotazione` +
  precompilazione + mutation `PUT` + pulsante nella colonna Azioni per stato `Attiva`. Va fatto
  **insieme a REV-015** perché tocca lo stesso componente.
- **Dashboard**: tra mezzanotte e le due mostra i dati del giorno prima — REV-016. **Già risolto
  lato backend nel checkpoint 2c** (`IClock` / `TodayInRome`): qui resta solo da verificare che
  la pagina Dashboard non faccia a sua volta conti sulle date lato client.
- **Indirizzo del server**: se la variabile non è impostata l'app fallisce in silenzio; aggiungo
  un errore esplicito — REV-017
- Far scadere la sessione in modo pulito invece che con un errore improvviso — REV-025

🧑 **Fabio**
- Verificare su Vercel che la variabile con l'indirizzo del backend sia impostata

**Chiusura**: i quattro casi sono verificati sull'ambiente di produzione.

---

## Fase 7 — Robustezza del backend

*Correggere i problemi che non bloccano ma degradano il servizio.*

🤖 **Claude**
- Svuotare correttamente la cache quando una fascia oraria viene eliminata — REV-018
- Validare il numero di pagina, oggi un valore sbagliato manda l'API in errore — REV-019
- Rendere stabile l'ordinamento delle liste paginate, oggi righe possono sparire o ripetersi —
  REV-020
- Alleggerire le query pesanti: elenco utenti, processi notturni, assegnazione del tavolo —
  REV-021, REV-022, REV-023
- Registrare l'indirizzo IP reale nel log attività, oggi registra quello del server intermedio —
  REV-029
- Aggiungere i controlli mancanti sui dati in ingresso, in particolare sull'endpoint pubblico —
  REV-027
- Restituire una lista vuota invece di un errore quando non ci sono risultati — REV-031
- Non cancellare lo storico delle prenotazioni quando si elimina un utente — REV-038
- Contare i tavoli occupati per fascia e non per giornata intera, nella dashboard — REV-039
- Rendere il log attività consultabile: indici sulla tabella e endpoint di lettura per l'Admin —
  REV-037
- ~~Restringere la mappatura `InvalidOperationException → 409`, oggi cattura anche errori interni
  di EF Core~~ — **REV-026 chiuso in Fase 3** (02/09/2026), anticipato lì per non lasciare due
  strade diverse verso il 409 dopo l'introduzione di `ConflictException`
- Includere l'audit log nella stessa transazione della scrittura che registra — REV-032
  (**parziale**: chiuso in Fase 3 per creazione/modifica/annullo di prenotazione, dove la
  transazione serviva comunque; restano da coprire le scritture di Zone, Postazioni e Fasce)
- Documentare (non serve un fix: Railway gira una sola replica oggi) che Quartz non è in cluster
  mode — se in futuro arrivano più repliche, ogni job girerebbe due volte — REV-028

🧑 **Fabio**
- Niente

**Chiusura**: nessuna segnalazione backend di priorità media aperta.

---

## Fase 8 — Robustezza del frontend

🤖 **Claude**
- Svuotare la cache al logout: oggi cambiando account si vedono i dati del precedente — REV-040
- Eliminare la gestione errori copiata 21 volte, sostituendola con una funzione unica — REV-041
- Togliere le notifiche doppie sulla pagina Postazioni — REV-042
- Aggiungere la paginazione: oltre 100 prenotazioni i dati oggi spariscono senza avviso — REV-043
- Disabilitare i pulsanti durante il salvataggio, per evitare doppie chiamate — REV-044
- Rendere riutilizzabile la finestra di conferma, oggi dice "Elimina" anche quando annulla —
  REV-045
- Attivare i controlli stretti di TypeScript — REV-046
- Togliere gli strumenti di sviluppo dal pacchetto di produzione — REV-076
- Aggiungere il resolver zod a `EditUserModal`/`ResetPasswordModal`, oggi senza — REV-048
- Impostare `defaultOptions` su `QueryClient` (staleTime, niente retry sui 403) — REV-049
- Centralizzare la decodifica del JWT, oggi duplicata in 3 punti — REV-050
- Introdurre i primi test frontend su accesso, ruoli e scelta della fascia oraria — REV-047

🧑 **Fabio**
- Niente

**Chiusura**: build e controlli dei tipi puliti, primi test frontend verdi.

---

## Fase 9 — Pulizia

*Togliere quello che confonde chi legge il codice, incluso un recruiter. Copre REV-055…REV-069 e
REV-082 (naming, cartelle morte, duplicazioni, refusi, `NotImplementedException` minori,
costanti ripetute, endpoint scritti inline) — dettaglio completo di ognuno in
`REVISIONE_END_TO_END.md` §3 Priorità BASSA, qui raggruppati per fase invece che citati uno a uno.*

🤖 **Claude**
- Eliminare cartelle vuote, codice morto e file mai usati (backend e frontend) — REV-055, REV-077
- Rimuovere il costruttore inutilizzato che, se selezionato, farebbe partire il servizio senza
  dipendenze — REV-010
- Uniformare il nome di "fascia oraria", oggi scritto in quattro modi diversi — REV-056
- Accorpare le duplicazioni: algoritmo copiato due volte, funzioni ripetute in quattro servizi,
  costanti ripetute — REV-058, REV-059, REV-060, REV-061
- Rimuovere le implementazioni che lanciano errore solo per soddisfare un'interfaccia — REV-030
- Correggere i nomi di file troncati e i refusi — REV-067, REV-068
- Centralizzare i path degli endpoint, oggi scritti inline in ogni hook — REV-082
- Applicare la formattazione automatica a tutto il progetto — REV-078

🧑 **Fabio**
- Niente

**Chiusura**: nessun file morto, nessuna duplicazione evidente, formattazione uniforme.

---

## Fase 10 — Esperienza d'uso

*Rendere il prodotto presentabile, anche da telefono.*

🤖 **Claude**
- Rendere l'app utilizzabile su smartphone: menu laterale richiudibile, tabelle scorrevoli,
  layout adattivo (oggi da telefono è inutilizzabile) — REV-071
- Aggiungere i messaggi per le liste vuote, oggi si vede una tabella vuota che sembra un errore —
  REV-073
- Sostituire il "Caricamento…" a schermo intero con un caricamento parziale — REV-074
- Uniformare i pulsanti e i colori a un unico stile — REV-075
- Sistemare l'accessibilità: etichette collegate ai campi, chiusura delle finestre con ESC —
  REV-072
- Mostrare nome e ruolo dell'utente e la pagina attiva nel menu — REV-081
- Aggiungere un link di ritorno nella pagina `/unauthorized`, oggi un vicolo cieco — REV-080
- Sistemare titolo, lingua e icona del sito — REV-079
- **Pannello disponibilità nel form di creazione prenotazione** (NEW-002, aperto 01/09/2026,
  **opzionale**): scelti data + coperti, mostrare le fasce con semaforo verde/rosso, posti
  residui e — sfruttando `messaggio` di `check-disponibilita` — il motivo quando non c'è posto,
  invece di lasciare che l'utente scopra il rifiuto solo all'invio. **Non** una pagina pubblica
  per il cliente: quella è esclusa dalla v1 (decisione 10). Da confermare se vale lo sforzo.

🧑 **Fabio**
- Un giro di prova da telefono sui tre ruoli

**Chiusura**: l'app è usabile e coerente su desktop e mobile.

---

## Fase 11 — Chiusura

🤖 **Claude**
- Aggiornare `BACKEND_FIX_TODO.md` con tutto quello che è stato chiuso — REV-083
- Aggiornare il tracker Excel su tutti i fogli e il blocco di stato di `CLAUDE.md` — REV-095
- Aggiornare il piano di test manuale, fermo a marzo e non più allineato al prodotto — REV-085
- Documentare la nuova regola di assegnazione dei tavoli, che oggi non è scritta da nessuna parte
- Scrivere quali requisiti iniziali sono stati implementati e quali esclusi (matrice
  requisito→implementazione) — REV-086
- Correggere il percorso sbagliato del progetto nei documenti — REV-094
- Aggiungere lo schema del database e l'elenco delle modifiche applicate — REV-091
- Rigenerare il grafo del progetto, fermo al 14/08 — REV-096
- Allineare il conteggio dei test su tutti i documenti — REV-088
- Correggere la descrizione architetturale ("Clean Architecture + DDD" dichiarato, ma la struttura
  reale è layered in un unico progetto) — REV-089
- Correggere lo stato dichiarato nel tracker per funzionalità non esistenti, es. "Reportistica +
  export CSV/PDF" marcato Completato quando esistono solo i 2 endpoint Dashboard — REV-084

🧑 **Fabio**
- Verifica finale in produzione sui tre ruoli
- Merge su `main` e tag `v1.1.0`

**Chiusura**: nessuna segnalazione aperta, documentazione allineata al prodotto reale.

> **Nota REV-093** (stato del progetto distribuito su 5 fonti — tracker, `CLAUDE.md` root,
> `CLAUDE.md` di progetto, `PIANO_RILASCIO.md`, `BACKEND_FIX_TODO.md`): non affrontato in questa
> roadmap. È un problema strutturale di processo, più grande di un fix di fase — consolidare le
> fonti è una decisione a sé, da valutare separatamente se vale lo sforzo. Segnalato qui invece
> che lasciato silenziosamente fuori.

---

## Riepilogo — cosa devi fare tu

1. **Fase 0** — allineare il database locale, committare `JobsController.cs` e **verificare le
   capienze delle fasce in produzione**
2. **Fase 1** — togliere il comando di build personalizzato su Railway
3. **Fase 2a** — applicare la migration in produzione (rename capienza), seguendo la procedura di
   migration
4. **Fase 2b/2c** — tre giri di prova (tavolo grande per 2, unione mista, disponibilità/Staff/
   Cliente)
5. **Fase 3** — applicare la migration in produzione (indice unique), seguendo la procedura di
   migration; provare la doppia prenotazione da due browser
6. **v1.0.1 (facoltativo)** — decidere se fare un rilascio intermedio a fine Fase 4
7. **Fase 6** — verificare una variabile su Vercel
8. **Fase 10** — un giro di prova da telefono
9. **A ogni fase** — commit e push
10. **Fase 11** — verifica finale, merge e tag `v1.1.0`

Tutto il resto è a mio carico, aggiornamento di tracker e documenti compreso.

---

## Backlog v2.0 — fuori da questa roadmap

- **Vincolo "una prenotazione al giorno" a livello database per il Cliente self-service**
  (decisione 10): richiede una colonna che distingua le prenotazioni create dal Cliente da quelle
  create da Staff/Admin (es. `CanaleCreazione`). Da riprendere insieme alla progettazione della
  futura app/API dedicata al Cliente, non isolatamente.
- Creazione automatica delle postazioni in base ai coperti richiesti
- Turnover del tavolo (durata seduta, due turni nella stessa fascia)
- No-show come stato reale, con storico e policy per cliente
- Lista d'attesa quando la fascia è piena
- Email di conferma e promemoria
- Zona come preferenza con ripiego, invece che vincolo
- Overbooking controllato per fascia
- Chiusure straordinarie e orari speciali
- Export CSV/PDF dei report
- Unione e separazione tavoli come azione manuale dello Staff
- Recupero password autonomo
- Sessione con rinnovo automatico
- Gestione di più locali sullo stesso impianto
- `PostiCapotavola` come campo per postazione, se in futuro serve precisione piena sul bonus
  testate anche per unioni miste (vedi nota sulla decisione 3)
