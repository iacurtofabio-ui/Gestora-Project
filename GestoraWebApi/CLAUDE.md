# GestoraWebApi — Backend

Questo file vale solo quando si lavora dentro `GestoraWebApi/`. Per stato sessione, decisioni di
prodotto e regole del progetto vedi il `CLAUDE.md` alla radice del repo — resta valido sempre.

## Stack

ASP.NET Core 9, C#, Entity Framework Core 9 + Npgsql (PostgreSQL), ASP.NET Identity + JWT
Bearer (3 ruoli: Admin, Staff, Cliente), FluentValidation 11, AutoMapper, Serilog,
IMemoryCache (30 min, invalidazione su write), Quartz.NET 3.15 (persistenza su Postgres,
tabelle `QRTZ_*` — **non create automaticamente**, va eseguito `Scripts/quartz_postgres.sql`).

## Verifica prima di dire "fatto"

```
dotnet build      # deve completare senza errori
dotnet test       # confronta "Passed" col numero atteso in TrackGestora_v2.xlsx
```
Se hai toccato un endpoint, verifica anche a mano su Swagger (`/swagger`) prima di considerarlo
concluso, non solo via test unitari.

## Architettura

Pattern: Controller → Service → Repository (layered).

- `Controllers/` — 9 controller, un endpoint REST per azione
- `Services/{Area}/` — logica di dominio + `DTOs/` per area
- `Repositories/{Area}/` — accesso dati via EF Core
- `Infrastructure/Middleware/ExceptionMiddleware.cs` — mapping eccezione → status code
  centralizzato: `NotFoundException`/`KeyNotFoundException` → 404, `ValidationException`/
  `ArgumentException` → 400 (con `errors[]` per campo), `ForbiddenException` → 403,
  `ConflictException` → 409 (**unica** eccezione che genera 409: le regole di dominio che
  rifiutano un'operazione sollevano questa), resto → 500.
  **Non lanciare risposte HTTP dai service** — solo eccezioni tipizzate.
  ⚠️ `InvalidOperationException` **non è mappata apposta**: se arriva al middleware è un bug
  interno vero, deve restare un 500 (non "correggerla" aggiungendola alla mappa).
- `Migrations/` — EF Core. **Applicate solo da Fabio** (decisione 2 di radice): Claude prepara
  la migration con `dotnet ef migrations add`, non esegue mai `dotnet ef database update`.

## Endpoint — trappole, non un catalogo

Per l'elenco aggiornato usa il comando `/verifica-endpoint` o `graphify query`, non affidarti a
liste scritte a mano: invecchiano. Quello che il codice non dice da solo:

- La rotta base è sempre `api/[nome della classe controller]` — es. `AuthenticationUserController`
  → `/api/AuthenticationUser/...` (non `/api/Auth/...`), `FasceOrarieController` →
  `/api/FasceOrarie/...` (non `/api/FasciaOraria/...`, vecchia documentazione).
- **`GET /health`** sta fuori dai controller (registrato in `Program.cs`), pubblico. È il
  Healthcheck Path di Railway: se non risponde, il deploy fallisce e resta online la versione
  precedente.
- **`check-disponibilita`** (Prenotazione) e **`Setup`** (`GET stato`, `POST admin`) sono pubblici,
  senza auth — necessario per la pagina pubblica e il primo avvio.
- **`JobsController`** (`POST trigger/{jobName}`, solo Admin) è **attivo anche in produzione**,
  nessun filtro per ambiente: forza un job Quartz già registrato senza aspettare il cron.
- **`update-prenotazione`/`annulla-prenotazione`**: aperte anche al Cliente, ma solo sulla propria
  prenotazione e solo fino a 2 ore prima dell'orario (`PrenotazioniService.GuardCutoffAsync`,
  costante `CutoffOreClienteSelfService`); oltre la soglia 409, deve contattare il locale.

## RBAC — perimetro ruoli (vedi `Auth/Roles.cs`)

