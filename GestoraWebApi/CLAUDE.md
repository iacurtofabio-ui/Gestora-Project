# GestoraWebApi — Backend

Questo file vale solo quando si lavora dentro `GestoraWebApi/`. Per stato sessione, iter di
progetto e protocollo tracker vedi il `CLAUDE.md` alla radice del repo — resta valido sempre.

## Stack

ASP.NET Core 9, C#, Entity Framework Core 9 + Npgsql (PostgreSQL), ASP.NET Identity + JWT
Bearer (3 ruoli: Admin, Staff, Cliente), FluentValidation 11, AutoMapper, Serilog,
IMemoryCache (30 min, invalidazione su write), Quartz.NET 3.15 (persistenza su Postgres,
tabelle `QRTZ_*` — **non create automaticamente**, va eseguito `Scripts/quartz_postgres.sql`).

## Architettura

Pattern: Controller → Service → Repository (layered).

- `Controllers/` — 6 controller, un endpoint REST per azione
- `Services/{Area}/` — logica di dominio + `DTOs/` per area
- `Repositories/{Area}/` — accesso dati via EF Core
- `Infrastructure/Middleware/ExceptionMiddleware.cs` — mapping eccezione → status code
  centralizzato: `NotFoundException`/`KeyNotFoundException` → 404, `ValidationException` → 400
  con `errors[]` per campo, `ArgumentException` → 400, `InvalidOperationException` → 409,
  resto → 500. **Non lanciare risposte HTTP dai service** — solo eccezioni tipizzate.
- `Migrations/` — EF Core, applicate manualmente in locale (`dotnet ef database update`)

## Endpoint — riferimento reale

⚠️ Il controller si chiama `FasceOrarieController` — la route base è `/api/FasceOrarie/`,
**non** `/api/FasciaOraria/` come riportato in vecchia documentazione. Il file è
`Controllers/FasceOrarieController.cs` (rinominato per allinearsi alla classe, NAMING-001-residuo
risolto il 27/08/2026).

Formato date: `yyyy-MM-dd`. Header auth: `Authorization: Bearer {token}`.
Formato errori: `{ statusCode, message, errors: [{field, error}] }`.

⚠️ La rotta base è `api/[nome della classe controller]`. Per l'autenticazione la classe è
`AuthenticationUserController`, quindi gli endpoint stanno sotto **`/api/AuthenticationUser/...`**
(es. `/api/AuthenticationUser/login`), non `/api/Auth/...`.

Fuori dai controller: **`GET /health`** — health check pubblico e senza autenticazione, registrato
in `Program.cs`. È il Healthcheck Path configurato su Railway: se non risponde, il deploy viene
marcato come fallito e resta online la versione precedente.

`JobsController` (`POST trigger/{jobName}`, solo Admin) — forza l'esecuzione immediata di un job
Quartz già registrato (`PrenotazioniJob`, `PrenotazioniCleanupJob`), senza aspettare il cron.
Utile per verificare un flusso automatizzato a comando o per rieseguirlo manualmente in caso di
necessità operativa. Aggiunto il 27/08/2026, presente solo in locale — valutare se portarlo anche
in produzione.

Auth: `POST register`, `POST login`, `POST assign-role`, `DELETE remove-role`, `GET get-users`,
`GET get-user/{id}`, `PUT update-user/{id}`, `DELETE delete-user/{id}`, `POST reset-password/{id}`.

Setup (REV-007, `SetupController`, pubblico): `GET stato` — dice solo se esiste già un Admin —
e `POST admin` — crea il primo amministratore dell'installazione, aperto finché quell'Admin non
esiste, poi 409 per sempre. Sostituisce `POST seed-admin`, **rimosso**. Serializzato da un
`SemaphoreSlim` statico: basta per un'app a istanza singola, non per più repliche.

Dashboard: `GET giornaliera?data=`, `GET settimanale?dataInizio=`.

LogActivity (REV-037, Fase 7, **solo Admin**): `GET get-log` — lettura paginata dell'audit trail,
dal piu' recente. Filtri opzionali via `LogActivityQueryParams`: `userId`, `da`/`a` (istanti UTC,
estremi inclusi), `azione` (ricerca libera nel testo), `page`, `pageSize` (max 200). Restituisce
un `PagedResult<LogActivityDTO>`; il `userName` puo' essere null, perche' l'audit trail
sopravvive volutamente all'utente. Nessuna interfaccia frontend: e' un endpoint da usare con
Postman o da agganciare in una fase successiva.

