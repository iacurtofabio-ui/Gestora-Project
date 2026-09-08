# Gestora — storico delle fasi

Documento **di sola lettura**. Racconta come il progetto è arrivato dove è oggi: cosa è stato
fatto in ogni fase, quali difetti sono emersi e — la parte più utile — **cosa abbiamo imparato
sbagliando**.

Per lo stato attuale vedi `CLAUDE.md` nella cartella principale. Per ciò che resta da fare vedi
`BACKLOG.md`. Per le procedure operative vedi `RUNBOOK.md`.

Le sigle `REV-xxx` rimandano alle segnalazioni della revisione end-to-end
(`REVISIONE_END_TO_END.md`, in questa stessa cartella), le sigle `NEW-xxx` al foglio *Fix e Bug*
del tracker.

---

## Riepilogo veloce

| Periodo | Fase | Cosa ha portato | Tag |
|---|---|---|---|
| giu–ago 2026 | Sviluppo v1 | Backend, frontend, deploy su Railway e Vercel | `v1.0.0` |
| 31/08 | 1 — Fondamenta di deploy | Dockerfile, health check sul database, 3 fix di sicurezza | — |
| 31/08 | 2a — Rename capienza | `MaxPrenotazioni` → `MaxCoperti` ovunque | — |
| 31/08 | 2b — Algoritmo tavoli | Nuovo motore di assegnazione e unione tavoli | — |
| 01/09 | 2c — Disponibilità | Un solo motore per disponibilità e assegnazione, un solo orologio | — |
| 02/09 | 3 — Prenotazioni simultanee | Indice unico sullo slot, transazioni, errore 409 leggibile | — |
| 03/09 | 4 — Sicurezza e primo avvio | Schermata di primo avvio, errore 403 corretto, log senza dati personali | `v1.0.1` |
| 03–04/09 | 5 — Test del backend | Da 31 a 74 test, coperto il percorso di prenotazione | — |
| 04/09 | 6 — Bug del frontend | Schermata bianca risolta, modifica ed eliminazione prenotazione | `v1.0.2` |
| 04–07/09 | 7 — Robustezza del backend | Paginazione corretta, prestazioni, audit, indirizzo IP reale | `v1.0.3` |
| 07/09 | 8 — Robustezza del frontend | Errori centralizzati, paginazione, primi 26 test frontend | `v1.0.4` |
| 07/09 | 9 — Pulizia | Codice morto, duplicazioni, refusi, 0 vulnerabilità nelle librerie | — |
| 07–08/09 | 10 — Esperienza d'uso | Responsive, accessibilità, stati vuoti, semaforo disponibilità | `v1.0.5` |
| 08/09 | 11 — Chiusura | Fix sovrapposizione fasce, 239 test | `v1.0.6` |

La Fase 9 non è taggata di proposito: era solo pulizia interna, nessun cambiamento visibile.

---

## Le regole di metodo — la parte da ricordare

Queste sono nate da errori veri, ognuno costato tempo. Valgono per qualsiasi progetto.

**1. Prima lo strumento che rende visibile il problema, poi il tentativo di correggerlo.**
Fase 7, indirizzo IP del client: due correzioni fatte "a intuito" e verificate a colpi di deploy
in produzione, tre giorni persi. L'endpoint di diagnostica andava scritto per primo. Quando un
comportamento non si riproduce in locale, la prima cosa da scrivere è quella che lo rende
osservabile.

**2. Per misurare una catena, il componente che la consuma va spento.**
Sempre Fase 7: il middleware che elabora l'header `X-Forwarded-For` **rimuove** l'anello che
elabora. Leggendo l'header dopo il suo passaggio sembrava esserci un proxy solo, e si concludeva
l'opposto del vero.

**3. Un test verde su codice rotto non è un test.**
Fase 8: un test verificava che cambiando giorno la fascia oraria si azzerasse. Rompendo apposta
l'azzeramento, il test restava verde: misurava un effetto collaterale della pagina (l'opzione
selezionata spariva da sola), non il comportamento vero. Da qui l'abitudine alla **controprova**:
rompere il codice di proposito e verificare che falliscano esattamente i test attesi.

**4. Quando cambia il metodo che una classe usa, controllare anche i test negativi.**
Fase 7: i test che verificavano "questo metodo non deve essere chiamato" hanno continuato a
passare **sempre**, perché controllavano un metodo che non veniva più chiamato da nessuno.

**5. I test vanno compilati, non solo eseguiti.**
`npm test` verde non sostituisce `npm run build`: il primo non controlla i tipi, il secondo sì.
In Fase 8 la build ha trovato un campo scritto male che i test non vedevano.

