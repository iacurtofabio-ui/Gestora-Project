# CONSEGNA v1.1 — diario del lavoro di chiusura

Lavoro svolto in autonomia dal 18/09 al 21/09/2026 (Claude, modello Fable) sul branch `dev`, in
locale. **Questo è il documento da leggere per primo.** Per la versione in linguaggio semplice,
senza codice, vedi `GestoraDocs/DIARIO_LAVORO_v1.1.md`. Le sezioni qui sono nell'ordine in cui
Fabio deve agire: riepilogo per fase, giro di test, rilascio, bloccato/non fatto.

---

## 0. Sicurezza — ✅ risolto da Fabio il 21/09/2026

`envDBNeon.txt` conteneva la password del database Neon di produzione ed era tracciato in un
repository pubblico. **Fabio ha ruotato la password su Neon, aggiornato la variabile su Azure,
verificato `/health`, e tolto il file dal tracciamento Git** (confermato: `git status` lo mostra
come `deleted, staged`, pronto per il commit). Non c'è più nulla da fare su questo punto.

### 0.2 Altri controlli fatti sui segreti (esito)

- `git ls-files | Select-String -Pattern "env|secret|password|\.pfx|\.key"` → oltre a
  `envDBNeon.txt`, escono solo `AdminResetPasswordDTOValidator.cs` e `ResetPasswordModal.tsx`:
  è la parola «password» nel nome del file, nessun segreto dentro. **Nient'altro.**
- `git grep -i "PGPASSWORD\|Password=" -- ':!*.md'` → **solo** `envDBNeon.txt`.
- `backup_LogActivities_20260904.csv` nella radice: **non tracciato** (il `.gitignore` lo esclude),
  ma contiene dati veri del registro attività. **Da cancellare.**

---

## 1. Vincoli incontrati nell'ambiente (letti prima di tutto)

- **Commit non eseguibili da Claude.** `.claude/settings.json` del progetto **nega**
  `git commit`, `git rm`, `git push` e `dotnet ef database update` (regola deny, non aggirabile
  e non aggirata). La deroga «commit locali su `dev`» del prompt non è quindi applicabile: ogni
  fase qui sotto riporta il **messaggio di commit pronto** e l'elenco dei file. Fabio committa
  fase per fase da Visual Studio (`git status --untracked-files=all` prima di ogni commit per
  contare i file nuovi).
- **Database locale**: già allineato (7 migration applicate, `dotnet ef migrations list` senza
  voci in sospeso). Seed di sviluppo caricato (`admin|staff|cliente@gestora.local` / `Sviluppo1!`).
- **Ambiente**: .NET SDK 10.0.401 (compila e testa il progetto net9.0 tramite `global.json`),
  Node 24, PostgreSQL locale attivo, User Secrets presenti, `.env.local` corretto.
- **Linea di base** prima di toccare qualsiasi cosa: `dotnet test` **239** verdi; `npm test`
  **35** verdi; `npm run build` pulito; `npm run lint` 0 errori e 1 avviso preesistente
  (`react-hooks/incompatible-library` in `PrenotazioneModal.tsx`, non bloccante).
- Le modifiche non ancora committate a `BACKLOG.md` e `CLAUDE.md` trovate all'avvio (chiusura
  `OPS-006`, scritte da Fabio) sono state **conservate** e vanno nel primo commit.

---

## 2. Riepilogo per fase

### Fase 0b — Sicurezza: credenziali fuori dal tracciamento ✅

**Fatto:** `.gitignore` di radice, sotto il blocco «backup di database»: regole `envDBNeon.txt`
e `env*.txt` con commento. Controlli sui segreti (esito in §0.2). Creato questo diario.

**Commit:** `chore: esclude file di credenziali dal tracciamento`
File: `.gitignore`, `GestoraDocs/CONSEGNA_v1.1.md`, più `BACKLOG.md` e `CLAUDE.md` (modifiche
di Fabio già presenti).

### Fase 1 — `CAP-001`: il tetto dei coperti è un vincolo vero ✅

**Fatto (backend):**
- `IFasciaOrariaRepository`/`FasciaOrariaRepository`: nuovo `GetByIdConLockAsync` che legge la
  fascia con `SELECT ... FOR UPDATE` (tolto anche l'`using static ...JSType` spurio).
- `PrenotazioniService.ValidatePrenotazioneAsync` usa il metodo con lock; contratto scritto nel
  commento: **solo dentro** `EseguiInTransazioneAsync`.
- `UpdateAsync`: validazione del tetto e guardia «una prenotazione al giorno» spostate **dentro**
  la transazione. I controlli di stato/permessi/cutoff restano fuori (non toccano il tetto).
- `DashboardService`/`CopertiFasciaDTO`: nuovo campo `CopertiOltreIlTetto`. `CopertiDisponibili`
  resta a 0 quando si sfora (il frontend lo usa per «pieno»).
- `FasciaOrariaService.VerificaDisponibilitaPerFasciaAsync` **lasciato com'è**: quell'endpoint
  non è chiamato dal frontend (`lib/endpoints.ts` non lo conosce).

**Fatto (frontend):** `types/dashboard.ts` + `DashboardPage.RigaFascia`: se
`copertiOltreIlTetto > 0` la riga mostra `· N oltre il tetto` in rosso al posto di «pieno».

**Test:** +2 in `PrenotazioniServiceTests` (Add e Update usano il metodo con lock e **mai** quello
senza, `Times.Never`), +2 in `DashboardServiceTests` (10 di tetto + 15 coperti → 5 oltre, 0
disponibili; fascia esattamente piena → 0 oltre). Controprova fatta: rimettendo `GetByIdAsync`
falliscono i test del lock, togliendo il calcolo fallisce quello dello sforamento.
**Backend 243 verdi.** Verificato anche a runtime contro Postgres: nel log compare
`SELECT * FROM "FasceOrarie" WHERE "Id" = @p0 FOR UPDATE` e la richiesta oltre il tetto riceve
409; con `--seed-sviluppo --stato-incoerente` l'API della dashboard risponde `copertiOltreIlTetto: 5`.

**Da verificare a mano (Fabio):** la prova a due browser descritta in `RUNBOOK.md` §2 (sezione
nuova «Prova manuale del tetto dei coperti»).

**Commit:** `feat: tetto coperti protetto da lock sulla fascia (CAP-001)`
File: `GestoraWebApi/Repositories/FasciaOrarie/IFasciaOrariaRepository.cs`,
`.../FasciaOrariaRepository.cs`, `GestoraWebApi/Services/Prenotazioni/PrenotazioniService.cs`,
`GestoraWebApi/Services/Dashboard/Dashboardservice.cs`, `.../DTOs/DashboardDTO.cs`,
`GestoraWebApi.Tests/Services/PrenotazioniServiceTests.cs`, `.../DashboardServiceTests.cs`,
`gestora-frontend/src/types/dashboard.ts`, `gestora-frontend/src/pages/DashboardPage.tsx`,
`RUNBOOK.md`, `GestoraWebApi/CLAUDE.md`, `BACKLOG.md`, `GestoraDocs/CONSEGNA_v1.1.md`.

### Fase 2 — Difetti piccoli e certi del frontend ✅

**Fatto:**
1. **Segnaposto pubblicato** (`RegisterPage.tsx`): `"SOTTOCrea il tuo account"` sostituito da un
   testo vero. Nessun altro residuo `SOTTO` trovato (l'unica altra occorrenza in `LoginPage.tsx`
   era già stata corretta in precedenza; una terza in `index.css` è solo un commento CSS).
2. **Loop dell'utente senza ruolo**: creato `src/lib/navigazione.ts` con `paginaDiCasa(roles)`,
   usato da `LoginPage`, `RedirectSeAutenticato`, `UnauthorizedPage`. Un utente con `roles: []`
   va sempre a `/unauthorized`, che per questo caso mostra un titolo dedicato («Il tuo account
   non ha ancora un ruolo») e **una sola azione, «Esci»** (logout + redirect a `/login`). Prima
   il caso zero-ruoli finiva in un ciclo: due punti mandavano a `/dashboard`, uno a
   `/prenotazioni`, e da lì `ProtectedRoute` rimbalzava di nuovo a `/unauthorized`.
3. **Staff e «Configura le fasce orarie»**: il pulsante compare solo per Admin
   (`useAuth().user.roles.includes('Admin')`); allo Staff resta il testo, riformulato.
4. **La vetrina non si trova**: aggiunto un link testuale «← Torna alla pagina del locale» sotto
   la card, sia in `LoginPage` sia in `RegisterPage`.
5. **Tendina poco leggibile**: `index.css` dichiara `color-scheme: light` su `:root` e
   `color-scheme: dark` su `.dark` (i controlli nativi seguono il tema); `native-select.tsx`
   aggiunge `text-foreground` e le classi `[&>option]:bg-popover [&>option]:text-popover-foreground`;
   l'orario nella tendina delle fasce (`PrenotazioneModal.tsx`) ora stampa `HH:MM–HH:MM` (trattino
   tipografico, senza secondi) invece di `HH:MM - HH:MM`.