Zona: `GET get-zone-attive`, `GET get-all-zone`, `GET get-zona/{id}`, `POST crea-zona`,
`PUT update-zona`, `PATCH update-stato/{id}?attiva=`, `DELETE delete-zona/{id}`.

Postazione: `GET get-postazioni-attive`, `GET get-postazioni-disponibili`,
`GET get-postazioni-per-zona`, `GET get-postazione-id`, `GET riepilogo-sala` (Admin+Staff,
quadro d'insieme sala — decisione 9), `POST crea-postazione`, `PUT update-postazione`,
`PUT associa-postazione-a-zona`, `DELETE delete-postazione`.

FasceOrarie: `GET fasce-attive`, `GET get-all-fasce` (Admin+Staff), `GET fasce-per-giorno?giorno={0-6}`,
`GET fasce-disponibili?fasciaId=&data=`, `POST crea-fascia`, `PUT update-fascia`,
`PATCH update-stato/{id}?attiva=`, `DELETE delete-fascia`.

Prenotazione: `POST crea-prenotazione`, `POST check-disponibilita` (pubblico, no auth),
`GET get-prenotazione?id=` (Admin/Staff su tutte; Cliente solo la propria — REV-034),
`GET get-all-prenotazioni` (filtri opzionali via
`PrenotazioniQueryParams`, paginato), `GET get-mie-prenotazioni` (solo Cliente, filtra su
UserId dal JWT), `GET get-prenotazioni-by-data?data=`, `PUT update-prenotazione`,
`DELETE delete-prenotazione` (solo Admin), `PATCH conferma-prenotazione?id=` (Admin+Staff),
`PATCH completa-prenotazione?id=` (Admin+Staff), `PATCH annulla-prenotazione?id=`.

`update-prenotazione` e `annulla-prenotazione` sono di nuovo aperte al Cliente (RBAC-002
risolto il 27/08/2026): Admin/Staff senza limiti, il Cliente solo sulla propria prenotazione e
solo fino a 2 ore prima dell'orario prenotato (`PrenotazioniService.GuardCutoffAsync`, costante
`CutoffOreClienteSelfService`); oltre la soglia l'azione è bloccata del tutto (409, nessuna
approvazione Staff) — deve contattare il locale.

## RBAC — perimetro ruoli (allineato 13/08/2026, vedi `Auth/Roles.cs`)

- **Admin**: tutti i permessi, nessuna eccezione.
- **Staff**: lettura completa su Zone/Postazioni/FasceOrarie/Prenotazioni (incluso `get-all-fasce`,
  `get-all-zone`, dettagli). Scrittura solo su Prenotazioni: crea, modifica, conferma, completa,
  annulla — **non** elimina (`delete-prenotazione` solo Admin) e **non** scrive su
  Zone/Postazioni/FasceOrarie (resta solo Admin).
- **Cliente**: crea-prenotazione + lettura propria (`get-mie-prenotazioni`) e di supporto alla
  prenotazione (zone/postazioni/fasce attive). Può modificare/annullare una propria prenotazione
  solo fino a 2 ore prima dell'orario prenotato (RBAC-002 risolto il 27/08/2026, vedi sezione
  Endpoint sopra) — oltre la soglia deve passare da Staff/Admin.

Un utente **può avere più ruoli contemporaneamente** (many-to-many `UserRoles`) — è un caso
d'uso legittimo, non un'anomalia (es. account che gestisce il locale ma prenota anche per sé
come Cliente). Il JWT serializza il claim `role` come stringa se l'utente ha un solo ruolo, come
array se ne ha più di uno — il frontend deve normalizzare sempre a array, vedi
`gestora-frontend/CLAUDE.md`.

## Audit trail (AUDIT-001 risolto 27/08/2026)

`ILogActivityService`/`Logging` (tabella dedicata) registra userId/azione/IP. Era già usato in
`AuthenticationUserController`; esteso a tutte le scritture di `ZonaService`, `PostazioneService`,
`FasciaOrariaService` (oltre a `PrenotazioniService`, che lo aveva già). Ogni service ha il
proprio `IHttpContextAccessor` + helper privati `GetAuthenticatedUserId()`/`GetIpAddress()` —
pattern copiato da `PrenotazioniService`, non centralizzato in un middleware.

## Flussi automatizzati (Quartz.NET)

