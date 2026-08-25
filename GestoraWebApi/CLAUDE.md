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
**non** `/api/FasciaOraria/` come riportato in vecchia documentazione. Nota: il file che lo
contiene è ancora `Controllers/FasciaOrariaController.cs` (disallineamento nome file/classe,
vedi NAMING-001).

Formato date: `yyyy-MM-dd`. Header auth: `Authorization: Bearer {token}`.
Formato errori: `{ statusCode, message, errors: [{field, error}] }`.

⚠️ La rotta base è `api/[nome della classe controller]`. Per l'autenticazione la classe è
`AuthenticationUserController`, quindi gli endpoint stanno sotto **`/api/AuthenticationUser/...`**
(es. `/api/AuthenticationUser/login`), non `/api/Auth/...`.

Fuori dai controller: **`GET /health`** — health check pubblico e senza autenticazione, registrato
in `Program.cs`. È il Healthcheck Path configurato su Railway: se non risponde, il deploy viene
marcato come fallito e resta online la versione precedente.

Auth: `POST register`, `POST login`, `POST seed-admin` (si autoblocca dopo il primo Admin),
`POST assign-role`, `DELETE remove-role`, `GET get-users`, `GET get-user/{id}`,
`PUT update-user/{id}`, `DELETE delete-user/{id}`, `POST reset-password/{id}`.

Dashboard: `GET giornaliera?data=`, `GET settimanale?dataInizio=`.

Zona: `GET get-zone-attive`, `GET get-all-zone`, `GET get-zona/{id}`, `POST crea-zona`,
`PUT update-zona`, `PATCH update-stato/{id}?attiva=`, `DELETE delete-zona/{id}`.

Postazione: `GET get-postazioni-attive`, `GET get-postazioni-disponibili`,
`GET get-postazioni-per-zona`, `GET get-postazione-id`, `POST crea-postazione`,
`PUT update-postazione`, `PUT associa-postazione-a-zona`, `DELETE delete-postazione`.

FasceOrarie: `GET fasce-attive`, `GET get-all-fasce` (Admin+Staff), `GET fasce-per-giorno?giorno={0-6}`,
`GET fasce-disponibili?fasciaId=&data=`, `POST crea-fascia`, `PUT update-fascia`,
`PATCH update-stato/{id}?attiva=`, `DELETE delete-fascia`.

Prenotazione: `POST crea-prenotazione`, `POST check-disponibilita` (pubblico, no auth),
`GET get-prenotazione?id=`, `GET get-all-prenotazioni` (filtri opzionali via
`PrenotazioniQueryParams`, paginato), `GET get-mie-prenotazioni` (solo Cliente, filtra su
UserId dal JWT), `GET get-prenotazioni-by-data?data=`, `PUT update-prenotazione` (Admin+Staff,
tolto Cliente — vedi RBAC-001/RBAC-002), `DELETE delete-prenotazione` (solo Admin),
`PATCH conferma-prenotazione?id=` (Admin+Staff), `PATCH completa-prenotazione?id=` (Admin+Staff),
`PATCH annulla-prenotazione?id=` (Admin+Staff, tolto Cliente — vedi RBAC-002 in
BACKEND_FIX_TODO.md per la regola di cutoff da progettare prima di riaprirlo al Cliente).

## RBAC — perimetro ruoli (allineato 13/08/2026, vedi `Auth/Roles.cs`)

- **Admin**: tutti i permessi, nessuna eccezione.
- **Staff**: lettura completa su Zone/Postazioni/FasceOrarie/Prenotazioni (incluso `get-all-fasce`,
  `get-all-zone`, dettagli). Scrittura solo su Prenotazioni: crea, modifica, conferma, completa,
  annulla — **non** elimina (`delete-prenotazione` solo Admin) e **non** scrive su
  Zone/Postazioni/FasceOrarie (resta solo Admin).
- **Cliente**: crea-prenotazione + lettura propria (`get-mie-prenotazioni`) e di supporto alla
  prenotazione (zone/postazioni/fasce attive). **Non** può modificare o annullare le proprie
  prenotazioni self-service (tolto il 13/08/2026, RBAC-002) — richiede intervento Staff/Admin
  finché non viene progettata una regola di cutoff temporale.

Un utente **può avere più ruoli contemporaneamente** (many-to-many `UserRoles`) — è un caso
d'uso legittimo, non un'anomalia (es. account che gestisce il locale ma prenota anche per sé
come Cliente). Il JWT serializza il claim `role` come stringa se l'utente ha un solo ruolo, come
array se ne ha più di uno — il frontend deve normalizzare sempre a array, vedi
`gestora-frontend/CLAUDE.md`.

## Note tecniche da tenere a mente

- HTTPS: Railway termina HTTPS a livello proxy → `UseHttpsRedirection` resta commentato in
  `Program.cs`, non riattivarlo in produzione.
- CORS: origin letti da `AllowedOrigins` in appsettings/env var, mai hardcoded.
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
  container non è raggiungibile nei primi secondi. Attenzione se in futuro si introducono
  transazioni esplicite (`BeginTransaction`): con una strategia di retry vanno eseguite dentro
  `CreateExecutionStrategy().ExecuteAsync(...)`. Oggi nel progetto non ce ne sono.
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
`PrenotazioniServiceTests.cs`, `ZonaServiceTests.cs`). 28 test totali. Eseguire con `dotnet test`
dalla cartella `GestoraWebApi/`. Nessun test su controller o su `AuthenticationUserController` —
la logica di auth non è ancora estratta in un service dedicato. Per mockare `IQueryable<T>`
restituito dai repository (necessario per testare query EF Core come `AnyAsync`/`FirstOrDefaultAsync`
su un repository mockato) si usa il pacchetto `MockQueryable.Moq` (7.0.3, l'unica versione
compatibile con net9.0) — pattern: `lista.AsQueryable().BuildMockDbSet().Object`.