**6. Prima di correggere una riga di backlog scritta giorni prima, rileggere il codice.**
Fase 11: la riga del tracker descriveva metà del difetto — quella che si era notata. Il problema
vero era doppio.

**7. Prima la query, poi il fix.**
NEW-007: una segnalazione nata leggendo a occhio una tabella. Cinque query sui dati veri hanno
mostrato che il difetto non esisteva. Delle tre ipotesi iniziali nessuna era giusta: la risposta
era una quarta possibilità che nessuna contemplava.

**8. Prima di confermare un commit, contare i file inclusi.**
Fase 7: un commit ha preso solo i file già tracciati, i 12 file nuovi sono rimasti fuori e sono
stati persi. In Visual Studio i file nuovi vanno spuntati a mano, e "Annulla modifiche" su di
essi li **cancella**.

**9. Se un test contraddice il comportamento a runtime, sospettare la build prima del codice.**
Ripristinare un file da un backup gli ridà il timestamp vecchio: il compilatore non lo ricompila
e i test girano sul codice precedente. Si risolve con `dotnet build -t:Rebuild`.

---

## Fase 1 — Fondamenta di deploy (31/08/2026)

Configurazione di build e deploy portata dentro il repo (`railway.json`, `global.json`), health
check esteso al database, avviso nei log se ci sono modifiche al database non applicate, pacchetti
allineati. Tre fix di sicurezza anticipati dalla Fase 4: blocco account e limite ai tentativi di
login (REV-008), niente più dettagli tecnici dell'errore nelle risposte 500 (REV-009), regole
password applicate anche al reset fatto dall'Admin (REV-013).

> **Nixpacks abbandonato, sostituito da Dockerfile.** Due deploy falliti sulla stessa causa: lo
> strumento di build automatico di Railway non trovava .NET 9. Sostituito con un Dockerfile
> scritto a mano (immagini ufficiali Microsoft), che risolve anche il problema del deploy non
> versionato (REV-011).

## Fase 2 — Logica di prenotazione e assegnazione tavoli

### Checkpoint 2a — Rename della capienza (31/08)

`MaxPrenotazioni` → `MaxCoperti` in tutto il progetto: il campo indicava il numero massimo di
**coperti**, non di prenotazioni, e il nome sbagliato induceva in errore. Modifica al database
semplice e reversibile, applicata in produzione con backup della tabella.

### Checkpoint 2b — Nuovo algoritmo di assegnazione (31/08)

Logica estratta in un motore autonomo (`AssegnazioneTavoli`), senza dipendenze dal database:
prima era annegata dentro un metodo e testabile solo con impalcature pesanti.

Regole: capienza dell'unione = somma dei tavoli, **+2 posti di testata solo se l'unione è fatta
esclusivamente di tavoli da 2**; unioni fino a 4 tavoli della stessa zona; vince la combinazione
con meno posti sprecati.

> **Scelta di implementazione da ricordare**: provare tutte le unioni fino a 4 tavoli su 40
> tavoli significa circa 90.000 combinazioni. Il motore le genera invece sulle **capienze
> distinte** (3-5 valori in un locale vero), perché due tavoli della stessa capienza sono
> intercambiabili; i tavoli veri si scelgono solo alla fine. Il costo non cresce col numero di
> tavoli in sala.

Chiuso qui anche **REV-001**: il campo `NumeroPosti` esisteva nel database ma non veniva **mai**
scritto, restava a 0, e nessun test se ne era accorto.

### Checkpoint 2c — Disponibilità unificata (01/09)

Il calcolo della disponibilità usava una logica diversa da quella dell'assegnazione: due risposte
possibili alla stessa domanda. Ora usano lo stesso motore. Aggiunto il riepilogo sala nella pagina
Postazioni. Introdotto **un solo orologio** (`IClock`) al posto dei `DateTime.Now` sparsi.

> **Prova raccolta il 03/09**: in un log si leggeva `[12:59:48 WRN] ... - 09/03/2026 10:59:48`.
> Il primo orario è quello scritto dal sistema di log, il secondo era scritto a mano dentro il
> messaggio: **stesso evento, due orari a due ore di distanza**.

## Fase 3 — Prenotazioni simultanee (02/09)

Il problema: due persone che prenotano nello stesso istante potevano ottenere lo stesso tavolo.
Risolto con un **indice unico** a livello di database sulla combinazione tavolo + data + fascia —
l'unico modo davvero sicuro, perché il controllo applicativo lascia sempre una finestra fra la
verifica e la scrittura.