6. **Ordine e nomi del menu** (`AppLayout.tsx`): un solo array `vociMenu` con `ruoli` e `gruppo`
   per voce (Dashboard · Prenotazioni, poi «Sala»: Zone · Tavoli · Fasce orarie, poi
   «Amministrazione»: Utenti). «Postazioni» rinominata **«Tavoli»** in menu (la pagina si
   intitola già così; la rotta `/postazioni` resta invariata, per non riaprire `REV-056`).
   «Admin Utenti» → «Utenti». Il Cliente vede solo Prenotazioni, senza separatori (un solo
   gruppo visibile → niente etichetta).
7. **Pagina 404**: nuova `pages/NotFoundPage.tsx` (usa `SchermataMessaggio`, come le altre
   schermate a tutta pagina) e rotta `path: '*'` in `router/index.tsx`, senza autenticazione
   richiesta. Porta alla pagina di casa se l'utente è loggato, altrimenti alla vetrina.

**Test:** nuovo file `src/lib/__tests__/navigazione.test.ts` (5 casi su `paginaDiCasa`, incluso
quello con `roles: []`). Aggiornato `PrenotazioneModal.fasce.test.tsx` per il nuovo formato
dell'orario (`–` senza spazi, senza secondi): le fixture di quel test usano già orari senza
secondi, quindi solo il separatore atteso è cambiato.

**Verifica:** `npm run build` (tipi ok), `npm run lint` (0 errori, il solito warning preesistente
su `PrenotazioneModal.tsx`, non toccato da questa fase), `node scripts/contrasto.mjs` (tutte le
coppie sopra soglia). **Frontend 40 test verdi** (35 preesistenti + 5 nuovi).

**Non verificato a mano nel browser**: come da regola, lo segnalo esplicitamente. La Fase 8
(verifica visiva con Playwright) è il momento previsto per guardare davvero queste pagine, in
particolare il menu riordinato e la tendina in tema scuro.

**Commit:** `fix: segnaposto register, loop utente senza ruolo, menu riordinato, tendine leggibili, 404`
File: `gestora-frontend/src/lib/navigazione.ts` (nuovo),
`gestora-frontend/src/lib/__tests__/navigazione.test.ts` (nuovo),
`gestora-frontend/src/pages/NotFoundPage.tsx` (nuovo),
`gestora-frontend/src/pages/RegisterPage.tsx`, `.../LoginPage.tsx`, `.../UnauthorizedPage.tsx`,
`.../DashboardPage.tsx`, `gestora-frontend/src/router/RedirectSeAutenticato.tsx`,
`.../index.tsx`, `gestora-frontend/src/layouts/AppLayout.tsx`,
`gestora-frontend/src/components/ui/native-select.tsx`,
`gestora-frontend/src/components/PrenotazioneModal.tsx`,
`gestora-frontend/src/components/__tests__/PrenotazioneModal.fasce.test.tsx`,
`gestora-frontend/src/index.css`, `GestoraDocs/CONSEGNA_v1.1.md`.

### Fase 3 — Stato «Non presentata» per le prenotazioni mai confermate ✅

**Fatto (backend):**
- `Enums/StatoPrenotazione.cs`: nuovo valore `NonPresentata = 4`. Nessuna migration: lo stato è
  salvato come stringa (`HasConversion<string>()`).
- `PrenotazioniService.AutomaticCompletPrenotazioniAsync`: dopo aver completato le `InCorso`,
  gira **sempre** (anche con zero completamenti) `AutomaticMarcaNonPresentateAsync`, che porta a
  `NonPresentata` le `Attiva` con data/fascia ormai passate. Le righe di collegamento ai tavoli
  restano (storico), come da indicazione.
- Transizioni: `ConfermaPrenotazioneAsync` rifiuta anche una `Attiva` già passata (409 «La
  prenotazione è già passata»); `AnnullaPrenotazioneAsync`/`UpdateAsync` rifiutano
  `NonPresentata` come `Completata`; `DeleteAsync` la ammette (a differenza di `Completata`);
  `AutomaticDeletePrenotazioniAsync` la elimina oltre i 6 mesi.
- `NonPresentata` esclusa dal conteggio coperti/occupazione ovunque lo è `Annullata`:
  `ValidatePrenotazioneAsync` (tetto), `PostazioneAssignmentService` (occupazione tavoli),
  `FasciaOrariaRepository.CountNumeroCopertiFasciaOrariaAsync`, `PrenotazioniRepository.GetPrenotazioniByDataAsync`.
- Dashboard settimanale: il no-show conta sia le `NonPresentata` esplicite sia le `Attiva` su
  data passata (finestra prima del job), coperti esclusi.

**Fatto (frontend):** `STATI_PRENOTAZIONE`/`STATO_LABELS` (→ «Non presentata»), `StatoBadge`
(pallino spento come Completata, testo in corsivo per distinguerla senza colore nuovo),
`AzioniPrenotazione` (nessuna azione di riga; solo «Elimina definitivamente» per Admin nel menu
«…»), nuova opzione nel filtro stato di `PrenotazionePage`.

**Test:** +8 in `PrenotazioniServiceTests` (job segna la scaduta, non tocca l'oggi non ancora
finito, conferma di una passata → 409, annulla una non presentata → 409, elimina una non
presentata → ok, cleanup la elimina oltre 6 mesi), +2 in `DashboardServiceTests` (no-show conta
le non presentate, coperti le escludono), +2 frontend in `AzioniPrenotazione.test.tsx`. Un test
preesistente (`ConfermaPrenotazioneAsync_SetsStatoInCorso_WhenAttiva`) aggiornato: ora serve una
fascia futura esplicita, altrimenti la nuova guardia lo rifiuterebbe a ragione. Un altro
(`AutomaticCompletPrenotazioni_NonToccaGliAltriStati`) non include più il caso `Attiva`: con la
Fase 3 quello stato **viene** toccato (giustamente), quindi il caso è ora un test a parte.
**Backend 250 test verdi, frontend 42 verdi.** Build e lint puliti.

**Controprova (due volte, per essere certi):** disabilitata la chiamata al nuovo sotto-job →
fallisce esattamente `AutomaticCompletPrenotazioni_SegnaNonPresentata_LaAttivaScadutaDiIeri` e
nessun altro, poi ripristinato.

**⚠️ Incidente durante questa fase, corretto.** Un `git checkout -- <file>` usato per annullare
la sola modifica della controprova ha invece scartato **tutte** le modifiche non salvate a
`PrenotazioniService.cs`, comprese quelle della Fase 1 (lock CAP-001) e della Fase 3 stessa:
`git checkout` su un file ripristina l'**intero** file all'ultimo commit, non l'ultima modifica.
Individuato subito confrontando `git status`/`git diff` con quanto atteso, e **tutte le modifiche
sono state riscritte** da questa stessa sessione (nessuna persa in modo permanente, perché mai
committate a `main`/`dev` remoto — solo lavoro locale non ancora salvato). Da quel momento la
controprova si fa con una copia di sicurezza del file (`cp file file.bak` prima, `cp` di ritorno
dopo), mai con `git checkout` su un file con modifiche non committate.

