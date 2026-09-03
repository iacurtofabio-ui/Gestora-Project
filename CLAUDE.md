# Progetto Gestora — Full Stack

## LEGGI QUESTO PRIMA DI TUTTO — STATO SESSIONE

Ultima sessione: 03/09/2026
Ultima cosa fatta: **FASE 4 CHIUSA** (stessa giornata in cui si è chiusa la Fase 3, vedi più
sotto). Nessun endpoint sensibile è più raggiungibile senza autenticazione: `seed-admin` pubblico
è **rimosso** e sostituito da una schermata di primo avvio (REV-007), un permesso negato non
provoca più il logout (REV-025), il Cliente non scrive un campo riservato a Staff/Admin
(REV-033), l'email è fuori dai log di accesso (REV-070).
`dotnet test` **74/74**, `tsc --noEmit` e `npm run build` puliti, `eslint` 0 errori.
**Nessuna migration.** Il primo avvio è stato verificato end-to-end su un DB locale **ricreato da
zero**; in produzione l'Admin esiste già, quindi la schermata è chiusa da sé.
**Rilasciata la `v1.0.1`** lo stesso giorno: merge `dev`→`main` (commit `66e7f5c`), tag
`v1.0.1` su GitHub, Railway e Vercel ridistribuiti. Prossimo passo: **Fase 5 — test del backend**.

### Fase 4 — riepilogo

- **REV-007** — nuovo `SetupController` **pubblico**: `GET /api/Setup/stato` risponde solo
  sì/no sull'esistenza di un Admin (niente dettagli sugli utenti, l'endpoint è aperto) e
  `POST /api/Setup/admin` crea il primo amministratore, poi si chiude per sempre (409).
  `POST /api/AuthenticationUser/seed-admin` **rimosso**. Tre scelte non ovvie:
  la creazione è serializzata da un `SemaphoreSlim` **statico** (il check "esiste già un Admin?"
  e la creazione non sono atomici — basta a istanza singola, **non** con più repliche: lì
  servirebbe un vincolo a database come in REV-003); se `AddToRoleAsync` fallisce l'utente
  appena creato viene **cancellato** (altrimenti username occupato + zero Admin bloccherebbero
  il setup per sempre); gli errori di Identity sono tradotti in `ValidationException` per avere
  la stessa forma di risposta del resto dell'API, altrimenti il frontend mostrerebbe un generico
  "riprova" invece di "questa email è già in uso".
- **REV-007 (frontend)** — `SetupPage` + hook `useSetup` + `SetupGuard` che avvolge `/login` e
  `/register`: finché non esiste un Admin ogni ingresso porta a `/setup`, appena esiste la
  guardia è trasparente e la pagina si autoredirige al login. **Nessun login automatico** dopo
  la creazione: si entra con le credenziali appena scelte, così si verificano subito. Se la
  chiamata di stato fallisce si prosegue al login, per non dirottare tutti sul primo avvio per
  un problema di rete.
- **REV-025** — nuova `ForbiddenException` (stesso schema di `ConflictException`) mappata a
  **403**; i 3 casi di permesso negato su prenotazione altrui non producono più un 401.
  `UnauthorizedAccessException` resta solo per le richieste **non autenticate**.
  Il frontend era **già corretto** (l'interceptor fa logout solo su 401 *con token presente*, e
  gli hook mostrano già `data.message`): il difetto era interamente nel mapping backend, nessuna
  modifica frontend necessaria.
- **REV-033** — `NomeCliente` è un campo di Staff/Admin (annotazione per le prenotazioni prese
  al telefono): ignorato in creazione e **lasciato invariato** in modifica quando a scrivere è un
  Cliente self-service, che quindi non può né impostarlo né cancellare l'annotazione dello Staff.
  Ignorato in silenzio invece di un 403, perché il form del Cliente non espone il campo.
- **REV-070** — 7 punti di log ripuliti in `AuthenticationUserController`: sui tentativi
  **falliti** resta solo l'IP (su un login fallito l'email non è detto sia di un nostro utente,
  e con un attacco a dizionario i log si riempirebbero di indirizzi di terzi), sui casi con
  utente noto si usa `UserId`. Email tolta anche dal messaggio dell'audit log di registrazione,
  dove `user.Id` è già il primo parametro.
- Test: +4 (`NomeCliente` scritto/ignorato in creazione e modifica), i 3 test sui permessi
  aggiornati a `ForbiddenException`. **74/74.**

> **Reset del DB locale — procedura**: `dotnet ef database drop` (senza `--force`, così stampa il
> database bersaglio e chiede conferma: è la rete di sicurezza) -> `dotnet ef database update` ->
> **`psql -U postgres -d gestora_db -f Scripts\quartz_postgres.sql`**. Quest'ultimo passo non è
> opzionale: le tabelle `QRTZ_` **non stanno nelle migration EF**, senza di loro l'app non parte.
> Il backend va **spento** prima del drop, altrimenti la connessione aperta lo blocca.

> **Da ricordare — nomi delle tabelle Identity**: in questo progetto sono rinominate in
> `Utenti` e `Ruoli`, **non** `AspNetUsers`/`AspNetRoles`.

> **Da ricordare — psql da PowerShell**: PowerShell 5.1 **mangia i doppi apici** negli argomenti
> passati a un eseguibile nativo, quindi `-c 'SELECT ... FROM "Utenti";'` arriva a psql senza
> virgolette e Postgres ripiega l'identificatore in minuscolo (`utenti` -> "relazione non
> esiste"). Vanno pre-escapate col backslash, dentro gli apici singoli.

### Fase 3 — riepilogo