Le scritture sono state messe dentro transazioni. L'errore tecnico del database viene tradotto in
un messaggio leggibile (409). Convertite 37 eccezioni generiche in eccezioni con un significato
preciso.

> **Nessun test automatico di concorrenza**: il database finto usato nei test non applica gli
> indici unici, e si è deciso di non introdurre un database vero nei test. La prova è stata
> manuale, in produzione, da due browser.

> **Trappola — BOM negli script SQL**: lo script generato da EF ha un carattere invisibile in
> testa che psql attacca alla prima istruzione. Risultato: `START TRANSACTION` fallisce e tutto
> lo script gira **senza rete di sicurezza**. Successo in produzione il 02/09.

## Fase 4 — Sicurezza e primo avvio (03/09)

Schermata di primo avvio per creare il primo amministratore, al posto di un endpoint pubblico.
Tre scelte non ovvie: la creazione è messa in fila (il controllo "esiste già un Admin?" e la
creazione non sono un'operazione sola); se l'assegnazione del ruolo fallisce l'utente appena
creato viene cancellato, altrimenti si resterebbe con lo username occupato e zero Admin, cioè
bloccati per sempre; gli errori vengono tradotti nella stessa forma del resto dell'API.

Corretto anche il codice di errore per "permesso negato": era 401 (non autenticato) invece di
403 (autenticato ma non autorizzato). Ripuliti i log dai dati personali: su un login fallito
l'email non è detto sia di un nostro utente.

## Fase 5 — Test del backend (03–04/09)

Da 31 a 74 test. Il punto di partenza era che i test verdi **non toccavano** la creazione di una
prenotazione, cioè la cosa che il prodotto esiste per fare. Coperti creazione e modifica,
assegnazione reale del tavolo (prima era testato solo un metodo gemello, non quello usato),
disponibilità, dashboard, ruoli e i due lavori notturni programmati.

> **Trappola**: due test usavano una data fissa (7 settembre 2026). Col passare del tempo quella
> data sarebbe diventata "ieri" e il test avrebbe iniziato a fallire da solo, senza che nessuno
> avesse toccato il codice. Sostituita con un lunedì calcolato al momento.

## Fase 6 — Bug del frontend (04/09)

- **Schermata bianca (REV-014)**: un token danneggiato in memoria faceva esplodere il primo
  disegno della pagina, e **nemmeno il login era raggiungibile**: si usciva solo svuotando la
  memoria del browser dagli strumenti per sviluppatori. Ora il token illeggibile o scaduto viene
  scartato e si riparte da anonimi.
- Aggiunta una rete di sicurezza per gli errori imprevisti. **Difetto emerso provando**: non
  intercettava gli errori dentro le pagine, perché React Router ha una propria rete che li
  cattura prima e mostra lo stack tecnico a schermo. Aggiunta anche quella.
- **Scelta della zona (REV-015)**: il campo non era registrato nel form, quindi chiudendo e
  riaprendo la finestra si vedeva ancora la zona di prima. **Difetto emerso provando**: la lista
  delle zone chiamava un endpoint riservato ad Admin e Staff, quindi **per il Cliente era sempre
  vuota**.
- **Modifica prenotazione (NEW-001)**: l'endpoint esisteva dal principio ma nessuno lo chiamava.
  **Difetto emerso provando**: un campo del DTO era dichiarato e mai valorizzato, usciva sempre 0
  — lo stesso identico schema di REV-001. Secondo bug della stessa famiglia, ora coperto da test.
- **Eliminazione prenotazione (NEW-004)**: stessa storia, il codice c'era e nessuno lo usava.
- **Dashboard e fuso orario (REV-016)**: le date erano calcolate in UTC, quindi fra mezzanotte e
  le due la dashboard mostrava il giorno prima.
- **Indirizzo del server mancante (REV-017)**: senza la variabile di configurazione le chiamate
  finivano sul sito stesso. **Difetto emerso provando**: la prima correzione bloccava il
  caricamento e lasciava la pagina **bianca**, lo stesso sintomo che la fase doveva eliminare.

> **Verifica riutilizzabile**: si può controllare che una variabile Vite sia impostata su Vercel
> **senza aprire il pannello**, guardando il pacchetto pubblicato: se la variabile c'è, il ramo
> di codice che gestisce la sua assenza sparisce del tutto. La sua **assenza** è la prova.

## Fase 7 — Robustezza del backend (04–07/09)

- **Correttezza**: `?page=0` faceva fallire la query; l'ordinamento della paginazione non era
  completo, quindi navigando le pagine si vedevano righe doppie e se ne perdevano altre;
  eliminando una fascia oraria restava prenotabile per 30 minuti perché la cache non veniva
  svuotata del tutto.
- **Prestazioni**: elenco utenti da "una query per ogni utente" a due query fisse; i due lavori
  notturni scrivevano una riga per volta, ora due query in tutto.
- **Dati e storico**: eliminare un utente non cancella più il suo storico. Indici sulla tabella
  delle attività.
- **REV-099**: un tavolo usato una volta non era più modificabile né disattivabile, per sempre.
  Ora il controllo guarda solo le prenotazioni **future**.

> **Difetto trovato da un test già esistente**: il conteggio dei tavoli occupati partiva
> dall'elenco delle fasce configurate, quindi le prenotazioni prese su una fascia poi disattivata
> sparivano dal totale.

### REV-029 — l'indirizzo IP del client, tre giorni e tre deploy

L'indirizzo di chi chiama si ricava scartando **un** anello in fondo alla catena dei proxy e
prendendo l'ultimo rimasto. Non il primo, che il client può falsificare; non l'ultimo, che è il
proxy. La catena misurata in produzione ha due livelli di proxy.

La prima soluzione tentata era **quella giusta**, ma è stata scartata perché la diagnostica
mostrava un solo valore nell'header — che era il residuo **dopo** il passaggio del middleware
che lo consuma (vedi regola di metodo 2).

Il fix non riguardava solo i log: il limite ai tentativi di login usa lo stesso valore, quindi
prima era di fatto un limite **globale** di 5 tentativi al minuto per tutta l'applicazione —
non fermava chi attacca un singolo account e poteva bloccare gli utenti veri.

L'endpoint di diagnostica è rimasto in produzione **di proposito**: il numero di proxy dipende
dalla piattaforma, e se cambia l'indirizzo torna silenziosamente sbagliato senza che nessuno se
ne accorga.

## Fase 8 — Robustezza del frontend (07/09)

- **Un solo posto per gli errori**: lo stesso blocco di sei righe era ripetuto **26 volte**.
  Aggiunto un caso che prima non esisteva: senza risposta, la richiesta non è mai partita
  (server spento, rete assente), ed è un'informazione diversa da "errore durante il salvataggio".
- **Oltre 100 prenotazioni i dati sparivano**: la pagina ne chiedeva 100 fisse e buttava via il
  totale che il server già mandava. Dalla 101ª in poi i dati non esistevano per l'interfaccia,
  senza alcun avviso. Ora 20 per pagina con barra di navigazione.
- **Il doppio invio era reale**: il pulsante si riabilitava con la richiesta ancora in volo.
- **Controllo dei tipi attivato**: zero errori. Uno zero è sospetto, quindi controprova —
  iniettato un errore che solo quel controllo rileva, comparso e poi sparito.
- **Primi 26 test del frontend**, prima non ce n'erano.
- **La password aveva quattro definizioni diverse** e solo una era quella giusta: registrazione e
  creazione utente accettavano 6 caratteri qualsiasi mentre il server ne chiedeva 8 con maiuscola,
  numero e carattere speciale. Il form lasciava passare e il rifiuto arrivava dal server.

## Fase 9 — Pulizia (07/09)

Codice morto (cartelle vuote, un secondo costruttore fantasma che se fosse stato scelto avrebbe
avviato il servizio con tutte le dipendenze vuote), duplicazioni (lo stesso metodo copiato in 6
punti, la stessa proiezione scritta 4 volte, costanti ripetute in 3 file), refusi, 31 orari
scritti a mano nei log che duplicavano quello del sistema di log.

`npm audit` segnalava 18 vulnerabilità nelle librerie: risolte **tutte** senza cambiare versione
principale di nulla. Zero vulnerabilità residue.

> **REV-056 rimandato di proposito**: allineare del tutto il nome "fascia oraria" avrebbe
> richiesto di cambiare l'indirizzo di un endpoint, rompendo tutti i collegamenti del frontend
> appena aggiornati, per un guadagno di sola leggibilità.

## Fase 10 — Esperienza d'uso (07–08/09)

- **Responsive**: da smartphone il menu laterale fisso occupava più di metà schermo e 7 tabelle
  non scorrevano. Ora il menu si richiude e le tabelle scorrono.
- **Accessibilità**: nessuna etichetta era collegata al proprio campo in 12 form; una finestra
  era fatta a mano, senza chiusura con ESC.
- **Liste vuote senza spiegazione**: mostravano solo l'intestazione della tabella, sembrava un
  errore. Aggiunto un messaggio in tutte e cinque le pagine.
- **Caricamento a pagina intera**: 6 pagine sostituivano tutta la vista, facendo sparire anche
  intestazione e filtri. Gli errori dicevano tutti la stessa frase a prescindere dalla causa.
- **Semaforo di disponibilità (NEW-002)**: prima si scopriva che una fascia era piena solo dopo
  aver premuto Salva. Ora le opzioni piene sono segnate "esaurita".

Prove manuali di Fabio superate il 07/09 da PC e da smartphone sui tre ruoli, 25 controlli.

> **Non coperto da test automatici**: il semaforo di disponibilità. Sopra la logica già coperta,
> aggiunge solo una lettura e un'etichetta. Da verificare a mano se si tocca.

## Fase 11 — Chiusura (08/09)

Un solo difetto, ma sulla base di tutto: nella **creazione** di una fascia oraria, il codice
identificativo arrivava dal client e veniva usato per escludere una fascia dal controllo di
sovrapposizione. Chi inviava il codice di una fascia esistente si faceva escludere proprio quella
e creava una fascia sovrapposta. Il difetto era **doppio**: lo stesso codice veniva anche copiato
sulla riga inserita, tentando di forzare una chiave che genera il database.

Fatto in TDD — i due test scritti prima e visti fallire per il motivo giusto: la fascia
sovrapposta veniva creata **senza alcuna eccezione**. 239 test verdi (erano 237).

---

## Incidenti e cose andate storte

### File nuovi persi in un commit (04/09)

Il primo commit della Fase 7 ha incluso solo i file già tracciati: 12 file nuovi non sono entrati
e sono stati persi dal working tree. Complicazione: un file tracciato era stato committato **già
aggiornato** mentre il file che gli corrispondeva mancava, quindi rigenerando in quello stato
sarebbe uscito un risultato vuoto.

> **Nota sul conteggio dei file**: `git status` breve raggruppa le cartelle nuove, quindi mostra
> meno file di quanti ne stai committando davvero. Per l'elenco reale serve
> `git status --untracked-files=all`.

### Dati e credenziali finiti nel repository pubblico (02–03/09)

Tre file di backup con dati erano stati committati su un repository **pubblico**. Storia di Git
riscritta con `git-filter-repo` e forzata sul remoto; verificato che i file non sono più
raggiungibili. I dati erano inventati, quindi nessuna segnalazione a GitHub.

> ⚠️ **Da sapere**: dopo una riscrittura forzata il vecchio commit resta raggiungibile
> **conoscendone il codice** finché GitHub non fa pulizia. Con dati davvero sensibili non basta:
> va chiesta la rimozione all'assistenza.

Password del database e chiave di firma dei token, esposte in chat, sono state entrambe
sostituite. Una password è finita a schermo perché `Read-Host` senza `-AsSecureString` la mostra.

### Errore ripetuto: modifiche fatte sul branch sbagliato

Almeno tre volte le modifiche sono partite su `main` invece che su `dev`. Sempre corrette prima
del commit, nessun danno, ma la regola resta: **controllare il branch a inizio di ogni blocco di
modifiche**, non solo a inizio sessione.

### Due progetti Railway invece di uno (14/08)

Database e applicazione erano stati creati in due progetti Railway distinti, e i riferimenti fra
variabili funzionano solo **dentro lo stesso progetto**. Da qui: su Railway **un progetto = una
applicazione con tutti i suoi servizi**, non un servizio per progetto.

### Una modifica al database applicata a mano e mai registrata (31/08)

Il rename della capienza era stato applicato con psql **senza** registrare la riga nella tabella
di storico delle migration. Lo schema era corretto ma EF continuava a considerarla da applicare,
e un futuro aggiornamento avrebbe ritentato il rename. Riga inserita a mano il 04/09.

> **Regola**: se una modifica si applica a mano, usare lo script generato da
> `dotnet ef migrations script`, che include già la riga di storico.

---

## Cose chiuse che sembravano difetti e non lo erano

**NEW-007 (08/09)** — segnalate due prenotazioni sullo stesso tavolo, stessa data e
apparentemente stessa fascia. Verificato con cinque query sui dati veri: l'indice unico è integro
e in tutta la tabella non c'è **una sola** coppia duplicata. Le due prenotazioni erano su fasce
diverse e non sovrapposte (20:00–21:30 e 21:30–23:30): il riuso corretto del tavolo nel secondo
turno. La lettura sbagliata nasceva dal numero 21:30 condiviso — fine dell'una, inizio dell'altra
— e dalle sette fasce con la stessa etichetta, una per giorno della settimana. Anche i "due
utenti diversi" erano apparenti: stesso account, e il nome visto era l'annotazione per le
prenotazioni prese al telefono.

Da questa indagine è però emerso il difetto vero poi corretto in Fase 11.
