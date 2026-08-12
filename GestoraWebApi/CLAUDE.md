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

Auth: `POST register`, `POST login`, `POST seed-admin` (si autoblocca dopo il primo Admin),
`POST assign-role`, `DELETE remove-role`, `GET get-users`, `GET get-user/{id}`,
`PUT update-user/{id}`, `DELETE delete-user/{id}`, `POST reset-password/{id}`.

Dashboard: `GET giornaliera?data=`, `GET settimanale?dataInizio=`.

Zona: `GET get-zone-attive`, `GET get-all-zone`, `GET get-zona/{id}`, `POST crea-zona`,
`PUT update-zona`, `PATCH update-stato/{id}?attiva=`, `DELETE delete-zona/{id}`.

Postazione: `GET get-postazioni-attive`, `GET get-postazioni-disponibili`,
`GET get-postazioni-per-zona`, `GET get-postazione-id`, `POST crea-postazione`,
`PUT update-postazione`, `PUT associa-postazione-a-zona`, `DELETE delete-postazione`.

FasceOrarie: `GET fasce-attive`, `GET get-all-fasce`, `GET fasce-per-giorno?giorno={0-6}`,
`GET fasce-disponibili?fasciaId=&data=`, `POST crea-fascia`, `PUT update-fascia`,
`PATCH update-stato/{id}?attiva=`, `DELETE delete-fascia`.

Prenotazione: `POST crea-prenotazione`, `POST check-disponibilita` (pubblico, no auth),
`GET get-prenotazione?id=`, `GET get-all-prenotazioni` (filtri opzionali via
`PrenotazioniQueryParams`, paginato), `GET get-mie-prenotazioni` (solo Cliente, filtra su
UserId dal JWT), `GET get-prenotazioni-by-data?data=`, `PUT update-prenotazione`,
`DELETE delete-prenotazione`, `PATCH conferma-prenotazione?id=`,
`PATCH completa-prenotazione?id=`, `PATCH annulla-prenotazione?id=`.

## Note tecniche da tenere a mente

- HTTPS: Railway termina HTTPS a livello proxy → `UseHttpsRedirection` resta commentato in
  `Program.cs`, non riattivarlo in produzione.
- CORS: origin letti da `AllowedOrigins` in appsettings/env var, mai hardcoded.
- Cache: chiavi in `Common/CacheKeys.cs` — attenzione a invalidare **tutte** le chiavi
  derivate (es. `FascePerGiorno+giorno`), non solo quella base. ⚠️ Oggi `FasciaOrariaService`
  non lo fa: vedi CACHE-001 in `BACKEND_FIX_TODO.md`.
- Segreti: `appsettings.Development.json` è in `GestoraWebApi/.gitignore` e **non è mai
  entrato nella git history** (verificato 12/08/2026) — nessuna credenziale da ruotare.
  Resta consigliata la migrazione a `dotnet user-secrets`, ma non è urgente (SEC-001).

## Test

`GestoraWebApi.Tests/Services/` — xUnit + Moq, pattern Arrange/Act/Assert. Un file per service
(`FasciaOrariaServiceTe.cs`, `PostazioneAssignmentServiceTests.cs`,
`PrenotazioniServiceTests.cs`, `ZonaServiceTests.cs`). Eseguire con `dotnet test` dalla cartella
`GestoraWebApi/`. Nessun test su controller o su `AuthenticationUserController` — la logica di
auth non è ancora estratta in un service dedicato.