- `PrenotazionePostazione` porta la copia denormalizzata di `DataPrenotazione` + `FasciaOrariaId`
  e l'unique index **pieno** `UX_PrenotazionePostazione_Slot`. Migration
  `20260902081401_AggiungiSlotPrenotazionePostazione` **scritta a mano** (lo scaffolding EF
  riempiva le colonne con `0001-01-01`/`0` e poi falliva l'indice): nullable → backfill →
  cancellazione righe delle annullate → `NOT NULL` → indice.
- `AddAsync` / `UpdateAsync` / `AnnullaPrenotazioneAsync` dentro
  `CreateExecutionStrategy().ExecuteAsync(...)` + transazione, via l'helper privato
  `EseguiInTransazioneAsync`. In `UpdateAsync` i DELETE sono salvati **prima** degli INSERT (EF
  non garantisce quell'ordine in una singola `SaveChanges`, e l'indice rifiuterebbe una modifica
  che riusa lo stesso tavolo).
- Annullo → cancella le righe join: l'annullata libera il tavolo.
- `ConflictException` + `DbExceptionTranslator`: `23505` su quel constraint → 409 leggibile,
  `23505` di altri indici → resta 500.
- **REV-026 chiuso** (era Fase 7): 37 `InvalidOperationException` convertite — 34 in
  `ConflictException`, 3 in `NotFoundException` (unico cambio di contratto: quei 3 casi da 409 a
  404). `InvalidOperationException` non è più mappata nel middleware.
- **REV-032 parziale**: audit log nella stessa transazione per creazione/modifica/annullo; Zone,
  Postazioni e Fasce restano alla Fase 7.
- Test: +13 (6 `DbExceptionTranslator`, 7 `PrenotazioniService`). **Nessun test di concorrenza
  automatico**: l'InMemory non applica gli unique index e si è deciso di non introdurre
  Testcontainers — la prova è quella manuale in produzione.

> **Da ricordare — BOM negli script SQL**: `dotnet ef migrations script` genera il file con BOM
> UTF-8; psql lo attacca alla prima istruzione, `START TRANSACTION` fallisce e **tutto lo script
> gira in autocommit**, senza rollback in caso di errore. È successo in produzione il 02/09 (esito
> corretto, ma senza rete di sicurezza). Il file in `GestoraWebApi/Scripts/` è stato ripulito e
> porta la nota in testa: se se ne rigenera uno, togliere il BOM prima di usarlo.

> **Rotazione password DB su Railway (02/09/2026)** — procedura e trappole:
> 1. La password va cambiata dal **tab Config del servizio Postgres** ("regenerate"), che aggiorna
>    insieme il database e le variabili. `ALTER USER` a mano cambia solo il database e lascia le
>    variabili indietro: il pannello poi non permette di allinearle, perché `PGPASSWORD` è un
>    **riferimento** a `POSTGRES_PASSWORD` (il valore vero vive lì).
> 2. `ConnectionStrings__DefaultConnection` del servizio .NET deve usare i **riferimenti**
>    (`${{Postgres.PGHOST}}` ecc.), non valori copiati: con i valori letterali ogni rigenerazione
>    rompe la connessione. I riferimenti vanno **digitati** nel campo (`${{` + autocomplete),
>    incollati da fuori restano stringhe morte.
> 3. ⚠️ **L'autocomplete di Railway mangia il carattere che precede il riferimento**: scegliendo
>    il valore dalla lista, gli `=` di `Port=`, `Database=`, `Username=`, `Password=` spariscono e
>    la stringa diventa `Port5432;Databaserailway;...`. Npgsql non trova i parametri e il database
>    risponde `password authentication failed` — errore che porta fuori strada, perché la password
>    è giusta. **Dopo ogni modifica contare i cinque `=`** con
>    `railway variables --service "Gestora-Project"` prima di deployare.
> 4. Verifica finale: `GET /health` → `Healthy` (copre anche il database, vedi Fase 1).

> **Nota su Railway**: il servizio Postgres **non ha TCP proxy pubblico**, quindi `pg_dump` da
> locale non è possibile (`postgres.railway.internal` non è raggiungibile da fuori). Il backup si
> fa con `\copy` dentro `railway connect Postgres`, tabella per tabella, come in Fase 2a.

### Emersi in Fase 3, da lavorare più avanti

- **REV-098** — la **modifica di una prenotazione non esiste nel frontend**: l'endpoint
  `PUT /update-prenotazione` c'è, ma non ci sono né hook né pulsante. **Già tracciato come
  `NEW-001`** nel foglio "Fix e Bug", pianificato per la Fase 6 insieme a REV-015 (stesso
  componente): REV-098 è solo il riferimento nel backlog scritto, non una segnalazione nuova.
- **REV-099** — `PostazioneService.UpdateAsync` blocca l'aggiornamento se esiste una qualsiasi
  riga in `PrenotazioniPostazioni`, anche di prenotazioni concluse: **un tavolo usato una volta
  non è più modificabile né disattivabile**. Proposta: Fase 7.
- **NEW-004** — l'hook `useDeletePrenotazione` esiste in `usePrenotazioni.ts` ma **nessun
  componente lo usa**: nel frontend c'è solo "Annulla", non "Elimina". Stesso schema di NEW-001
  (la modifica). Emerso il 03/09: eliminare una prenotazione passa solo da Postman. Fase 6.
- Password del database e token Admin esposti in chat il 02/09: **entrambi ruotati** (password il
  02/09, `JwtSettings__Secret` il 03/09).

### ✅ Chiuso il 03/09/2026 — sicurezza repository e credenziali

**1. Storia Git ripulita dai backup con dati.** I tre file (`backup_Prenotazioni_20260902.csv`,
`backup_PrenotazioniPostazioni_20260902.csv`, `backup_20260902_pre_slot.dump`) erano nel commit
`15f98a0`, già pushato su `dev` e `main` di un repository **pubblico**. Riscritta la storia con
`git-filter-repo` (`--invert-paths --paths-from-file`, 80 commit riparsati) e **force push** su
`dev` (`a07fda0`) e `main` (`b09e583`). Verificato: `git rev-list --objects --all | grep backup_`
non trova nulla, `raw.githubusercontent.../main/backup_...csv` → 404. I dati erano **inventati**,
quindi nessun ticket a GitHub Support.

> ⚠️ **Da sapere**: dopo il force push il vecchio commit resta raggiungibile **per SHA diretto**
> (oggetto orfano lato GitHub) finché non fanno garbage collection. Con dati realmente sensibili
> non basta il force push: serve chiedere la purga a GitHub Support.

> **Trappole incontrate**: `git filter-repo` non è un comando git nativo, va installato
> (`python -m pip install git-filter-repo`) e su Windows si invoca meglio come
> `python -m git_filter_repo`, perché lo script non finisce nel PATH. Il comando lungo va passato
> **da file** (`--paths-from-file`): incollato in PowerShell si spezza a metà. Il `repack` finale
> è andato in loop su una cartella `.git/objects` lockata da Windows: interrotto con Ctrl+C senza
> danni, la storia era già scritta (`git fsck` pulito).

**2. `.gitignore`**: aggiunte `backup_*.csv`, `backup_*.dump`, `*.dump` al file di **root**.
Erano finite prima in `GestoraWebApi/.gitignore` (file sbagliato) e con **spazi iniziali**, che in
gitignore fanno parte del pattern e rendono la regola inefficace. Verificato con `git check-ignore`.

**3. `JwtSettings__Secret` ruotato**: 64 byte da `RandomNumberGenerator` generati **direttamente
negli appunti** (`Set-Clipboard`, mai a video né in chat) e sostituiti su Railway. `/health` →
`Healthy`, login dal frontend ok: il token esposto il 02/09 è invalidato.

### Code aperte da questa sessione

- **`git gc --prune=now`** da rilanciare a freddo (dopo un riavvio): il repack di `filter-repo` è
  stato interrotto, restano oggetti orfani **locali**. Solo spazio su disco, nessun impatto sul
  remoto.
- **Tag `v1.0.0` disallineato fra locale e remoto** (scoperto il 03/09 preparando la `v1.0.1`):
  in locale punta a `807cca9`, su GitHub a `6430cc8`. È l'effetto della riscrittura con
  `git-filter-repo`: il tag locale ha seguito i nuovi SHA, quello remoto è rimasto agganciato al
  commit **vecchio**, che non appartiene più a nessun branch e prima o poi verrà raccolto dalla
  garbage collection di GitHub. Per questo `git fetch --tags` risponde
  `! [rejected] v1.0.0 (would clobber existing tag)`. Si riallinea con
  `git push --force origin refs/tags/v1.0.0` — contenuto identico, cambia solo lo SHA. La `v1.0.1`
  **non** ha questo problema: è nata dopo la riscrittura.
- **`Personali\Gestora_BACKUP_20260903`** — copia di sicurezza pre-riscrittura. Da cancellare
  quando si è tranquilli.
- **NEW-005 — pulizia generale del DB di produzione a fine progetto.** I dati di test (zona
  "Test concorrenza", tavolo da 2, prenotazione del 09/09) sono stati **lasciati apposta**: la
  produzione è anche l'ambiente di test. Ordine obbligato quando si farà: prima le prenotazioni
  (Admin, `DELETE /delete-prenotazione`, solo su `Attiva` o `Annullata`), poi le postazioni, poi
  le zone — finché la postazione ha righe join non è né modificabile né eliminabile (REV-099).
  Nota: una zona di test **attiva** compare nelle disponibilità reali; se dà fastidio va
  disattivata, non eliminata.

### ✅ Rilascio v1.0.1 (03/09/2026)

Decisa e fatta: le Fasi 1-4 sono in produzione. Merge `dev`→`main` in fast-forward (`main` era un
antenato diretto di `dev`, nessun conflitto possibile), commit `66e7f5c`, tag **`v1.0.1`**
pubblicato su GitHub sulla punta di `main`. Railway e Vercel si sono ridistribuiti da soli sul
push. **Nessuna migration**, quindi nessun backup né finestra di manutenzione: deploy di solo
codice.

Verificato in produzione dopo il deploy: `GET /health` → `Healthy`,
`GET /api/Setup/stato` → `{"setupCompletato":true}`,
`POST /api/AuthenticationUser/seed-admin` → **404** (endpoint rimosso).

> Il merge ha portato in produzione anche `094d146` ("Fase 3 fix + pulizia"), che era rimasto su
> `dev`: unico contenuto di codice, la correzione degli accenti nel messaggio di
> `ConflictException`. Nessun buco funzionale, la Fase 3 era già completa in produzione.

> La rimozione di `seed-admin` è un **breaking change** di API, innocuo qui (l'Admin esisteva già
> e l'endpoint era comunque autobloccato) ma da ricordare.

### Poi — Fase 5 (test del backend)

Coprire il percorso che il prodotto esiste per fare: oggi i 74 test verdi **non toccano** la
creazione di una prenotazione. Test su creazione e modifica (REV-051), sull'assegnazione reale
del tavolo — oggi è coperto solo un metodo gemello, non quello usato (REV-052), su disponibilità,
dashboard e ruoli (REV-053), sui due job notturni (REV-054). Nessun task 🧑 previsto.

### Checkpoint 2c — riepilogo (codice chiuso 01/09/2026)

Tre commit su `dev`:
- **Blocco 1+2** — `DisponibilitaService` riscritto: usa il motore `AssegnazioneTavoli` (non più
  `TrovaCombinazioniDisponibili`, rimosso), posti residui sul tetto `MaxCoperti` (decisione 8),
  esclude zone/tavoli disattivati (REV-024, applicato anche a `PostazioneAssignmentService`),
  `Messaggio` che distingue tetto esaurito da tavoli insufficienti. DTO `FasciaDisponibilitaDTO`
  +3 campi (nessun consumatore frontend di `check-disponibilita`). REV-002 (Staff/Admin modificano
  la prenotazione di un cliente), REV-034 (Cliente legge il dettaglio della propria), REV-006
  (audit log sulla modifica). Rimosso `IPrenotazioniRepository.GetAllPostazioniAsync` (morto).
- **Blocco 3** — endpoint `GET /api/Postazione/riepilogo-sala` (Admin/Staff) + card "Riepilogo
  sala" in cima a `PostazionePage` (decisione 9). `PostazioneService` ora dipende da
  `IFasciaOrariaRepository`.
- **Blocco 4** — un solo orologio (REV-016 / REV-092): nuovo `Common/IClock` (`SystemClock`,
  singleton) con `UtcNow` / `NowInRome` / `TodayInRome`. `PrenotazioniService` (rimosso il
  privato `GetNowInRome` duplicato), `DashboardService` + `DashboardController` (fix REV-016 +
  bug di precedenza nel calcolo del lunedì corrente), `PrenotazioneCreateDTOValidator` ora usano
  `IClock`. Job Quartz: log timestamp normalizzati a UTC. `TestClock` nel progetto test.
  I ~30 `DateTime.Now` nelle stringhe di log dei controller **non** toccati (rumore di logging,
  non "orologio di dominio") — candidati Fase 9.

> **Evidenza raccolta il 03/09**: in un log di login fallito si legge `[12:59:48 WRN] ... -
> 09/03/2026 10:59:48`. Il primo è il timestamp di Serilog (ora locale), il secondo è il
> `DateTime.UtcNow` scritto a mano dentro il messaggio: **stesso evento, due orari a due ore di
> distanza**. Non è solo ridondanza cosmetica, confonde chi legge i log.

> **Nota debito**: `PrenotazioniService` ha ancora il secondo costruttore fantasma con
> `object1..object7` (REV-010, assegnato a Fase 9). Non è un bug attivo — la DI sceglie sempre il
> costruttore più largo, ora a 10 parametri — ma va rimosso.

### Riprendere da qui — leggere in quest'ordine

1. **`ROADMAP_REVISIONE.md`** — il documento operativo da seguire, riscritto il 31/08/2026:
   Definition of Done, procedura per le migration in produzione, tracciabilità con ID
   `REV-001`…`REV-097`, Fase 2 già divisa in checkpoint 2a/2b/2c. **10 decisioni di prodotto**
   vincolanti in testa (le 9 del 28/08 + la 10 del 31/08 su "una prenotazione al giorno" Cliente,
   rimandata al backlog v2.0): non riaprirle.
2. **`REVISIONE_END_TO_END.md`** — la revisione completa da cui nasce la roadmap, ora con ID
   `REV-001`…`REV-097` su ogni segnalazione.

⚠️ **Attenzione**: `BACKEND_FIX_TODO.md` dichiara "nessun backlog residuo". **Non è più vero.**
Lo era prima della revisione del 28/08; da quel momento il backlog reale è
`REVISIONE_END_TO_END.md` e l'ordine di lavorazione è `ROADMAP_REVISIONE.md`.

### Fase 1 — riepilogo (chiusa il 31/08/2026)

Config di build/deploy portata nel repo (`GestoraWebApi/railway.json` + `global.json`), health
check esteso al database (`AddDbContextCheck`), avviso in log all'avvio se ci sono migration non
applicate, migration `StatoAsEnum` documentata come no-op intenzionale, pacchetti allineati (EF
Tools 9.0.9, rimosso `Serilog.Sinks.Seq` mai usato), 3 fix di sicurezza anticipati dalla Fase 4
(REV-008 lockout+rate limit login, REV-009 niente più leak di `exception.Message` sulle 500,
REV-013 policy password anche sul reset Admin). Su Railway tolto il comando di build
personalizzato dal pannello (ora vive in `railway.json`). Verificato: `dotnet test` 31/31 verdi,
`GET /health` in produzione → `Healthy` (ora copre anche la raggiungibilità del DB, non solo il
processo).

> **Nixpacks abbandonato durante il deploy, sostituito con Dockerfile**: due tentativi falliti
> sulla stessa causa di fondo (SDK .NET 6 invece di 9 il primo, poi `dotnet-sdk_9` assente dallo
> snapshot nixpkgs pinnato da Nixpacks il secondo) hanno reso chiaro che l'auto-detection Nixpacks
> per .NET 9 non è affidabile in questo ambiente. Sostituita con `GestoraWebApi/Dockerfile`
> (multi-stage, immagini ufficiali Microsoft `dotnet/sdk:9.0` e `dotnet/aspnet:9.0`) +
> `.dockerignore`; `railway.json` aggiornato con `"builder": "DOCKERFILE"`. Risolve anche REV-011
> (deploy non versionato). `global.json` mantenuto comunque, utile per fissare la versione SDK
> anche in locale/CI a prescindere da Railway.

### Fase 2, checkpoint 2a — riepilogo (chiuso il 31/08/2026)

Rename `MaxPrenotazioni`→`MaxCoperti` in modello, DTO, validator, mapping, tutti i service
coinvolti e nei test (backend), più tipi/form/tabella (frontend) — comportamento invariato, solo
il nome, con un'etichetta chiara aggiunta nel form ("Capienza massima (coperti)"). Migration
`RinominaMaxPrenotazioniInMaxCoperti` (semplice `RenameColumn`, reversibile), applicata in
produzione da Fabio via `railway connect Postgres` + `psql`, con backup mirato della tabella
(`\copy "FasceOrarie" TO ... CSV HEADER`, 7 righe) prima del rename. Verificato in produzione:
pagina Fasce Orarie, creazione/modifica prenotazione, Dashboard — tutto pulito, nessun errore.

> **Nota di processo**: due volte in questa sessione le modifiche sono state fatte per un attimo
> sul branch `main` invece di `dev` (stesso errore già capitato in passato, vedi storico Fase 5) —
> corrette entrambe le volte prima di committare, nessun danno, ma **controllare sempre il branch
> corrente a inizio di ogni blocco di modifiche**, non solo a inizio sessione.

### Fase 2, checkpoint 2b — riepilogo (chiuso il 31/08/2026)

Nuovo algoritmo di assegnazione tavoli. Estratto in un motore puro
`GestoraWebApi/Services/PostazioneAssignment/AssegnazioneTavoli.cs` (classe statica, nessuna
dipendenza da repository/DB — prima la logica era annegata dentro un metodo `async` e testabile
solo con mock pesanti). Regole implementate: capienza = somma, **+2 solo se l'unione è composta
esclusivamente da tavoli da 2** e ha almeno 2 tavoli; unioni fino a **4 tavoli della stessa zona**
(i tavoli si accostano fisicamente, mai tra zone diverse); vince la combinazione con **meno posti
sprecati**, a parità di spreco quella con meno tavoli — tavolo singolo e unioni valutati insieme,
non più "il singolo vince sempre".

> **Scelta di implementazione da ricordare**: valutare tutte le unioni fino a 4 tavoli su N tavoli
> liberi sarebbe O(N⁴) (~90.000 casi con 40 tavoli). Il motore genera invece le combinazioni sulle
> **capienze distinte** (3-5 valori in un locale reale), perché due tavoli di pari capienza sono
> intercambiabili; i tavoli concreti si scelgono solo alla fine. Il costo non cresce col numero di
> tavoli in sala.

Chiusi nello stesso checkpoint: **REV-001** — `PrenotazionePostazione.NumeroPosti` esisteva nel
modello e in tabella ma non veniva **mai** scritto (restava 0); ora `PrenotazioniService` lo
valorizza in creazione e in modifica tramite `AssegnazioneTavoli.DistribuisciCoperti` (i posti di
testata, che non appartengono a nessun tavolo, sono ripartiti sui tavoli dell'unione; la somma
corrisponde sempre ai coperti richiesti). È il dato su cui si appoggia il checkpoint 2c. Rimosso
anche il vincolo capienza 2/4/8 dai due validator (`PostazioneDTOValidator`,
`PostazioneUpdateDTOValidator`) → capienza libera da 1 in su; il form frontend accettava già
qualsiasi valore ≥1, quindi era un disallineamento silenzioso, ora risolto — nessuna modifica
frontend necessaria.

Firma cambiata: `AssegnaPostazioneDisponibileAsync` restituisce `List<PostazioneAssegnata>`
(record tavolo + posti occupati) invece di `List<Postazione>`.

**Nessuna migration**: la colonna `NumeroPosti` esisteva già, nessuna modifica di schema.
Verificato: `dotnet test` 42/42 verdi (erano 31), test manuali di Fabio in produzione ok
(prenotazione da 2 con soli tavoli grandi, unione per 8, unione mista 2+6 senza bonus, creazione
postazione con capienza 6 ora accettata).

### Fase 2, checkpoint 2c — riepilogo (chiuso 01/09/2026, test manuali inclusi)

In sintesi: `DisponibilitaService` unificato sul motore `AssegnazioneTavoli` (REV-001/REV-024,
posti residui sul tetto `MaxCoperti`), REV-002 / REV-034 / REV-006, riepilogo sala
(`GET /api/Postazione/riepilogo-sala` + card in `PostazionePage`), un solo orologio
`Common/IClock` (REV-016 / REV-092). Nessuna migration. Tutti e 5 i test manuali di Fabio passati.
Con questo la **Fase 2 è chiusa**.

> Nota sulle migration: per decisione del 28/08 **restano manuali**. Claude prepara la migration,
> Fabio la applica seguendo la procedura descritta in testa a `ROADMAP_REVISIONE.md`. Riguarda le
> Fasi 2 e 3.

---

### Storico — Rilascio v1.0.0 (27/08/2026)

**FASE 8 COMPLETATA — RILASCIO v1.0.0.** Tag `v1.0.0` creato e pushato su
`main` (commit `6430cc8`). Progetto in produzione, nessun backlog bloccante residuo.

URL produzione: backend `https://gestora-project-production.up.railway.app`, frontend
`https://gestora-project-xi.vercel.app`. CORS allineato (`AllowedOrigins__1` su Railway con
l'URL Vercel, verificato con preflight OPTIONS).

Credenziali Admin iniziali: salvate da Fabio in password manager, fuori dal repo (non
documentate qui per policy di sicurezza del progetto).

Nessun backlog residuo prima del rilascio v1.0: chiusi tutti i punti sospesi dalla Fase 5
(GAP-001, RBAC-002, AUDIT-001, NAMING-001-residuo, DEAD-CODE-001, dettaglio in
`BACKEND_FIX_TODO.md` sezione "Fix completate") e tutti i bug emersi nel testing integrato di
Fase 7 (404 Vercel su refresh/login fallito, pulsante Annulla mancante per il Cliente — vedi
storico sotto per il dettaglio).

dotnet test 31/31 verdi, `npm run build`/`tsc --noEmit` puliti.

Backlog non bloccante per dopo v1.0: `AppuntiFix.txt` (nuovo, sostituisce `Fix Fase 5.txt`) —
note d'uso quotidiano di Fabio su Gestora, es. "possibile miglioramento della lettura dei log"
(oggi solo via Railway → Deployments → View Logs, nessuna UI applicativa sull'audit trail
`Logging`).

### Verifica flussi automatizzati Quartz (27/08/2026)

Due job schedulati (`GestoraWebApi/Background/`, dettaglio in `GestoraWebApi/CLAUDE.md`):
- **`PrenotazioniCleanupJob`** (02:30, elimina prenotazioni `Completata` più vecchie di 6 mesi):
  **✅ verificato oggi** forzandolo a comando in locale (nuovo endpoint `POST
  /api/Jobs/trigger/{jobName}`, solo Admin) su una prenotazione di test con data falsificata a
  mano nel DB — cancellazione confermata da log e da verifica successiva (404 sull'id).
- **`PrenotazioniJob`** (02:00, completa automaticamente le prenotazioni `InCorso` scadute):
  **⏳ in verifica** — lasciato girare naturalmente stanotte su 2 prenotazioni reali in stato
  "in corso" con data odierna; controllare domattina che siano passate a "Completata".

Durante il test emerso un ambiente di sviluppo locale finalmente allineato: `localhost:5173`
(frontend, `.env.local` → `http://localhost:5099/api`) + `localhost:5099` (backend locale) + DB
Postgres locale, **separato in modo stabile e permanente** dalla produzione (Vercel + Railway +
DB Railway), non solo per il test di oggi. Il DB locale va tenuto allineato manualmente con
`dotnet ef database update` dopo ogni nuova migration (era rimasto indietro di una migration,
mancava la colonna `NomeCliente`).

Prossimo passo: nessun rilascio pianificato — il progetto è in produzione. Domattina confermare
l'esito di `PrenotazioniJob` sulle 2 prenotazioni di test, poi valutare se portare
`JobsController` (endpoint di trigger manuale job, oggi solo in locale) anche su Railway.
Eventuali richieste future vanno prima registrate come nuovo punto in `AppuntiFix.txt` o nel
tracker.

---

### Storico — Fase 5-6-7 (26-27/08/2026, per riferimento)

**Fase 5** (26-27/08): risolti i 5 bug della checklist manuale di Fabio (`Fix Fase 5.txt`) — 403
del Cliente su prenotazioni (endpoint sbagliato), pulsanti Staff visibili su Zone/Postazioni/Fasce
quando non dovevano, vincolo "una prenotazione al giorno" che bloccava Staff/Admin su prenotazioni
per conto cliente (richiesta una **migration EF Core**, applicata a mano su Railway via
`railway connect Postgres` + `psql`, stesso procedimento di FIX-009), campo `NomeCliente`
aggiunto. Poi chiuso il backlog rimasto in sospeso da sessioni precedenti: GAP-001 (UI creazione
utenti — pagina pubblica `/register` + bottone Admin), RBAC-002 (cutoff 2h per annullo/modifica
self-service Cliente, non applicato ad Admin/Staff), AUDIT-001 (log attività esteso a
Zone/Postazioni/Fasce), NAMING-001-residuo (rinominato `FasciaOrariaController.cs`), DEAD-CODE-001
(filtro postazioni disponibili). Emersi altri 2 bug durante la ri-verifica: filtro
`GetPostazioniPerZonaAsync` troppo restrittivo, Fasce Orarie non filtrate per giorno in creazione
prenotazione — entrambi corretti.

**Fase 6** (27/08): deploy frontend su Vercel (root directory `gestora-frontend`,
`VITE_API_URL` verso Railway), CORS aggiornato su Railway con l'URL Vercel.

**Fase 7** (27/08): testing integrato completo sui 3 ruoli in produzione. 2 bug emersi e risolti
(vedi sopra, "404 Vercel" e "pulsante Annulla Cliente").

⚠️ **Nota di processo (dalla Fase 5)**: durante una sessione precedente le modifiche erano state
fatte per errore a working-tree su branch `main` invece di `dev` (poi corretto, nessuna perdita).
Controllare sempre `git status`/branch corrente a inizio sessione prima di modificare file.

---

### Storico — Fase 4 (25/08/2026, per riferimento)
**FASE 4 COMPLETATA — backend verificato in produzione.**

Checklist Fase 4 eseguita con token Admin reale via Postman:
- `GET /api/Zona/get-all-zone` → 404 "Nessuna zona trovata" (comportamento noto, vedi FIX-007)
- `GET /api/FasceOrarie/fasce-attive` → 200 `[]` (conferma che FIX-003 tiene anche in produzione)
- `GET /api/Dashboard/giornaliera?data=2026-08-25` → 200 con tutti i contatori a zero (l'aggregato
  non soffre dell'anti-pattern di FIX-007)
- Log Railway del servizio .NET puliti, nessuna eccezione durante le chiamate di verifica

Nessun fix applicato in questa sessione: FIX-007 resta aperto, da decidere in Fase 5 se e come
sistemarlo prima di collegare il frontend (vedi `BACKEND_FIX_TODO.md`).

Prima di partire con la Fase 4 è stato anche riallineato il repo, rimasto disallineato dalla
sessione del 14/08: `dev` era avanti di 2 commit non ancora mergiati su `main` (chiusura Fase 3),
e la cartella `.vs/` di Visual Studio non era esclusa dal tracking (`.gitignore` aveva la riga
scritta come commento `# .vs/` invece che come regola attiva — corretto in `.vs/`). Mergiati
`dev` → `main` (PR #3) e poi `main` → `dev`, così i due branch sono di nuovo allineati.

Prossimo passo: **FASE 5 — fix/verifica frontend contro l'URL Railway**. È qui che va deciso
cosa fare di FIX-007 (il frontend gestisce già le liste vuote come errore o come stato vuoto?).

---

### Storico — Fase 3 (14/08/2026, per riferimento)
**FASE 3 COMPLETATA — backend online su Railway.**

URL produzione backend: `https://gestora-project-production.up.railway.app`
Progetto Railway: `romantic-enthusiasm` (environment `production`), contiene DUE servizi:
`Postgres` e il servizio .NET collegato al repo GitHub `Gestora-Project`, branch `main`.

### Causa del blocco della sessione precedente (risolta)

La variabile `ConnectionStrings__DefaultConnection` non si risolveva perché **database e
applicazione erano stati creati in due progetti Railway distinti**. I riferimenti tra variabili
(`${{Postgres.PGHOST}}`) funzionano solo tra servizi dello **stesso progetto** — per questo
`Postgres` non compariva nell'autocomplete. Nessun problema di sintassi né di permessi.
Soluzione: ricreato il servizio .NET dentro il progetto del database; il progetto orfano è stato
eliminato. Regola da ricordare: **su Railway un progetto = un'applicazione con tutti i suoi
servizi**, non un servizio per progetto.

### Modifiche al codice di questa sessione (commit `4801060`, mergiato su main via PR)

Tutte in `GestoraWebApi/Program.cs` salvo dove indicato:
1. **Fail-fast sulla configurazione**: se `ConnectionStrings:DefaultConnection` o
   `JwtSettings:Secret` mancano, l'avvio si ferma con un messaggio esplicito (prima l'errore
   emergeva come `The ConnectionString property has not been initialized` dentro `RoleSeeder`,
   illeggibile). Incluso il controllo di lunghezza minima 256 bit del segreto JWT.
2. **Connection resiliency** (`EnableRetryOnFailure`, 5 tentativi / 10s): la rete privata tra
   container non è raggiungibile nei primi secondi dopo l'avvio. Verificato che nel progetto non
   ci sono transazioni esplicite (`BeginTransaction`), quindi l'opzione è sicura.
3. **Endpoint `/health`** (`AddHealthChecks` + `MapHealthChecks`, nessun pacchetto aggiuntivo),
   impostato come Healthcheck Path su Railway: un deploy rotto ora risulta fallito invece di
   andare in crash loop silenzioso.
4. **Serilog**: sink su file spostato in `appsettings.Development.json`; in produzione solo
   console (il filesystem del container è effimero, Railway raccoglie lo stdout).
5. `appsettings.Development.json` **rimosso da `.gitignore` e versionato** — non contiene più
   segreti (sono negli User Secrets dal 13/08) ed è configurazione per ambiente. Attenzione: la
   configurazione .NET sovrascrive gli array **per posizione**, quindi il sink Console va
   riconfermato all'indice 0 in quel file o si perde.

### Configurazione Railway del servizio .NET (per riferimento)

- Source: repo `Gestora-Project` (NON `GestoraWebApi`, repo vecchio/obsoleto), branch `main`,
  Root Directory `GestoraWebApi`
- Build: Custom Build Command `dotnet publish GestoraWebApi.csproj -c Release -o out`
  (il default builda l'intera solution, test inclusi, e fallisce)
- Deploy: Healthcheck Path `/health`
- Variabili: `ConnectionStrings__DefaultConnection` (composta con riferimenti
  `${{Postgres.PGHOST}}` / `PGPORT` / `PGDATABASE` / `PGUSER` / `PGPASSWORD`, creati con
  l'autocomplete del campo — incollati da fuori NON si attivano), `JwtSettings__Secret`,
  `AllowedOrigins__0=http://localhost:5173` (URL Vercel da aggiungere in Fase 6),
  `PORT=8080`, `ASPNETCORE_URLS=http://0.0.0.0:${{PORT}}`

### Verifiche eseguite in produzione (14/08/2026)

- build Railway ok, `[1/1] Healthcheck succeeded`
- `GET /health` da internet → 200 `Healthy`
- `GET /api/Zona/get-all-zone` senza token → 401 (autenticazione attiva)
- primo Admin creato con `POST /api/AuthenticationUser/seed-admin` (endpoint autobloccante)
- login in produzione → token JWT valido (373 caratteri)
- chiamata autenticata al DB → risposta dal service (404 su lista vuota, vedi FIX-007)

> Nota: la rotta base è `/api/[controller]`, dove `[controller]` è il nome della classe **senza
> il suffisso `Controller`**. Quindi `AuthenticationUserController` risponde su
> `/api/AuthenticationUser/...` — **non** `/api/AuthenticationUserController/...` (errore facile,
> capitato il 03/09) e non `/api/Auth/...` come scritto in vecchia documentazione.

### Trovato durante la verifica: FIX-007 (registrato in BACKEND_FIX_TODO.md)

Sei endpoint in `ZonaController`, `PostazioneController` e `PrenotazioneController` restituiscono
404 invece di `200 []` su lista vuota — stesso anti-pattern già corretto con FIX-003 sulle sole
fasce orarie. Si manifesta **sempre su un DB di produzione appena creato**. Da valutare in Fase 5:
il frontend potrebbe mostrare errori dove dovrebbe mostrare uno stato vuoto.

### Prossimo passo: FASE 4 — verifica backend in produzione

Con il token Admin: `GET /api/Zona/get-all-zone`, `GET /api/FasceOrarie/fasce-attive`,
`GET /api/Dashboard/giornaliera?data=`, controllo dei log Railway. Poi FASE 5 (frontend contro
l'URL Railway) — è lì che va deciso cosa fare di FIX-007.

### Iter di progetto — SEQUENZA OBBLIGATORIA (aggiornata post SA Assessment)
1. ~~Completare il frontend~~ ✅ FATTO
2. ~~Fix backend~~ ✅ FATTO (vedi `BACKEND_FIX_TODO.md` e `PIANO_RILASCIO.md`)
3. ~~Test backend (dotnet test + verifica manuale Swagger)~~ ✅ FATTO (31/31 test verdi)
4. ~~Deploy backend su Railway~~ ✅ FATTO
5. ~~Verifica backend in produzione~~ ✅ FATTO
6. ~~Fix/verifica frontend contro Railway URL~~ ✅ FATTO
7. ~~Deploy frontend su Vercel~~ ✅ FATTO (`https://gestora-project-xi.vercel.app`)
8. ~~Testing integrato su produzione~~ ✅ FATTO
9. ~~Rilascio v1.0.0~~ ✅ FATTO (27/08/2026, tag `v1.0.0` su `main`)

### File fix backend — LEGGERE AD OGNI SESSIONE
Ogni problema backend trovato va registrato in:
`C:\Users\Carlo Taranto\Progetti_Tech\02_Personali\Gestora\BACKEND_FIX_TODO.md`

### Nota React 19
Il progetto è stato inizializzato con React 19 (non 18 come da piano). Non è un problema — React 19 è stabile.

### Prossima cosa da fare (in ordine)

1. ~~Inizializzare progetto Vite + React + TypeScript~~ ✅ FATTO (React 19)
2. ~~Creare struttura cartelle src/ + Configurare Prettier (ESLint già presente)~~ ✅ FATTO
3. ~~Integrare shadcn/ui + Tailwind CSS~~ ✅ FATTO
4. ~~Setup React Router v6 con route protette per ruolo~~ ✅ FATTO
5. ~~Setup Axios con interceptor JWT (attach token + refresh/logout su 401)~~ ✅ FATTO
6. ~~Setup React Query (TanStack Query v5)~~ ✅ FATTO
7. ~~Pagina Login + hook useAuth con Context API~~ ✅ FATTO
8. ~~Layout shell: sidebar, header, area contenuto~~ ✅ FATTO
9. ~~Pagina Dashboard (consuma GET /Dashboard/giornaliera e /settimanale)~~ ✅ FATTO
10. ~~CRUD Zone, Postazioni, Fasce Orarie (Admin)~~ ✅ FATTO
11. ~~Gestione Prenotazioni (Staff + Cliente)~~ ✅ FATTO
12. ~~Pannello Admin utenti (consuma endpoint Auth)~~ ✅ FATTO
13. ~~Deploy su Vercel~~ ✅ FATTO


---

## Contesto progetto

Applicativo per la gestione organizzativa di attivita commerciali (ristoranti, pub, pizzerie).
Funzionalita principali: gestione postazioni, fasce orarie, prenotazioni online con assegnazione automatica tavoli.

### Stack tecnologico completo

BACKEND (completato):
- ASP.NET Core 9, C#, Entity Framework Core 9, PostgreSQL (Npgsql)
- Auth: ASP.NET Identity + JWT Bearer, 3 ruoli: Admin, Staff, Cliente
- Extra: Quartz.NET 3.15, FluentValidation 11, AutoMapper, Serilog, IMemoryCache
- Test: xUnit + Moq, 18 test unitari (tutti verdi)
- Deploy: Railway (backend + PostgreSQL)

FRONTEND (completato — deploy pendente):
- React 19 + TypeScript + Vite
- shadcn/ui + Tailwind CSS
- React Query (TanStack Query v5)
- React Hook Form + Zod
- React Router v6
- Axios con interceptor JWT
- Deploy: Vercel

MOBILE (fase futura):
- React Native + Expo


---

## Riferimento tecnico per area

- Backend (architettura, endpoint reali, note tecniche): `GestoraWebApi\CLAUDE.md`
- Frontend (stack, pattern CRUD, routing): `gestora-frontend\CLAUDE.md`

---

## Come affiancarmi

Sono un developer con 4 anni di esperienza su Dynamics 365 / Power Platform.
Sto costruendo questo progetto per fare uno switch di carriera verso il full stack.
Obiettivo dichiarato: trovare un'azienda che mi assuma come full stack developer.

Come mi devi affiancare (aggiornato 13/08/2026 — vedi nota sotto):
- Ruolo: senior developer che lavora insieme a me, io sono un middle developer che impara
- Implementa direttamente le modifiche di codice, frontend incluso — niente più procedura guidata
  passo-passo dove indichi lo step e aspetti che lo scriva io. La guida passo-passo ha rallentato
  troppo su fix piccoli/meccanici; l'ho chiesto io di cambiare approccio.
- Quando c'è un concetto nuovo o non ovvio, spiegalo comunque (breve, non un tutorial) — ma senza
  bloccare l'implementazione in attesa che lo scriva io
- Indica quando qualcosa non e production-ready e perche
- Suggerisci le best practice del settore, non solo la soluzione che funziona
- Obiettivo finale: progetto portfolio-ready che dimostri competenze full stack reali
- Il backend e completato — non modificarlo salvo regressions, bug critici, o richieste esplicite
  di cambio architetturale (es. RBAC)
- Ricorda comunque che non ho esperienza pregressa sul frontend — se introduci un pattern nuovo
  spiegalo, solo senza il cerimoniale a step

> Nota: fino al 13/08/2026 questo file richiedeva un protocollo di affiancamento passo-passo
> rigido (mostrare solo esempi parziali, un'istruzione alla volta, mai scrivere codice al posto
> di Fabio). Rimosso su sua richiesta esplicita per accelerare i fix piccoli — vedi
> TrackAttività_Gestora.xlsx, foglio Appunti e Step, per il contesto.

---

## Tracker attivita — PRIORITA MASSIMA

Il tracker **unico e ufficiale** si trova in: `TrackAttività_Gestora.xlsx` (stessa cartella di
questo file). Claude lo legge e aggiorna tramite PowerShell + Excel COM — nessun allegato
necessario.

> Non esistono altri tracker validi. Se in futuro compare un secondo file xlsx/md che sembra un
> tracker di progetto (è già successo con `Gestora_Piano_Operativo.xlsx`, generato da una
> sessione Claude esterna e rimosso il 12/08/2026 perché duplicava e disallineava lo stato),
> non aggiornarlo — segnalarlo a Fabio come possibile doppione prima di usarlo.

### Procedura standard di ripresa sessione

1. Leggere il blocco "LEGGI QUESTO PRIMA DI TUTTO — STATO SESSIONE" in cima a questo file
2. Leggere il foglio **Appunti e Step** di `TrackAttività_Gestora.xlsx`
3. `git status` — se ci sono modifiche non committate da prima, capire cosa sono prima di
   assumere che siano lavoro "in corso"
4. Se si riprende un task a metà: eseguire la skill `verifica-gestora` per un'evidenza fresca
   (build/test) invece di fidarsi dell'ultimo stato scritto a mano
5. Procedere con il lavoro richiesto

### Protocollo sessione (tracker)

1. Inizio — leggere questo file + foglio Appunti e Step del tracker
2. Dopo ogni implementazione — aggiornare stato nel tracker da "Da fare" a "Completato"
3. Nuova decisione architetturale — aggiungere riga in "Note e Decisioni" nel tracker
4. Fine sessione — aggiornare il blocco "LEGGI QUESTO PRIMA DI TUTTO" qui sopra + commit Git

### Protocollo commit Git

- Il commit e il push li fa SEMPRE Fabio, mai Claude
- Claude deve fornire il messaggio di commit con l'elenco delle modifiche effettuate
- Formato messaggio:

feat: breve descrizione cosa hai fatto

Se incompleto:
feat: WIP - descrizione cosa stavi facendo (da completare)

### Operazioni delicate — REGOLA OBBLIGATORIA

Per operazioni che coinvolgono sicurezza o configurazione sensibile (es. .env, .gitignore, credenziali, variabili d'ambiente, rimozione file da tracking Git) Claude deve:
1. Spiegare cosa sta per succedere e perché
2. Passare le istruzioni a Fabio che le esegue
3. MAI eseguire queste operazioni autonomamente


---

## Protocollo aggiornamento tracker — REGOLE OBBLIGATORIE

Ogni volta che si aggiorna il tracker applicare SEMPRE queste regole su TUTTI i fogli:

### Fogli da aggiornare ad ogni sessione
1. Dashboard — aggiornare la data "Aggiornato: gg/mm/aaaa"
2. Appunti e Step — aggiornare data header + stati task
3. Roadmap — aggiornare stati
4. Piano di Sviluppo — aggiornare stato settimana + date inizio/fine
5. Fix e Bug — aggiornare stati
6. Controllers — aggiungere SUBITO ogni nuovo endpoint o componente implementato

### Colori stati (applicare SEMPRE, nessuna eccezione)
- Completato     → verde         (#C6EFCE)
- Da fare        → giallo        (#FFEB9C)
- Parziale       → giallo        (#FFEB9C)
- In corso       → giallo        (#FFEB9C)
- Non necessario → grigio        (#D9D9D9)
- Pianificato    → azzurro       (#DDEBF7)
- Futuro         → grigio chiaro (#EDEDED)

### Regole generali
- Aggiornare TUTTI i fogli, non solo uno
- Quando si completa un task aggiornarlo su Appunti, Roadmap, Piano di Sviluppo e Fix e Bug contemporaneamente
- Le decisioni architetturali vanno aggiunte SUBITO in "Note e Decisioni" in Appunti e Step
- La data va aggiornata in Dashboard e nell'header di Appunti e Step

## graphify

This project has a knowledge graph at graphify-out/ with god nodes, community structure, and cross-file relationships.

Rules:
- For codebase questions, first run `graphify query "<question>"` when graphify-out/graph.json exists. Use `graphify path "<A>" "<B>"` for relationships and `graphify explain "<concept>"` for focused concepts. These return a scoped subgraph, usually much smaller than GRAPH_REPORT.md or raw grep output.
- If graphify-out/wiki/index.md exists, use it for broad navigation instead of raw source browsing.
- Read graphify-out/GRAPH_REPORT.md only for broad architecture review or when query/path/explain do not surface enough context.
- After modifying code, run `graphify update .` to keep the graph current (AST-only, no API cost).