Due job in `Background/`, registrati in `Program.cs` con persistenza su Postgres (`QRTZ_*`):
- **`PrenotazioniJob`** (cron `0 00 2 * * ?`, ogni notte alle 2:00) — completa automaticamente le
  prenotazioni `InCorso` la cui fascia oraria è già passata.
- **`PrenotazioniCleanupJob`** (cron `0 30 2 * * ?`, ogni notte alle 2:30, 30 min dopo il primo di
  proposito) — elimina fisicamente (hard delete) le prenotazioni `Completata` con data ≤ oggi−6
  mesi.

Entrambi verificati manualmente il 27/08/2026 tramite `JobsController` (vedi sopra) invece che
aspettando il cron/il cutoff reale — pattern da riusare per testare qualunque job futuro senza
attese.

Nessun test unitario copre oggi `AutomaticCompletPrenotazioniAsync`/`AutomaticDeletePrenotazioniAsync`.

## Assegnazione tavoli (riscritta 31/08/2026, checkpoint 2b)

`Services/PostazioneAssignment/AssegnazioneTavoli.cs` — motore **puro e statico**, nessuna
dipendenza da repository o DbContext: tutta la logica di scelta dei tavoli vive qui ed è testata
direttamente (`PostazioneAssignmentServiceTests`, 15 test). `PostazioneAssignmentService` resta
il solo responsabile di leggere i dati (tavoli attivi, tavoli già occupati nella fascia) e poi
delega al motore. **Non rimettere logica di scelta dentro il service**: è proprio ciò che rendeva
l'algoritmo precedente non testabile.

Regole (decisioni di prodotto, vedi `ROADMAP_REVISIONE.md` — non riaprirle):
- capienza di un'unione = somma delle capienze, **+2 (`BonusTestate`) solo se l'unione è composta
  esclusivamente da tavoli da 2 posti** e ha almeno 2 tavoli; ogni altra combinazione = somma
  semplice
- si uniscono al massimo **4 tavoli** (`MaxTavoliPerUnione`) e sempre **della stessa zona**
- vince la combinazione con **meno posti sprecati**; a parità, quella con meno tavoli. Tavolo
  singolo e unioni sono valutati insieme
- nessun vincolo sulle capienze ammesse (il vecchio 2/4/8 è stato rimosso dai validator)

Le combinazioni sono generate sulle **capienze distinte**, non sui singoli tavoli (due tavoli di
pari capienza sono intercambiabili): il costo non cresce col numero di tavoli in sala. I tavoli
concreti si scelgono solo sulla combinazione vincente.

`DistribuisciCoperti` riparte i coperti sui tavoli assegnati e alimenta
`PrenotazionePostazione.NumeroPosti` (REV-001: il campo esisteva da sempre ma restava 0). La somma
dei posti distribuiti è **sempre** pari ai coperti richiesti — è la proprietà su cui si appoggia
il calcolo della disponibilità.

`DisponibilitaService` (checkpoint 2c, 01/09/2026) chiama direttamente
`AssegnazioneTavoli.TrovaMigliorCombinazione` — stesso motore dell'assegnazione reale. Basa i
posti residui sul tetto della fascia (`MaxCoperti`, decisione 8), esclude tavoli in zone
disattivate (REV-024, come `PostazioneAssignmentService`) e restituisce un `Messaggio` che
distingue "tetto esaurito" da "tetto libero ma tavoli fisici insufficienti". Il vecchio wrapper
`TrovaCombinazioniDisponibili` è stato rimosso.

## Orologio unico (REV-016 / REV-092, checkpoint 2c)

`Common/IClock` (`SystemClock`, singleton): `UtcNow`, `NowInRome`, `TodayInRome`. Il database e la
logica interna lavorano in UTC; la conversione a `Europe/Rome` avviene solo al confine —
`PrenotazioniService` (cutoff, completamento job, cleanup), `DashboardService` /
`DashboardController` (data "oggi"), `PrenotazioneCreateDTOValidator` (soglia data passata). Non
reintrodurre `DateTime.Now`/`DateTime.Today`/`GetNowInRome()` privati: iniettare `IClock`. Nei
test usare `TestClock` (istante fisso).

## Note tecniche da tenere a mente

- HTTPS: Railway termina HTTPS a livello proxy → `UseHttpsRedirection` resta commentato in
  `Program.cs`, non riattivarlo in produzione.