- **Admin**: tutti i permessi, nessuna eccezione.
- **Staff**: lettura completa su Zone/Postazioni/FasceOrarie/Prenotazioni. Scrittura solo su
  Prenotazioni (crea, modifica, conferma, completa, annulla) — **non** elimina, **non** scrive su
  Zone/Postazioni/FasceOrarie (resta solo Admin).
- **Cliente**: crea-prenotazione + lettura propria + lettura di supporto (zone/postazioni/fasce
  attive). Può modificare/annullare solo entro il cutoff di 2 ore (vedi sopra).

Un utente **può avere più ruoli contemporaneamente** (many-to-many `UserRoles`) — caso d'uso
legittimo, non un'anomalia. Il JWT serializza il claim `role` come stringa se l'utente ha un solo
ruolo, come array se ne ha più di uno — il frontend normalizza sempre a array, vedi
`gestora-frontend/CLAUDE.md`.

## Assegnazione tavoli

`Services/PostazioneAssignment/AssegnazioneTavoli.cs` — motore **puro e statico**, nessuna
dipendenza da repository o DbContext, testato direttamente (`AssegnazioneTavoliTests`, 15 test).
`PostazioneAssignmentService` legge solo i dati (tavoli attivi, tavoli occupati) e delega al
motore. **Non rimettere logica di scelta dentro il service**: rendeva l'algoritmo precedente non
testabile.

Regole (decisioni di prodotto, vedi `CLAUDE.md` di radice §4 — non riaprirle):
- capienza di un'unione = somma delle capienze, **+2 (`BonusTestate`) solo se composta
  esclusivamente da tavoli da 2 posti** e almeno 2 tavoli; ogni altra combinazione = somma semplice
- si uniscono al massimo **4 tavoli** (`MaxTavoliPerUnione`), sempre **della stessa zona**
- vince la combinazione con **meno posti sprecati**; a parità, quella con meno tavoli
- nessun vincolo sulle capienze ammesse (qualsiasi numero da 1 in su)

Le combinazioni si generano sulle **capienze distinte**, non sui singoli tavoli: il costo non
cresce col numero di tavoli in sala.

`DisponibilitaService` chiama lo stesso motore (`TrovaMigliorCombinazione`) usato
dall'assegnazione reale — non un algoritmo parallelo. Basa i posti residui sul tetto della fascia
(`MaxCoperti`, decisione 8), esclude tavoli in zone disattivate.

## Orologio unico

`Common/IClock` (`SystemClock`, singleton): `UtcNow`, `NowInRome`, `TodayInRome`. Database e
logica interna in UTC; conversione a `Europe/Rome` solo al confine. **Non reintrodurre
`DateTime.Now`/`DateTime.Today` privati**: iniettare `IClock`. Nei test: `TestClock` (istante
fisso).

## Note tecniche da tenere a mente

- HTTPS: Railway termina HTTPS a livello proxy → `UseHttpsRedirection` resta commentato in
  `Program.cs`, **non riattivarlo** in produzione.
- CORS: origin letti da `AllowedOrigins` in appsettings/env var, mai hardcoded.
- **Enum su DB: non tutti mappati allo stesso modo** (`Context/GestoraContext.cs`).
  `Prenotazione.Stato` è **stringa** (`'Completata'` in colonna); `FasciaOraria.GiornoSettimana`
  è **intero**. Attenzione scrivendo SQL a mano (query dirette, seed, fix su Railway): usare il
  valore giusto per la colonna giusta.