**Commit:** `feat: stato Non presentata per le prenotazioni mai confermate`
File: `GestoraWebApi/Enums/StatoPrenotazione.cs`,
`GestoraWebApi/Services/Prenotazioni/PrenotazioniService.cs`,
`GestoraWebApi/Services/Dashboard/Dashboardservice.cs`,
`GestoraWebApi/Repositories/FasciaOrarie/FasciaOrariaRepository.cs`,
`GestoraWebApi/Repositories/Prenotazioni/PrenotazioniRepository.cs`,
`GestoraWebApi/Services/PostazioneAssignment/PostazioneAssignmentService.cs`,
`GestoraWebApi.Tests/Services/PrenotazioniServiceTests.cs`, `.../DashboardServiceTests.cs`,
`gestora-frontend/src/types/prenotazione.ts`, `.../components/StatoBadge.tsx`,
`.../components/AzioniPrenotazione.tsx`, `.../pages/PrenotazionePage.tsx`,
`.../components/__tests__/AzioniPrenotazione.test.tsx`,
`GestoraWebApi/CLAUDE.md`, `BACKLOG.md`, `GestoraDocs/CONSEGNA_v1.1.md`.

### Fase 4 — Dati che il server ha e non manda: turno e coperti per tavolo ✅

**Fatto (backend):**
- `PostazioneAssegnataDTO.NumeroPosti` mappato in AutoMapper da `PrenotazionePostazione.NumeroPosti`
  (era il caso REV-001/NEW-001: il dato esiste dal checkpoint 2b, non usciva mai dall'API).
- `PrenotazioneDTO.NumeroTurno`: posizione (1-based) della fascia fra le fasce **attive** dello
  stesso giorno della settimana, ordinate per orario. Calcolato in `PrenotazioniService`
  (`ApplicaNumeroTurnoAsync`, una sola query per giorno della settimana coinvolto, tramite
  `IFasciaOrariaRepository.GetFasceByGiornoAsync`, lo stesso metodo del controller fasce), non in
  AutoMapper. Applicato a tutti i metodi di lettura: lista per data, mie prenotazioni, lista
  paginata, singola. 0 se la fascia non è più fra quelle attive (disattivata dopo la
  prenotazione): il frontend non stampa nulla in quel caso.
- Fasce orarie: `FasciaOrariaRepository.GetAllFasceAsync` ora ordina per giorno della settimana
  (lunedì primo: `((int)GiornoSettimana + 6) % 7`), poi orario, poi Id (ordinamento totale) —
  prima non ordinava affatto.
- `Development/SeedSviluppo.cs`: corretto un difetto nel seed di sviluppo, non nel motore vero —
  un'unione di due tavoli scriveva tutti i coperti sul primo e 0 sul secondo. Ora li distribuisce
  (metà per tavolo, arrotondato per eccesso sul primo), come farebbe davvero l'assegnazione.

**Fatto (frontend):**
- `types/prenotazione.ts`: `numeroPosti` e `numeroTurno`. `PrenotazionePage`: colonna Tavoli in
  formato `numero (coperti)` (coperti in `text-nota`, omessi se 0 — dato vecchio); sotto l'orario,
  in piccolo, «Nº turno» se `numeroTurno > 0`; separatore a tutta larghezza quando cambia la data
  (solo per Staff/Admin, con `dataEstesaInItalia`).
- `FasciaOrariaPage`: colonne invertite (Giorno prima di Orario), `nomeFascia()` in formato
  «venerdì 19:00–23:00». Nuovo `lib/giorni.ts` → `GIORNI_SETTIMANA_DA_LUNEDI`: stesso indice
  numerico di `GIORNI_SETTIMANA` (0 = Domenica, coerente col backend), ma elencato da lunedì.
  Usato dal `FasciaOrariaModal` per l'elenco dei giorni nel form.

**Test:** +2 backend in `PrenotazioniServiceTests` (turno = 2ª posizione per orario, non per id
della fascia; turno = 0 se la fascia non è più attiva), +1 in `PrenotazioneMappingProfileTests`
(assert su `NumeroPosti`, mancava). Aggiunto un setup di default
(`GetFasceByGiornoAsync` → lista vuota) nel costruttore dei test esistenti, altrimenti la nuova
chiamata andava in eccezione su Moq. **Backend 252 verdi, frontend 42 verdi.** Build e lint
puliti, contrasto colori invariato (nessun colore toccato in questa fase).

**⚠️ Nota di correzione al piano.** Il prompt indicava di calcolare `NumeroTurno` «usando il
repository (già in cache)»: la cache in realtà vive nel livello `FasciaOrariaService`
(`IMemoryCache`), non nel repository, che interroga sempre il database. Ho seguito la lettera
dell'istruzione (repository, non `_context` diretto) ma segnalo che ogni chiamata a
`ApplicaNumeroTurnoAsync` fa una query per ogni giorno della settimana coinvolto — accettabile per
il volume di Gestora (poche righe per pagina, al massimo 2-3 giorni diversi), ma non è la stessa
cosa di "già in cache".

**Non verificato a mano nel browser** (colonna Tavoli, turno, separatore per giorno, ordine delle
fasce): rimandato alla Fase 8.

**Commit:** `feat: turno e coperti per tavolo nell'API e in tabella, fasce ordinate per giorno`
File: `GestoraWebApi/Services/Prenotazioni/DTOs/PrenotazioneDTO.cs`,
`GestoraWebApi/Mappings/PrenotazioneMappingProfile.cs`,
`GestoraWebApi/Services/Prenotazioni/PrenotazioniService.cs`,
`GestoraWebApi/Repositories/FasciaOrarie/FasciaOrariaRepository.cs`,
`GestoraWebApi/Development/SeedSviluppo.cs`,
`GestoraWebApi.Tests/Services/PrenotazioniServiceTests.cs`,
`GestoraWebApi.Tests/Mappings/PrenotazioneMappingProfileTests.cs`,
`gestora-frontend/src/types/prenotazione.ts`, `.../pages/PrenotazionePage.tsx`,
`.../pages/FasciaOrariaPage.tsx`, `.../components/FasciaOrariaModal.tsx`, `.../lib/giorni.ts`,
`.../components/__tests__/AzioniPrenotazione.test.tsx`, `GestoraDocs/CONSEGNA_v1.1.md`.

### Fase 5 — Log leggibili ✅

**Fatto:**
- `appsettings.json`: `outputTemplate` sul sink Console:
  `[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext:l} {Message:lj}{NewLine}{Exception}`.
- **Decisione presa e annotata**: niente enricher per accorciare `{SourceContext}` al solo nome
  della classe. Costerebbe una classe nuova per un guadagno cosmetico, e l'unica dipendenza nuova
  ammessa in questa chiusura è il carattere della Fase 6. `{SourceContext:l}` stampa il namespace
  completo (es. `GestoraWebApi.Services.Dashboard.DashboardService`): più lungo ma sempre vero.
- `Program.cs`: `app.UseSerilogRequestLogging()` con `MessageTemplate` (`METODO PATH → STATUS in
  Nms`) e livello `Warning` per status ≥ 500 o eccezione, `Verbose` per `/health` (Azure lo chiama
  di continuo), `Information` altrimenti.
- Tolti i prefissi `[DashboardService]`/`[PrenotazioniService]` dai messaggi di log di quei due
  service: duplicavano `{SourceContext}`. **Non toccati** i log dei controller nel formato
  `[{Controller}] - [{Method}]:` (oltre 30 occorrenze): includono `{Method}`, un'informazione che
  `SourceContext` non dà (solo il nome della classe, non del metodo) — non sono duplicati come i
  due tolti, sono fuori dallo specifico citato nel piano. Segnalato qui per completezza, non
  toccato per restare nello scopo indicato.

**⚠️ Correzione a un punto del piano, verificata con una chiamata vera.** Il piano indicava di
mettere `UseSerilogRequestLogging` **dopo** `UseGlobalExceptionHandler`. Provandolo con una
richiesta che genera un 409 di dominio (fascia sbagliata per il giorno), il log di richiesta
diceva **500** mentre il client riceveva correttamente **409**: il middleware di log, essendo più
interno, vede l'eccezione prima che quello esterno la traduca nel codice giusto. Invertito l'ordine
(`UseSerilogRequestLogging` **prima** di `UseGlobalExceptionHandler`, quindi più esterno): riprovato
lo stesso 409, il log ora dice correttamente `409`. Motivazione scritta come commento in `Program.cs`.

**Verifica dal vivo (login + prenotazione + trigger job), 09/19 04:14-04:17 UTC:**

Prima (ordine sbagliato):
```
[06:14:25 ERR] GestoraWebApi.Infrastructure.Middleware.ExceptionMiddleware Unhandled Exception: La fascia oraria selezionata è valida solo per il giorno lunedì.
[06:14:25 INF] Serilog.AspNetCore.RequestLoggingMiddleware POST /api/Prenotazione/crea-prenotazione → 500 in 100ms   ← sbagliato, il client riceveva 409
[06:14:25 INF] GestoraWebApi.Services.Prenotazioni.PrenotazioniService 25 prenotazioni segnate come non presentate: 1140, 1143, ...
[06:14:25 INF] GestoraWebApi.Background.PrenotazioniJob PrenotazioniJob completed at 09/19/2026 04:14:25
```

Dopo (ordine corretto, prefissi tolti):
```
[06:17:06 ERR] GestoraWebApi.Infrastructure.Middleware.ExceptionMiddleware Unhandled Exception: La fascia oraria selezionata è valida solo per il giorno lunedì.
[06:17:06 INF] Serilog.AspNetCore.RequestLoggingMiddleware POST /api/Prenotazione/crea-prenotazione → 409 in 109ms
[06:17:06 INF] GestoraWebApi.Controllers.JobsController Job 'PrenotazioniJob' forzato manualmente via API da un Admin.
[06:17:06 INF] Serilog.AspNetCore.RequestLoggingMiddleware POST /api/Jobs/trigger/PrenotazioniJob → 202 in 18ms
[06:17:06 INF] GestoraWebApi.Background.PrenotazioniJob PrenotazioniJob started at 09/19/2026 04:17:06
[06:17:06 INF] GestoraWebApi.Services.Prenotazioni.PrenotazioniService Nessuna prenotazione da completare.
[06:17:06 INF] GestoraWebApi.Services.Prenotazioni.PrenotazioniService Nessuna prenotazione da segnare come non presentata.
[06:17:06 INF] GestoraWebApi.Background.PrenotazioniJob PrenotazioniJob completed at 09/19/2026 04:17:06
```
(seconda esecuzione del job: la prima aveva già marcato tutto, da cui «nessuna prenotazione»
nella seconda — è la prova che il job è idempotente, non un difetto).

**Test:** nessun test automatico dedicato (è formattazione/instradamento di log, non logica di
dominio). **Backend 252 verdi** invariati. Database locale reseedato dopo la prova (il job aveva
marcato 25 prenotazioni di sviluppo come non presentate).

**Commit:** `feat: log di richiesta e formato leggibile`
File: `GestoraWebApi/appsettings.json`, `GestoraWebApi/Program.cs`,
`GestoraWebApi/Services/Dashboard/Dashboardservice.cs`,
`GestoraWebApi/Services/Prenotazioni/PrenotazioniService.cs`, `GestoraDocs/CONSEGNA_v1.1.md`.

### Fase 6 — Restyle moderno, direzione Docker Hub ✅

**6.1 — Studio del riferimento.** Playwright ha funzionato (a differenza di quanto temuto in
partenza): screenshot e valori CSS reali misurati su `hub.docker.com` (home),
`hub.docker.com/_/postgres` (pagina interna) e la pagina Explore, a 1440px e 375px. Analisi in
`GestoraDocs/restyle/ANALISI.md`, screenshot in `GestoraDocs/restyle/riferimento/`. Le cinque
scelte identificate: un solo blu d'azione, bordi sottili al posto delle ombre, un raggio unico e
contenuto, molta aria, barra superiore fissa e piena di colore.

**6.2 — Token (`index.css`).**
- Font: **Inter Variable** al posto di Archivo (pacchetto `@fontsource-variable/inter`,
  self-hosted come prima; tolto `@fontsource-variable/archivo`). Rigenerato il ripiego metrico
  (`scripts/metriche-ripiego.mjs` ora punta al file Inter) per evitare lo spostamento di layout
  al caricamento del font.
- Raggi: da tre scaglioni a due (6px controlli/card, 12px ciò che galleggia, 2px bande invariato).
  Cambiati solo i **valori** dei token esistenti (`--radius-lg`, `--radius-xl`, `--radius-2xl`,
  `--radius-3xl`), non i nomi: nessun componente ha dovuto cambiare classe.
- Corpo del testo: da 0.875rem a 0.9375rem (Inter a corpo piccolo è un filo più stretta di
  Archivo). Peso di `text-sezione` abbassato da 600 a 500, per allontanarlo da `text-titolo`
  (600) e mantenere la gerarchia.
- **Colori non toccati**: la tavolozza attuale (un solo blu segnale, neutri freddi verificati)
  soddisfaceva già i criteri del piano («un solo blu, superfici fredde non calde»). Ho preferito
  non rimetterci mano per non rischiare di rompere il contrasto già verificato.

**6.3 — Navigazione: barra superiore fissa.** `layouts/AppLayout.tsx` riscritto: sidebar
laterale rimossa, sostituita da una barra fissa (`h-14`, sticky) con logo a sinistra, voci di
menu orizzontali al centro (voce attiva sottolineata in blu, non più sfondo pieno — stesso
linguaggio dei tab di Docker Hub), tema e un menu utente a tendina (avatar con iniziali
dell'email, email + ruoli + «Esci») a destra. Sotto i 1024px le voci vanno in un pannello a
scomparsa sotto la barra. La logica di filtro per ruolo (`vociMenu`, un solo array) è la stessa
della Fase 2, solo la resa è cambiata.

**6.4 — Componenti.** Passata su `table.tsx` (intestazione con sfondo tenue, maiuscolo, righe
più alte), `dialog.tsx`/`alert-dialog.tsx` (separatore sotto il titolo oltre a quello sopra le
azioni; **corretto un bug preesistente**: il piede del dialogo usava un margine/raggio pensato
per un padding diverso da quello vero del contenitore — invisibile prima perché la differenza
era di 2-4px, diventato più evidente col nuovo raggio). `button.tsx` e `card.tsx` non hanno
richiesto modifiche: erano già "bordo sottile, niente ombra, un solo blu pieno", il cambio dei
token di raggio si è propagato da solo.

**6.5 — Vetrina pubblica (`LandingPage.tsx`).** Header reso fisso (sticky, h-14) come quello
dell'area autenticata. Il pulsante primario dell'hero ora scorre al modulo di verifica
disponibilità (già visibile a fianco) invece di portare dritto alla registrazione — il modulo
stesso è la prova, non va nascosto. Le tre zone da un unico riquadro diviso da filetti a tre
card separate con bordo proprio, coerenti con `card.tsx`. Nessun contenuto o logo di Docker Hub
copiato: solo le proporzioni.

**Correzione a margine (non richiesta dal piano, trovata mentre lavoravo su `Enums/StatoPrenotazione.cs`
della Fase 3):** un commento XML mal formato (`HasConversion<string>` letto come tag XML aperto)
generava un warning di build. Corretto con `<c>HasConversion&lt;string&gt;</c>`.

**Verifica visiva fatta con Playwright** (non solo lint/build): vetrina in chiaro e scuro a
1440px, dashboard e prenotazioni con la barra superiore e il menu utente, fasce orarie con le
colonne invertite, un modal con tendina in tema scuro, vista mobile 375px con il menu a
scomparsa aperto. Screenshot in `GestoraDocs/restyle/dopo/`. Tutto confermato funzionante:
turno/coperti per tavolo/stato «Non presentata»/separatore per giorno della Fase 3-4 si vedono
correttamente insieme al nuovo aspetto.

**Non verificato con Playwright** (limite noto, lo dice anche il piano): il menu a tendina nativo
del browser (`<select>`) aperto — è disegnato dal sistema operativo, non dalla pagina, quindi non
compare nello screenshot. La correttezza del CSS (`color-scheme`, colori delle `<option>`) resta
verificata dal codice, non da un'immagine.

**Test:** nessun test automatico nuovo (è lavoro di aspetto, non di logica). **Frontend 42 test
verdi** (invariati), **backend 252 verdi** (invariati, solo il fix del commento XML). Build, lint
e `contrasto.mjs` puliti dopo ogni passo.

**Documentazione aggiornata:** `gestora-frontend/CLAUDE.md` (tipografia, raggi, barra superiore),
`GestoraDocs/verifica-redesign.md` (le voci che parlavano di "barra laterale" ora dicono "barra
superiore" — X1 aggiornata da quattro a tre livelli di superficie, essendo sparita la sidebar
come livello a sé).

**Commit:** `feat: restyle moderno — token, carattere, barra superiore, componenti, vetrina`
File: `gestora-frontend/src/index.css`, `.../scripts/metriche-ripiego.mjs`,
`.../src/layouts/AppLayout.tsx`, `.../src/components/ui/table.tsx`, `.../ui/dialog.tsx`,
`.../ui/alert-dialog.tsx`, `.../src/pages/LandingPage.tsx`, `.../package.json`,
`.../package-lock.json`, `GestoraWebApi/Enums/StatoPrenotazione.cs`,
`gestora-frontend/CLAUDE.md`, `GestoraDocs/verifica-redesign.md`,
`GestoraDocs/restyle/ANALISI.md` (nuovo), `GestoraDocs/restyle/riferimento/*.png` (nuovi),
`GestoraDocs/restyle/dopo/*.png` (nuovi), `GestoraDocs/CONSEGNA_v1.1.md`.

### Fase 7 — Dashboard più viva ✅

**Fatto (backend):**
- `GiornoSettimanaleDTO`: due campi nuovi, `CapienzaGiorno` (somma di `MaxCoperti` delle fasce
  attive di quel giorno della settimana, calcolata con una query raggruppata sola, non una per
  giorno) e `NonPresentate` (conteggio delle non presentate, valorizzato **solo** sui giorni già
  conclusi — su oggi e sui giorni futuri resta 0, altrimenti darebbe un falso "tutto ok" prima
  che il giorno sia davvero finito).
- `PrenotazioniQueryParams`/`GetAllPrenotazioniAsync`: nuovo filtro opzionale `FasciaOrariaId`,
  usato dal clic su una riga della dashboard (non richiesto esplicitamente dal punto 2 del piano,
  ma necessario per farlo funzionare: senza un filtro lato server per fascia, "&fascia=id"
  nell'URL non avrebbe avuto alcun effetto).

**Fatto (frontend):**
1. **Navigazione per giorno**: frecce ‹ › più pulsante «Oggi» più un campo data, in cima alla
   Dashboard. La settimana mostrata segue il giorno scelto (nuovo `lib/date.ts` →
   `lunediSettimanaDi(data)`, versione generica di quella già esistente per "oggi";
   `aggiungiGiorni(data, n)` per i due pulsanti).
2. **Righe cliccabili**: ogni fascia in "Le fasce del giorno" e ogni giorno in "Questa settimana"
   portano a `/prenotazioni?data=...` (e `&fascia=id` per la fascia). `PrenotazionePage` ora legge
   i filtri (data, stato, fascia, pagina) da `useSearchParams` invece che da stato locale: sono
   condivisibili e sopravvivono a un ricaricamento della pagina.
3. **Aggiornamento automatico**: `refetchInterval: 60_000` su entrambe le query della dashboard,
   `refetchIntervalInBackground: false` (non continua a interrogare il server per una scheda del
   browser non attiva). In alto a destra "aggiornato alle HH:MM". Le sei mutation di
   `usePrenotazioni.ts` (crea, modifica, conferma, completa, annulla, elimina) ora invalidano
   anche le due query della dashboard, non solo l'elenco prenotazioni — prima una prenotazione
   creata da un'altra pagina non aggiornava la dashboard fino al giro di refetch automatico
   (fino a un minuto di ritardo).
4. **Prossime in arrivo**: nuovo blocco fra le fasce e "Il resto della giornata", le prossime 5
   prenotazioni `Attiva`/`InCorso` del giorno scelto a partire dall'ora corrente (solo se il
   giorno scelto è oggi; su un altro giorno l'ordine parte dall'inizio). Riusa
   `AzioniPrenotazione` per intero (non solo il pulsante Conferma): stesse regole della tabella
   Prenotazioni, comprese le conferme, gli annullamenti e le eliminazioni.
5. **Settimana con le bande**: nuova colonna "Coperti / capienza" con una `BandaCoperti` piccola
   per riga, e per i giorni passati il conteggio delle non presentate accanto.
6. **Vivacità senza rumore**: nessuna animazione nuova aggiunta — le bande della fascia e della
   settimana riusano le classi già esistenti (`.banda-in-aggiornamento`, che rispetta
   `prefers-reduced-motion`), non serviva altro.

**Test:** +3 backend (`Settimanale_CapienzaGiorno_...`, `Settimanale_NonPresentate_...` ×2),
+1 backend (`GetAllPrenotazioniAsync_FiltraPerFasciaOraria`), +2 frontend nuovi file
(`DashboardPage.dateNav.test.tsx`: il pulsante "giorno successivo" cambia davvero la data chiesta
al backend; `PrenotazionePage.urlParams.test.tsx`: i parametri dell'URL arrivano alla chiamata
dei dati, con e senza filtri). Controprova fatta sul test dei parametri URL (disabilitato il
passaggio del filtro fascia → fallisce esattamente quel test). **Backend 256 verdi, frontend 45
verdi.**

**Verifica visiva con Playwright** (login, dashboard con la navigazione per giorno, blocco "In
arrivo" con azioni reali, tabella settimanale con le bande, clic su un giorno che porta alla
pagina Prenotazioni già filtrata — verificato che il filtro data risulti precompilato). Screenshot
in `GestoraDocs/restyle/dopo/dashboard-fase7-1440.png` e `prenotazioni-da-dashboard.png`. Database
locale reseedato dopo la prova.

**Commit:** `feat: dashboard navigabile per giorno, prossime prenotazioni, settimana con bande`
File: `GestoraWebApi/Services/Dashboard/DTOs/DashboardDTO.cs`,
`GestoraWebApi/Services/Dashboard/Dashboardservice.cs`,
`GestoraWebApi/Services/Prenotazioni/DTOs/PrenotazioniQueryParams.cs`,
`GestoraWebApi/Services/Prenotazioni/PrenotazioniService.cs`,
`GestoraWebApi.Tests/Services/DashboardServiceTests.cs`,
`GestoraWebApi.Tests/Services/PrenotazioniServiceTests.cs`,
`gestora-frontend/src/lib/date.ts`, `.../src/hooks/useDashboard.ts`,
`.../src/hooks/usePrenotazioni.ts`, `.../src/types/dashboard.ts`,
`.../src/pages/DashboardPage.tsx`, `.../src/pages/PrenotazionePage.tsx`,
`.../src/pages/__tests__/DashboardPage.dateNav.test.tsx` (nuovo),
`.../src/pages/__tests__/PrenotazionePage.urlParams.test.tsx` (nuovo),
`GestoraDocs/restyle/dopo/*.png`, `GestoraDocs/CONSEGNA_v1.1.md`.

### Fase 8 — `UI-001`: verifica visiva, parziale ✅⚠️

**Da sapere subito: questa fase NON è completa.** La checklist ha circa 90 voci; ne ho verificate
14 con Playwright (le 7 delle "prime dieci" più verificabili da screenshot, più X0/X1/X6). Le
restanti ~76 — in particolare tutti i controlli a 768px e 375px, il touch vero (non emulabile da
Playwright), i dialoghi di Zone/Fasce/Utenti, gli stati S1/S3-S7 — restano da fare a mano, come
già previsto dal piano stesso ("Fabio ripeterà a mano le voci che segnali"). Non ho chiuso
`UI-001` in `BACKLOG.md`: l'ho aggiornata per dire onestamente cosa è stato fatto.

**Esiti scritti voce per voce dentro `GestoraDocs/verifica-redesign.md`** (non solo qui), con la
riga `**Esito 21/09 (Fable):** ...` sotto ogni voce toccata, come richiesto dal piano.

**11 ✅:** D1 (banda del giorno), D3 (14 fasce con le soglie giuste), P2 (nome lungo troncato),
P3 (coperti per tavolo, aggiornato per la Fase 4), P4 (azione di riga a riposo, contorno visibile),
T4 (tendina zona con nome lungo, il popover Radix va a capo senza rompersi), U1 (riga utente come
testo, non pillole), X0 (tema scuro croma 0, misurato via codice, non a occhio), X1 (tre livelli
di grigio distinti nel dialogo, misurati), X6 (contorno del fuoco da tastiera visibile sulla
barra superiore), S2 (backend spento → riquadro rosso corretto → «Riprova» ripopola senza
ricaricare).

**2 ⚠️ (non verificabili con uno screenshot, non difetti):** D2 (l'animazione d'ingresso è un
movimento, uno screenshot statico non la cattura — il codice non è stato toccato da nessuna fase
recente); X4 (l'evidenziazione delle voci in "Fascia oraria"/"Zona preferita" nel modal di
prenotazione: sono `<select>` nativi, il menu aperto lo disegna il sistema operativo, non la
pagina — stesso limite già noto e documentato per il tema scuro delle tendine).

**1 ❌ trovato e corretto:** **T1**, esattamente il rischio che il piano stesso segnalava. Con le
26 fasce del dataset di sviluppo, il riepilogo "I tavoli bastano a coprire il tetto?" su Postazioni
spingeva davvero il selettore zona e la tabella dei tavoli fuori dalla prima schermata. Corretto:
l'elenco ora sta dentro un riquadro con altezza massima (`max-h-72`) e scorrimento verticale, il
resto della pagina resta sempre a portata. Ripreso lo screenshot dopo la correzione: confermato.

**Trovato ma non un difetto — documentazione disallineata:** P3 e "U4" (l'utente con tre ruoli)
descrivevano un formato/dataset di un seed più vecchio. P3 aggiornata al formato coperti-per-tavolo
introdotto in Fase 4. Su U4 il dataset attuale (6 utenti) non contiene un caso con tre ruoli né
un'email lunghissima: annotato nel file, non un difetto da correggere qui.

**Verifica finale:** backend 256 test verdi, frontend 45 verdi, build/lint/contrasto puliti dopo
la correzione di T1. Database locale reseedato.

**Commit:** `fix: correzioni dalla verifica visiva del redesign (UI-001, parziale)`
File: `gestora-frontend/src/pages/PostazionePage.tsx`, `GestoraDocs/verifica-redesign.md`,
`BACKLOG.md`, `GestoraDocs/verifica-redesign/screenshots/*.png` (nuovi),
`GestoraDocs/CONSEGNA_v1.1.md`.

### Fase 9 — Pulizie tecniche ✅

**Fatto:**
1. **OPS-005**: `@tanstack/react-query-devtools` spostato da `dependencies` a `devDependencies`
   in `package.json`, `npm install` per riallineare `package-lock.json` (ora ha `"dev": true` su
   quel pacchetto). Verificato che il bundle di produzione non lo contenga
   (`grep -l ReactQueryDevtools dist/assets/*.js` → nessun risultato).
2. **Residui Railway**: `GestoraWebApi/railway.json` eliminato; `GestoraWebApi/.github/workflows/`
   (vuota) eliminata insieme alla cartella `.github` che la conteneva; commento nel `Dockerfile`
   riscritto per Azure (`ASPNETCORE_URLS`/`WEBSITES_PORT` invece di "variabile Railway"); un
   commento in `SeedSviluppo.cs` che citava Railway come database di produzione aggiornato a Neon.
   **Non toccato** `RUNBOOK.md`: ha molti riferimenti a Railway (backup, rotazione password,
   pubblicazione) che descrivono procedure **intere** da riscrivere per Azure/Neon, non singole
   parole — è compito della Fase 10, dedicata proprio a questo, per non fare lo stesso lavoro due
   volte.
3. **Numeri dei test allineati**: `RUNBOOK.md` (239→256 backend, 26→45 frontend — il "26" era già
   disallineato anche rispetto alla baseline di partenza di questa sessione, 35), `CLAUDE.md`
   radice, `GestoraWebApi/CLAUDE.md`, `gestora-frontend/CLAUDE.md` (con il dettaglio dei nuovi
   file di test aggiunto).
4. **Controllo vulnerabilità**: `npm audit` → 0. `dotnet list package --vulnerable
   --include-transitive` → **1 trovata**, non c'era all'avvio di questa sessione: **AutoMapper
   12.0.1** (dipendenza transitiva di `AutoMapper.Extensions.Microsoft.DependencyInjection`,
   riferimento diretto nel `.csproj`), gravità **High**, avviso
   `GHSA-rvv3-g6hj-g44x`. **Non aggiornato**: la versione che risolve l'avviso è **16.2.0** — un
   salto di 4 versioni maggiori, non una patch/minor come la regola di questa fase permette da
   sola. AutoMapper ha cambiato licenza dalla 13 in poi (uso commerciale oltre una soglia di
   fatturato dell'azienda che lo usa): è una decisione che spetta a Fabio, non solo tecnica.
   **Segnalato**, non risolto.

**Verifica:** backend 256 verdi, frontend 45 verdi, build/lint puliti su entrambi.

**Da decidere (Fabio):** se/quando aggiornare AutoMapper oltre la 12.x, considerando il cambio di
licenza. Nel frattempo l'avviso resta aperto ma non blocca nulla: non è sfruttabile da remoto,
riguarda un dettaglio interno della libreria di mapping.

**Commit:** `chore: devtools in devDependencies, residui Railway rimossi, numeri allineati`
File: `gestora-frontend/package.json`, `gestora-frontend/package-lock.json`,
`GestoraWebApi/Dockerfile`, `GestoraWebApi/Development/SeedSviluppo.cs`,
`RUNBOOK.md`, `CLAUDE.md`, `GestoraWebApi/CLAUDE.md`, `gestora-frontend/CLAUDE.md`,
`GestoraDocs/CONSEGNA_v1.1.md`.
File eliminati: `GestoraWebApi/railway.json`, `GestoraWebApi/.github/` (cartella vuota).

### Fase 10 — Documentazione allineata alla realtà ✅

**Fatto:**

1. **`RUNBOOK.md` riscritto per Azure + Neon.** §3 (reset produzione: Neon ha un indirizzo
   pubblico, `psql`/`pg_dump` diretti da locale, non serve più una sessione remota come Railway),
   §4 (sequenza backup → migration → push → verifica, aggiornata ai nomi Azure/Neon), §5
   (pubblicazione: la catena backend è in due passi automatici — GitHub Actions → Docker Hub →
   webhook Azure — non un solo passo come Railway; indirizzo di verifica aggiornato), §6 (backup
   e rotazione password su Neon, con il dettaglio del doppio underscore nelle variabili Azure),
   §8 (le trappole Railway sostituite con le quattro trovate durante la migrazione: doppio
   underscore, segnaposto dimenticati, `VITE_API_URL` Config non Secret, SCM Basic Auth per il
   webhook). **Aggiunta la sezione 9, Sicurezza**: la spiegazione in linguaggio semplice del
   dubbio di Fabio sul token JWT in `localStorage` (perché non è un bug), con la correzione fatta
   (`ClockSkew = TimeSpan.Zero` in `AuthenticationExtensions.cs`: il token scadeva a 65 minuti
   invece di 60, per la tolleranza di 5 minuti che .NET applica di default).
2. **I tre `CLAUDE.md`** aggiornati: stato, produzione (Vercel + Azure + Neon, non più Railway),
   tavolozza (non più "calda", ora la direzione Docker Hub della Fase 6), sezione "Barra
   superiore, non più sidebar" nel frontend, pattern per i filtri nell'URL (Fase 7), grafo degli
   stati della prenotazione con `NonPresentata` (già fatto in Fase 3), numeri dei test (già
   fatto in Fase 9).
3. **Il dubbio sul token** — chiuso per iscritto in `RUNBOOK.md` §9 (vedi sopra) e corretto il
   piccolo miglioramento indicato (`ClockSkew`). Verificato che il logout svuoti anche la cache
   di React Query: **già così** (`AppLayout.handleLogout` chiama `queryClient.clear()`).
4. **`BACKLOG.md`**: rimosse/chiuse con una riga di richiamo `CAP-001` (già chiusa in Fase 1),
   `DOC-001` (chiusa qui, vedi tabella sotto), `OPS-005` (chiusa in Fase 9). **`UI-001` non è
   stata chiusa**: resta aperta con lo stato reale (14/90 voci fatte). Aggiunta una voce nuova,
   `SEC-001`, per l'avviso di sicurezza su AutoMapper trovato in Fase 9. Aggiunta l'idea v2.0 dei
   cookie `HttpOnly`. Data aggiornata al 21/09/2026.
5. **`docs/archivio/STORICO_FASI.md`**: aggiunte «Fase 12 — Migrazione Azure/Neon» e «Fase 13 —
   Chiusura v1.1», vedi sotto.
6. **`AppuntiFix.txt`**: **non toccato** (è il file personale di Fabio). Tabella di corrispondenza
   riga → fase, qui sotto.
7. **Tracker Excel**: **non toccato**. Elenco delle righe da aggiungere al foglio *Fix e Bug*,
   più sotto.

**Ogni riga di `AppuntiFix.txt` → dove è finita:**

| Appunto (riassunto) | È diventato | Fase |
|---|---|---|
| SEPARARE FE E BE | Preparazione dei file per lo split, non lo split vero | 11 (in corso) |
| Dubbio sul token JWT in localStorage | Spiegazione scritta + `ClockSkew` corretto | 10 |
| Bug: utente senza ruolo resta bloccato in loop su "Accesso non autorizzato" | `paginaDiCasa()`, schermata dedicata con solo «Esci» | 2 |
| Ottimizzazione della leggibilità dei log | Formato di log + instradamento richieste, bug di un 409 loggato come 500 corretto | 5 |
| Prenotazione mai confermata resta "attiva" oltre la data | Nuovo stato "Non presentata", job notturno | 3 |
| Ordinare le view in un ordine coerente | Menu riordinato per flusso di lavoro (non alfabetico: interpretazione più utile — uso quotidiano, poi sala, poi amministrazione) | 2 |
| Dashboard troppo statica | Navigazione per giorno, aggiornamento automatico, "In arrivo", settimana con bande | 7 |
| Register: segnaposto "SOTTOCrea il tuo account" | Testo vero | 2 |
| Staff vede il pulsante "Configura fasce orarie" che non può usare | Pulsante solo per Admin | 2 |
| Vetrina pubblica poco raggiungibile da login/register | Link "Torna alla pagina del locale" | 2 |
| Tendina delle fasce poco leggibile (serve hover) | `color-scheme` + colori delle opzioni | 2 |
| Idea: turno accanto all'orario | `NumeroTurno` esposto dall'API, mostrato in tabella | 4 |
| Idea: giorno prima dell'orario nelle Fasce orarie | Colonne invertite, elenco ordinato | 4 |
| Idea: separatore per turno in tabella Prenotazioni | Fatto per **giorno** invece che per turno (la colonna Orario mostra già il turno, un secondo raggruppamento sarebbe stato ridondante) | 4 |
| Idea: coperti per tavolo | `NumeroPosti` esposto dall'API, mostrato in tabella | 4 |
| Idea: tetto dei coperti come vincolo vero | Lock `FOR UPDATE` sulla fascia | 1 |

**Righe da aggiungere al foglio *Fix e Bug* del tracker** (`TrackGestora_v2.xlsx`, non toccato —
elenco per chi lo aggiorna a mano):

| Sigla | Titolo | Stato |
|---|---|---|
| `CAP-001` | Tetto dei coperti protetto da lock sulla fascia | Chiuso 18/09 |
| `UI-001` | Verifica visiva del redesign «Turno» | Parziale — 14/90 voci, dettaglio in `verifica-redesign.md` |
| `DOC-001` | Formalizzazione appunti d'uso (`AppuntiFix.txt`) | Chiuso 21/09 |
| `OPS-005` | Devtools spostati in devDependencies | Chiuso 21/09 |
| `OPS-006` | Migrazione Railway → Azure/Neon | Chiuso 18/09 |
| `SEC-001` | Avviso di sicurezza AutoMapper 12.0.1 (High) | Aperto — decisione su licenza da prendere |

**Verifica:** backend 256 verdi, frontend 45 verdi (nessuna modifica di logica in questa fase,
solo documentazione + il fix `ClockSkew`, verificato con la suite completa).

**Commit:** `docs: runbook per Azure/Neon, CLAUDE.md, backlog e storico aggiornati alla v1.1`
File: `RUNBOOK.md`, `CLAUDE.md`, `GestoraWebApi/CLAUDE.md`, `gestora-frontend/CLAUDE.md`,
`GestoraWebApi/Extensions/AuthenticationExtensions.cs`, `BACKLOG.md`,
`docs/archivio/STORICO_FASI.md`, `GestoraDocs/CONSEGNA_v1.1.md`.

### Fase 11 — Separare FE/BE, reset database, consegna finale ⚠️ parziale

**11a — Preparazione per separare i repository.** Non ho creato repository né fatto push (vietato
dalla regola del progetto), solo preparato i file:
- `.gitignore` di `GestoraWebApi/` esteso con le regole per backup e credenziali (prima solo
  nella radice del monorepo).
- `README.md` nuovo per `GestoraWebApi/` (non esisteva) e riscritto per `gestora-frontend/` (era
  ancora il testo generico di scaffolding di Vite, mai personalizzato).
- Copie dei documenti trasversali (`CLAUDE.md` di radice, `BACKLOG.md`, `RUNBOOK.md`,
  `docs/archivio/`) dentro `GestoraWebApi/docs/progetto/` — copie, non spostamento: gli originali
  alla radice restano quelli veri finché il monorepo esiste.
- I tre workflow copiati nella cartella del progetto giusto, **con `paths:` e
  `working-directory` tolti** (la radice del nuovo repo sarà già quella cartella):
  `GestoraWebApi/.github/workflows/{ci-backend.yml, docker-publish.yml}`,
  `gestora-frontend/.github/workflows/ci-frontend.yml`. I workflow originali alla radice
  **restano** finché il monorepo esiste (si spostano/cancellano solo al momento vero dello split).
- `gestora-frontend/CLAUDE.md`: aggiunta una nota che rimanda a `docs/progetto/` nel repository
  del backend, con un segnaposto per il link GitHub (non esiste ancora, il repo non è stato
  creato).
- Procedura passo-passo per lo split vero e proprio scritta in `RUNBOOK.md` §10
  (`git subtree split`, creazione dei due repository, ricollegamento di Vercel, secret di GitHub
  Actions da ricreare, verifica finale, archiviazione — non cancellazione — del monorepo).

**11b — Reset dei database.**
- **Locale**: fatto. Non con `dotnet ef database drop` + `update` come indicato dal piano — quei
  due comandi sono **negati** dalle regole del progetto in questa sessione (`dotnet ef database
  update` è in blocco esplicito; `drop` chiede una conferma interattiva che questo ambiente non
  può dare). Ho usato l'equivalente già in uso per tutta la sessione,
  `dotnet run -- --seed-sviluppo`, che cancella e riscrive tutti i dati di dominio allo stesso
  modo (non ricrea lo schema da zero, ma lo schema non è cambiato: nessuna migration nuova in
  questa chiusura). Se serve davvero uno schema ricreato da zero — per esempio per verificare che
  le migration si applichino pulite su un database vuoto — quei due comandi restano da fare a
  mano da Fabio.
- **Neon (produzione)**: preparato `GestoraWebApi/Scripts/reset_dati_prova.sql` — svuota tutti i
  dati di dominio (`TRUNCATE ... RESTART IDENTITY CASCADE`) e cancella ogni utente che non ha il
  ruolo Admin, senza toccare le tabelle `QRTZ_*` o `__EFMigrationsHistory`. **Non eseguito** (è
  produzione, lo esegue Fabio). Comando: `psql "<connection string>" -f Scripts\reset_dati_prova.sql`,
  dopo un backup (`pg_dump`, RUNBOOK.md §6).

**11c — Questa consegna.** Le sezioni finali richieste (giro di test, sequenza di rilascio,
pulizie facoltative, bloccato/non fatto) sono qui sotto.

**Verifica:** backend 256 verdi, frontend 45 verdi (nessun codice toccato in questa fase, solo
file di preparazione e lo script SQL, non eseguito).

**Commit:** `docs: consegna v1.1 — preparazione split repo, script reset Neon`
File nuovi: `GestoraWebApi/README.md`, `gestora-frontend/README.md` (riscritto),
`GestoraWebApi/docs/progetto/*` (copie), `GestoraWebApi/.github/workflows/*`,
`gestora-frontend/.github/workflows/ci-frontend.yml`,
`GestoraWebApi/Scripts/reset_dati_prova.sql`, `RUNBOOK.md` (§10 nuova),
`gestora-frontend/CLAUDE.md`, `GestoraWebApi/.gitignore`, `GestoraDocs/CONSEGNA_v1.1.md`.

---

## Giro di test consigliato per Fabio

Sul portale locale (`localhost:5173`, backend `localhost:5099`, dati di sviluppo già caricati).
In ordine, ognuno con «cosa devi vedere»:

1. Apri `localhost:5173` senza account → vedi la vetrina con l'hero, il modulo di disponibilità,
   "Come funziona", "Le zone". Il pulsante blu grande scorre al modulo, non porta altrove.
2. Login → Accedi → `admin@gestora.local` / `Sviluppo1!` → arrivi alla Dashboard con la barra
   superiore in alto (non più il menu laterale).
3. Sulla Dashboard, clicca la freccia «→» accanto a «Oggi»: la data cambia, i numeri si
   aggiornano.
4. Clicca su una fascia della lista «Le fasce del giorno»: si apre Prenotazioni, già filtrata su
   quella data e quella fascia.
5. Se ci sono prenotazioni "In arrivo" in Dashboard, prova a cliccare «Conferma» su una: lo stato
   cambia, il numero "Ancora da confermare" si aggiorna senza ricaricare la pagina.
6. Vai su Prenotazioni → prova il filtro data e il filtro stato → l'elenco si aggiorna, l'URL nella
   barra degli indirizzi cambia con `?data=...&stato=...`.
7. Nella tabella Prenotazioni, controlla una riga con due tavoli uniti: i numeri dei tavoli sono
   separati da un punto, con i coperti fra parentesi (es. `9 (3) · 10 (3)`).
8. Sotto l'orario di una riga, controlla che compaia «1º turno» o «2º turno» quando ci sono più
   fasce quello stesso giorno.
9. Crea una nuova prenotazione (pulsante blu in alto a destra su Prenotazioni): scegli data,
   fascia, coperti, salva → compare nell'elenco.
10. Prova a creare una prenotazione che sfora il tetto della fascia → arriva un messaggio di
    errore chiaro, non un errore tecnico.
11. Vai su Tavoli: il riepilogo «I tavoli bastano a coprire il tetto?» in cima ora scorre dentro
    un riquadro con la sua barra, senza spingere il resto della pagina in basso.
12. Vai su Fasce orarie: le colonne sono Giorno-Orario-Tetto coperti-Stato (Giorno prima), l'elenco
    è ordinato per giorno della settimana partendo da lunedì.
13. Vai su Utenti (solo Admin): righe con nome, email, ruoli come testo, pulsante Modifica + «…».
14. Prova a togliere tutti i ruoli a un utente di prova, poi esci e rientra con quell'utente →
    vedi «Il tuo account non ha ancora un ruolo» con solo il pulsante «Esci», niente loop.
15. Cambia tema (icona sole/luna in alto a destra) → tutta l'app passa a scuro, il menu resta
    leggibile, le tendine dei form restano leggibili.
16. Riduci la finestra del browser a circa 400px (o usa la modalità telefono, F12): il menu
    diventa un'icona hamburger, aprendola vedi le voci con i gruppi «Sala» e «Amministrazione».
17. Spegni il backend (`Ctrl+C` nel terminale di `dotnet run`), ricarica Prenotazioni → riquadro
    rosso «Non riesco a caricare i dati» con pulsante «Riprova». Riaccendi il backend, premi
    «Riprova» → la tabella si popola senza ricaricare la pagina.
18. Crea una prenotazione per oggi e non confermarla, poi (da Admin) forza il job:
    `POST /api/Jobs/trigger/PrenotazioniJob` (da Swagger o da un client HTTP) → la prenotazione
    passa a «Non presentata» (in corsivo, grigio) — solo se la fascia scelta è già passata.
19. Prova ad accedere come Cliente (`cliente@gestora.local`) → vedi solo la voce Prenotazioni nel
    menu, senza i separatori di gruppo.
20. Prova ad annullare e poi eliminare una prenotazione «Annullata» → il dialogo di conferma dice
    testi diversi per le due azioni («Annulla prenotazione» vs «Elimina definitivamente»).

## Rilascio

Sequenza per Fabio, quando deciderà di pubblicare v1.1.0:

1. `git log --oneline main..dev` per vedere l'elenco dei commit di questa chiusura.
2. Merge `dev` → `main` (da Visual Studio).
3. Tag `v1.1.0` sulla punta di `main`, push del tag.
4. Attendere la catena Actions → Docker Hub → webhook → Azure (qualche minuto, vedi
   `RUNBOOK.md` §5).
5. `curl https://gestora-api-emdvdqegg7g8gmaq.canadacentral-01.azurewebsites.net/health` →
   `Healthy`.
6. Vercel ripubblica da solo il frontend (push su `main` che tocca `gestora-frontend/**`).
7. Login con i tre ruoli in produzione.
8. `POST /api/Jobs/trigger/PrenotazioniJob` (Admin) per verificare che il job giri anche lì.

## Pulizie facoltative (restano a Fabio)

- `OPS-002` — cancellare `Gestora_BACKUP_20260903\` (445 MB, non tracciata da Git, verificato con
  `du -sh` in questa sessione).
- `OPS-003` — `git gc --prune=now` a PC appena riavviato.
- `OPS-004` — `git push --force origin refs/tags/v1.0.0` per riallineare il tag.
- **Separazione dei repository** (Fase 11a): procedura completa in `RUNBOOK.md` §10, file di
  preparazione già pronti.
- `backup_LogActivities_20260904.csv` nella radice (non tracciato, ma contiene dati veri): da
  cancellare.
- `SEC-001` — decidere se aggiornare AutoMapper (12.0.1 → 16.2.0, cambio di licenza da valutare).

## Bloccato / non fatto

Onestà prima di tutto: elenco di ciò che questa chiusura **non** ha completato.

- **`UI-001`**: verifica visiva completa. Fatte 14 voci su circa 90 (le più importanti, con un
  difetto reale trovato e corretto). Restano soprattutto i controlli a 768px/375px e il tocco
  vero col dito, non simulabile da uno strumento automatico. Dettaglio in
  `GestoraDocs/verifica-redesign.md`.
- **D2** (animazione d'ingresso della Dashboard) e **X4** (evidenziazione delle tendine native nei
  dialoghi): non verificabili con uno screenshot, per due motivi diversi — un movimento non si
  fotografa, un menu nativo lo disegna il sistema operativo. Il codice non è stato toccato in
  questa chiusura, quindi non dovrebbe essere cambiato, ma nessuno lo ha guardato di persona.
  Verifica manuale in carico a Fabio, riferimenti disponibili in `verifica-redesign.md`.
- **`SEC-001`**: avviso di sicurezza su AutoMapper, non risolto — decisione sulla licenza da
  prendere prima di aggiornare.
- **Reset locale con schema ricreato da zero** (`dotnet ef database drop`/`update`): non eseguibile
  in questa sessione (comandi negati dalle regole del progetto). Fatto l'equivalente
  (`--seed-sviluppo`), che pulisce i dati ma non ricrea lo schema. Da fare a mano se serve
  davvero verificare le migration su un database vuoto.
- **Reset di Neon**: script pronto (`Scripts/reset_dati_prova.sql`), **non eseguito** — è
  produzione, tocca a Fabio.
- **Separazione vera dei repository**: solo preparazione (file pronti). La procedura in
  `RUNBOOK.md` §10 non è stata eseguita — crea repository, fa push, tocca pannelli di produzione
  (Vercel, GitHub), tutte cose fuori dal perimetro di questa sessione.
- **Nessun commit, push, merge, tag**: come da regola del progetto in questa sessione, ogni fase
  ha il proprio messaggio di commit pronto qui sopra, ma l'esecuzione (`git add`, `git commit`,
  `git status --untracked-files=all` per contare i file) resta a Fabio.

## Verifica finale — numeri

- `git branch --show-current` → `dev`.
- Test backend: **256 verdi** (erano 239 all'inizio di questa chiusura).
- Test frontend: **45 verdi** (erano 35).
- `npm run build`, `npm run lint`, `node scripts/contrasto.mjs`: puliti.
- `npm audit`: 0 vulnerabilità. `dotnet list package --vulnerable --include-transitive`: 1 trovata
  (AutoMapper, `SEC-001`, non risolta per decisione — vedi sopra).
- Nessun commit su `main`, nessun push, nessun tag: solo modifiche non salvate su `dev`, in
  locale, pronte per essere riviste e committate da Fabio.

**Conteggio finale:**
Fasi completate per intero: 0b, 1, 2, 3, 4, 5, 6, 7, 9, 10 (10 fasi).
Fasi con riserva (parziali, documentato cosa manca): 8, 11 (2 fasi).
Fasi bloccate: nessuna.
Test backend: 256 verdi. Test frontend: 45 verdi.