- CORS: origin letti da `AllowedOrigins` in appsettings/env var, mai hardcoded.
- Enum su DB: **non tutti gli enum sono mappati allo stesso modo** (`Context/GestoraContext.cs`).
  `Prenotazione.Stato` è salvato come **stringa** (`.HasConversion<string>()`, es. `'Completata'`
  in colonna), mentre `FasciaOraria.GiornoSettimana` è salvato come **intero**
  (`.HasConversion<int>()`). Attenzione se si scrive SQL a mano (query dirette, seed, fix
  manuali su Railway): usare il valore giusto per la colonna giusta, non assumere che tutti gli
  enum del progetto seguano la stessa convenzione.
- **Transazioni (REV-032, Fase 7)**: `Common/IEsecutoreTransazione` avvolge scrittura + audit log
  in una sola operazione atomica. Lo usano `ZonaService`, `PostazioneService` e
  `FasciaOrariaService` (4 punti ciascuno). `PrenotazioniService` **non** lo usa: ha il proprio
  `EseguiInTransazioneAsync`, che in piu' traduce la violazione dell'unique index sullo slot in
  un 409. Regola: la cache si invalida **dopo** il commit, mai dentro il blocco. Nei test si usa
  `EsecutoreTransazioneFinto`, che con `Esegui = false` permette di dimostrare cosa sta dentro il
  blocco atomico (quello che non parte, era dentro).
- **Indirizzo IP reale (REV-029, Fase 7)**: `UseForwardedHeaders` e' registrato **per primo** in
  `Program.cs`, con `KnownNetworks`/`KnownProxies` svuotati (il proxy della piattaforma non e' su
  loopback) e `ForwardLimit = 1`. Quest'ultimo non e' un dettaglio: prende solo l'ultimo anello
  della catena `X-Forwarded-For`, quello scritto dal proxy, quindi un client non puo' falsificare
  l'header. Oltre all'audit trail, il fix rimette in sesto il **rate limit del login**, che
  partiziona su `RemoteIpAddress`: prima era di fatto un limite globale di 5 tentativi al minuto
  per tutta l'applicazione.
- **Storico e utenti (REV-038, Fase 7)**: la FK `Prenotazioni → Utenti` e' `Restrict`, non piu'
  `Cascade`. Un utente con prenotazioni **non si elimina**: `DELETE delete-user/{id}` risponde
  409 con un messaggio esplicito. E' voluto: lo storico regge i conteggi di coperti e presenze.
- **Quartz e repliche (REV-028)**: lo scheduler **non** e' in cluster mode. Con una sola istanza
  va bene; prima di aggiungere repliche va abilitato `store.UseClustering()`, altrimenti ogni job
  parte su ogni istanza — innocuo per `PrenotazioniJob`, una corsa fra DELETE per
  `PrenotazioniCleanupJob`. Il dettaglio e' commentato in `Program.cs`.
- **Liste vuote (REV-031)**: una collezione vuota si restituisce come `200 []`, mai 404. Il 404
  resta per la singola entita' non trovata.
- **Paginazione (REV-019, REV-020)**: `Page` e `PageSize` fuori range vengono riportati dentro i
  limiti, non generano errore. L'ordinamento delle liste paginate deve sempre essere **totale**
  (`.OrderBy(...).ThenBy(x => x.Id)`): senza un criterio che spezzi le parita', il database non
  garantisce l'ordine fra righe di pari chiave e navigando le pagine si vedono duplicati e si
  perdono righe.
- **Tavoli e prenotazioni future (REV-099)**: `IPostazioneRepository.HasPrenotazioniFutureAsync`
  guarda solo da oggi in avanti. Non reintrodurre controlli sull'intero storico: rendevano un
  tavolo immutabile per sempre dopo la sua prima prenotazione.
- Cache: chiavi in `Common/CacheKeys.cs` — attenzione a invalidare **tutte** le chiavi
  derivate (es. `FascePerGiorno+giorno`), non solo quella base. `FasciaOrariaService` e
  `PostazioneService` lo fanno correttamente (CACHE-001 risolto 13/08/2026); se si aggiungono
  nuove chiavi derivate altrove, verificare lo stesso pattern.
- Avvio: `Program.cs` valida la configurazione **prima** di registrare i servizi (fail-fast) — se
  `ConnectionStrings:DefaultConnection` o `JwtSettings:Secret` mancano, o se il segreto è più corto
  di 32 caratteri (256 bit, minimo per HMAC-SHA256), l'app si ferma con un messaggio esplicito.
  Non rimuovere quei controlli: senza, un errore di configurazione emerge molto più tardi come
  eccezione opaca del driver.