- **Transazioni**: `Common/IEsecutoreTransazione` avvolge scrittura + audit log in un'unica
  operazione atomica (usato da `ZonaService`, `PostazioneService`, `FasciaOrariaService`).
  `PrenotazioniService` ha il proprio `EseguiInTransazioneAsync` (traduce anche la violazione
  dell'unique index sullo slot in 409). **La cache si invalida dopo il commit**, mai dentro il
  blocco.
- **Indirizzo IP reale** — `Common/IndirizzoClient`. **Non usare `Connection.RemoteIpAddress`
  direttamente**: dietro il proxy è un indirizzo di rete interna, uguale per chiunque. La catena
  ha due livelli di proxy: l'helper scarta l'ultimo anello e prende quello prima. Non
  `UseForwardedHeaders` (prende l'anello sbagliato, cioè il proxy). Per rimisurare la catena:
  `GET /api/LogActivity/diagnostica-inoltro` (Admin), con il middleware disattivato (altrimenti
  legge un header già consumato).
- **Storico utenti**: FK `Prenotazioni → Utenti` è `Restrict`, non `Cascade`. Un utente con
  prenotazioni **non si elimina** (409): lo storico regge i conteggi.
- **Quartz e repliche**: scheduler **non** in cluster mode. Con una sola istanza va bene; prima di
  aggiungere repliche va abilitato `store.UseClustering()` (dettaglio commentato in `Program.cs`).
- **Liste vuote**: una collezione vuota è sempre `200 []`, mai 404 (il 404 resta per la singola
  entità non trovata).
- **Paginazione**: `Page`/`PageSize` fuori range vengono riportati dentro i limiti, non generano
  errore. L'ordinamento delle liste paginate deve sempre essere **totale**
  (`.OrderBy(...).ThenBy(x => x.Id)`), altrimenti pagine duplicate/perse.
- **Tavoli e prenotazioni future**: `HasPrenotazioniFutureAsync` guarda solo da oggi in avanti.
  Non reintrodurre controlli sull'intero storico: renderebbe un tavolo immutabile per sempre dopo
  la prima prenotazione.
- **Cache**: chiavi in `Common/CacheKeys.cs` — invalidare **tutte** le chiavi derivate (es.
  `FascePerGiorno+giorno`), non solo quella base.
- **Avvio**: `Program.cs` valida la configurazione **prima** di registrare i servizi (fail-fast):
  se manca `ConnectionStrings:DefaultConnection`/`JwtSettings:Secret`, o il segreto è più corto di
  32 caratteri, l'app si ferma con un messaggio esplicito. **Non rimuovere quei controlli.**
- **Connessione DB**: `EnableRetryOnFailure` attivo (5 tentativi/10s). Le transazioni esplicite
  vanno dentro `CreateExecutionStrategy().ExecuteAsync(...)` — usare l'helper
  `EseguiInTransazioneAsync`, non aprire transazioni a mano.
- **Concorrenza sul tavolo**: unique index **pieno** `UX_PrenotazionePostazione_Slot` su
  `(PostazioneId, DataPrenotazione, FasciaOrariaId)`. Chi scrive una riga join deve valorizzare
  entrambi i campi (passare da `CreaRigaPostazione`); annullare una prenotazione cancella le sue
  righe join.
- **Errori del database**: `DbExceptionTranslator` riconosce il codice Postgres `23505`
  (violazione unicità). Il provider InMemory **non** applica gli unique index: nei test la
  violazione va simulata a mano.
- **Log**: in produzione **solo console** (filesystem del container effimero). Il sink su file
  resta in `appsettings.Development.json` (versionato, senza segreti) — la configurazione .NET
  sovrascrive gli array **per posizione**, quindi il sink Console va riconfermato all'indice 0.
- **Segreti**: connection string e JWT Secret di sviluppo in **User Secrets**
  (`dotnet user-secrets`), non in `appsettings.Development.json` (solo placeholder vuoti).
  Percorso e comandi in `RUNBOOK.md`. In produzione: solo env var Railway.

## Test

`GestoraWebApi.Tests/Services/` — xUnit + Moq, Arrange/Act/Assert. Un file per service, più il
motore puro, i job, i validator, il mapping, il repository, il modello. **239 test totali.**

- `PrenotazioniServiceTests` configura l'InMemory con
  `ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))` — senza questa
  riga tutti i test della classe falliscono (l'InMemory non supporta transazioni).
- Orologio nei test: `TestClock` (istante fisso).
- Nessun test su `AuthenticationUserController`: la logica di auth non è ancora estratta in un
  service dedicato (vedi `BACKLOG.md`).
- Per mockare `IQueryable<T>` dai repository: pacchetto `MockQueryable.Moq` (7.0.3, unica
  versione compatibile con net9.0) — pattern `lista.AsQueryable().BuildMockDbSet().Object`.