- Connessione DB: `EnableRetryOnFailure` attivo (5 tentativi / 10s) perché la rete privata tra
  container non è raggiungibile nei primi secondi. Con una strategia di retry le transazioni
  esplicite vanno eseguite dentro `CreateExecutionStrategy().ExecuteAsync(...)`: dal 02/09/2026
  ce ne sono tre (`AddAsync`, `UpdateAsync`, `AnnullaPrenotazioneAsync` di `PrenotazioniService`),
  tutte incapsulate nell'helper privato `EseguiInTransazioneAsync` — usare quello, non aprire
  transazioni a mano.
- Concorrenza sul tavolo (REV-003, Fase 3): la riga di `PrenotazioniPostazioni` porta una copia
  denormalizzata di `DataPrenotazione` + `FasciaOrariaId`, e l'unique index **pieno**
  `UX_PrenotazionePostazione_Slot` su `(PostazioneId, DataPrenotazione, FasciaOrariaId)` rende
  impossibile assegnare due volte lo stesso tavolo nello stesso slot. Due conseguenze da non
  dimenticare: **chi scrive una riga join deve valorizzare anche quei due campi** (si passa da
  `CreaRigaPostazione`), e **annullare una prenotazione cancella le sue righe join** — senza
  filtro `WHERE` sull'indice, righe di annullate continuerebbero a occupare lo slot.
- Errori del database: `Infrastructure/Exceptions/DbExceptionTranslator` riconosce il codice
  Postgres `23505` (violazione di unicità) e, opzionalmente, il nome del constraint. La
  traduzione è isolata lì per poter essere testata senza database: il provider InMemory **non**
  applica gli unique index, quindi la violazione va simulata (`PostgresException` costruita a
  mano). Corollario: un test di concorrenza vero richiederebbe Postgres reale — scelta del
  01/09/2026, non è stato introdotto.
- Codici di errore (REV-026, 02/09/2026): il **409 è solo `ConflictException`**. Le regole di
  dominio che rifiutano un'operazione sollevano quella; `InvalidOperationException` non è più
  mappata e, se affiora, è un bug interno → 500 con messaggio generico. `NotFoundException` →
  404, `ValidationException`/`ArgumentException` → 400.
- Log: in produzione **solo console** (`appsettings.json`) — il filesystem del container è effimero
  e la piattaforma raccoglie lo stdout. Il sink su file resta in `appsettings.Development.json`,
  che ora è **versionato** (non contiene segreti). Nota: la configurazione .NET sovrascrive gli
  array **per posizione**, quindi in quel file il sink Console va riconfermato all'indice 0.
- Segreti: connection string e JWT Secret di sviluppo vivono in **User Secrets**
  (`dotnet user-secrets`, non in `appsettings.Development.json` che ora contiene solo
  placeholder vuoti — SEC-001 risolto 13/08/2026). Percorso dello store e comandi in
  `Utilities.txt` alla root del progetto. In produzione: solo env var Railway.

## Test

`GestoraWebApi.Tests/Services/` — xUnit + Moq, pattern Arrange/Act/Assert. Un file per service
(`FasciaOrariaServiceTe.cs`, `PostazioneServiceTests.cs`, `PostazioneAssignmentServiceTests.cs`,
`PrenotazioniServiceTests.cs`, `ZonaServiceTests.cs`, `DisponibilitaServiceTests.cs`,
`DashboardServiceTests.cs`) più `Validators/PrenotazioneCreateDTOValidatorTests.cs` e
`Infrastructure/DbExceptionTranslatorTests.cs`. **224 test totali** (04/09/2026, Fase 7).
Nota: `PrenotazioniServiceTests` configura il contesto InMemory con
`ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))` — l'InMemory non
supporta le transazioni e senza quella riga il service, che ora ne apre una, farebbe fallire
tutti i test della classe. Orologio nei test: `TestClock` (istante fisso, alla radice del progetto
test). Eseguire con `dotnet test` dalla cartella `GestoraWebApi/`. Nessun test su controller o su `AuthenticationUserController` —
la logica di auth non è ancora estratta in un service dedicato. Per mockare `IQueryable<T>`
restituito dai repository (necessario per testare query EF Core come `AnyAsync`/`FirstOrDefaultAsync`
su un repository mockato) si usa il pacchetto `MockQueryable.Moq` (7.0.3, l'unica versione
compatibile con net9.0) — pattern: `lista.AsQueryable().BuildMockDbSet().Object`.
