# Fix da apportare al Backend

Questo file raccoglie tutti i problemi trovati durante lo sviluppo del frontend
che richiedono una modifica al backend. Vanno risolti DOPO il completamento del frontend,
prima del testing finale e del rilascio in produzione.

---

## Come leggere questo file

Ogni fix ha:
- **Problema**: cosa succede di sbagliato
- **Quando si verifica**: in quale situazione l'utente lo vede
- **Cosa fare**: la modifica da apportare nel backend
- **File da modificare**: dove mettere le mani

---

## Fix da fare

> Nota 13/08/2026: le sezioni sotto (CORS-001, FIX-003, FIX-002, FIX-005, FIX-006) descrivono
> problemi risolti da tempo — la scrittura originale resta come riferimento storico, lo stato
> reale è nell'elenco "Fix completate" più sotto.
>
> Nota 27/08/2026: chiusi anche gli ultimi 5 punti aperti (GAP-001, RBAC-002, AUDIT-001,
> NAMING-001-residuo, DEAD-CODE-001) prima di passare alla Fase 6 — nessun backlog residuo,
> vedi "Fix completate".

---

### CORS-001 — CORS hardcoded su localhost ⚠️ CRITICO — blocca produzione

**Problema:**
`Program.cs` riga 107: `policy.WithOrigins("http://localhost:5173")` è scritto fisso nel codice.
In produzione il frontend sarà su `https://{nome}.vercel.app` — tutte le chiamate API
saranno bloccate dal browser con errore CORS prima ancora di raggiungere il server.

**Quando si verifica:**
Qualsiasi chiamata API dal frontend Vercel verso Railway.

**Cosa fare:**
Leggere gli origin consentiti da configurazione (appsettings / env var Railway):
```csharp
// appsettings.json
"AllowedOrigins": ["http://localhost:5173"]

// appsettings.Production.json (o Railway env var)
"AllowedOrigins": ["https://{nome}.vercel.app"]

// Program.cs
var origins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [];
policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
```

**File da modificare:**
- `Program.cs` (CORS policy)
- `appsettings.json` (aggiungere sezione AllowedOrigins)

---

### FIX-003 — FasceOrarie restituisce 404 invece di array vuoto

**Problema:**
`GET /api/FasceOrarie/fasce-attive` restituisce 404 quando non ci sono fasce attive nel database.
React Query interpreta il 404 come errore, mostrando "Errore nel caricamento" anche quando il database è semplicemente vuoto.

**Quando si verifica:**
Pagina Fasce Orarie → primo accesso con database vuoto.

**Cosa fare:**
Rimuovere il check `if (!fasce.Any()) return NotFound(...)` e restituire sempre `Ok(fasce)`.
Un array vuoto `[]` con status 200 è il comportamento REST corretto per "nessun risultato trovato".

**File da modificare:**
- `Controllers/FasceOrarieController.cs` (metodo GetFasceAttive)

---

### FIX-002 — Mancante endpoint get-all per Fasce Orarie

**Problema:**
Esiste solo `GET /fasce-attive` — non c'è un endpoint che restituisce tutte le fasce (attive e non).
Dal pannello admin non è possibile vedere le fasce disattivate per riattivarle.

**Quando si verifica:**
Pannello admin Fasce Orarie — fasce disattivate diventano invisibili e irrecuperabili dalla UI.

**Cosa fare:**
Aggiungere endpoint `GET /api/FasceOrarie/get-all-fasce` che restituisce tutte le fasce senza filtro su `Attiva`.
Aggiungere anche `PATCH /api/FasceOrarie/update-stato/{id}?attiva=true` per attivare/disattivare (come già esiste per Zone).

**File da modificare:**
- `Services/FasceOrarie/FasciaOrariaService.cs`
- `Controllers/FasciaOrariaController.cs`

---

### FIX-005 — PrenotazioneDTO manca fasciaOrariaId e zonaId

**Problema:**
`PrenotazioneDTO` restituisce `oraInizio`/`oraFine` ma non `fasciaOrariaId` e `zonaId`.
Il frontend non può pre-compilare il form di modifica senza questi campi.

**Quando si verifica:**
Pagina Prenotazioni → click Modifica su una prenotazione esistente.

**Cosa fare:**
Aggiungere `FasciaOrariaId` e `ZonaId` a `PrenotazioneDTO` nel mapping del service.

**File da modificare:**
- `Services/Prenotazioni/PrenotazioneService.cs` (mapping → DTO)
- `Services/Prenotazioni/DTOs/PrenotazioneDTO.cs`

---

### FIX-006 — Nessun endpoint per le prenotazioni del Cliente

**Problema:**
`GET /api/Prenotazione/get-all-prenotazioni` è accessibile solo ad Admin/Staff (403 per Cliente).
Il Cliente non ha nessun endpoint per vedere le proprie prenotazioni.

**Quando si verifica:**
Pagina Prenotazioni con utente ruolo Cliente → errore 403.

**Cosa fare:**
Aggiungere endpoint `GET /api/Prenotazione/get-mie-prenotazioni` che restituisce
solo le prenotazioni dell'utente loggato (filtro per UserId dal JWT).
Oppure modificare `get-all-prenotazioni` per filtrare automaticamente in base al ruolo:
se Cliente → restituisce solo le proprie; se Admin/Staff → restituisce tutte.

**File da modificare:**
- `Controllers/PrenotazioneController.cs`
- `Services/Prenotazioni/PrenotazioneService.cs`

---

## Fix completate

- **GAP-001** — Nessuna UI per creare utenti. Implementate entrambe le opzioni (decisione
  Fabio 27/08/2026): pagina pubblica `/register` (`RegisterPage.tsx`, linkata da `LoginPage.tsx`,
  assegna sempre Cliente via `POST register`) + bottone "+ Crea utente" in
  `AdminUtentiPage.tsx` (`CreateUserModal.tsx`, nuovo hook `useCreateUser` che compone
  `register` → `get-users` → `assign-role`/`remove-role` per assegnare un ruolo diverso da
  Cliente, dato che il backend non ha un endpoint dedicato). Nessuna modifica al backend.
  ✅ (sessione 27/08/2026)
- **RBAC-002** — Regola di cutoff per il Cliente self-service (decisione Fabio 27/08/2026: 2 ore
  di preavviso minimo, oltre la soglia azione bloccata del tutto senza approvazione Staff,
  vincolo non applicato ad Admin/Staff). `update-prenotazione`/`annulla-prenotazione` riaperte
  a `Roles.AdminOrStaffOrCliente`; `PrenotazioniService` aggiunge `GuardCutoffAsync` (confronta
  `DataPrenotazione` + `FasciaOraria.OrarioInizio` con l'ora corrente, costante
  `CutoffOreClienteSelfService = 2`) e un controllo di ownership su `AnnullaPrenotazioneAsync`
  (mancava, `UpdateAsync` ce l'aveva già), entrambi attivi solo per `IsSelfServiceCliente()`.
  3 nuovi test (ownership altrui, oltre cutoff, entro cutoff). ✅ (sessione 27/08/2026)
- **AUDIT-001** — Estesa la tracciabilità utente (`ILogActivityService`/`Logging`, già in uso
  solo su `AuthenticationUserController`) a tutte le scritture di `ZonaService`,
  `PostazioneService`, `FasciaOrariaService` — stesso pattern già presente in
  `PrenotazioniService` (`IHttpContextAccessor` iniettato, helper privati
  `GetAuthenticatedUserId()`/`GetIpAddress()`). Nessuna nuova tabella, riuso di infrastruttura
  esistente. ✅ (sessione 27/08/2026)
- **NAMING-001-residuo** — `Controllers/FasciaOrariaController.cs` rinominato in
  `FasceOrarieController.cs` con `git mv` (preserva la history), nessun impatto sulla route
  (deriva dal nome della classe, invariata). ✅ (sessione 27/08/2026)
- **DEAD-CODE-001** — Rimosso da `PostazioneRepository.GetPostazioniDisponibiliAsync()` lo stesso
  filtro "mai prenotata" già corretto su `GetPostazioniPerZonaAsync` il 26/08/2026. Endpoint
  tuttora non collegato al frontend, fix preventivo. ✅ (sessione 27/08/2026)
- **FIX-009** — Vincolo "una prenotazione al giorno" bloccava anche dopo un annullamento: indice
  univoco `UX_Prenotazione_User_DataPrenotazione` su `(UserId, DataPrenotazione)` non escludeva lo
  stato `Annullata`. Aggiunto filtro `WHERE "Stato" <> 'Annullata'` (migration
  `FilterIndicePrenotazioneEscludeAnnullata`), messaggio d'errore reso esplicito ("prenotazione
  **attiva**"). Applicata a mano via `psql` sul DB Railway (tunnel privato via Railway CLI,
  nessuna esposizione pubblica del DB), verificata con `\d "Prenotazioni"` e test end-to-end.
  ✅ (sessione 25/08/2026)
- **UI-001** — Etichetta di stato prenotazione "In Corso" fraintesa: indica una prenotazione
  confermata (`InCorso` a DB), non che l'orario prenotato sia in corso ora — un utente ha
  scambiato l'errore "Non è possibile completare: non ancora terminata" per un bug legato al
  filtro. Rinominata solo l'etichetta UI in "Confermata" (`STATO_LABELS` in
  `types/prenotazione.ts`), enum/DB invariati. ✅ (sessione 25/08/2026)
- **FIX-008** — Pagina Admin Fasce Orarie usava l'hook `useFasceOrarie()` (endpoint
  `fasce-attive`) anche per la propria lista di gestione: una fascia disattivata spariva dalla
  tabella e non era più raggiungibile per riattivarla, mentre il guard anti-sovrapposizione
  (FIX-004) la considerava comunque esistente — utente bloccato. Aggiunto hook dedicato
  `useAllFasceOrarie()` (endpoint `get-all-fasce`, già esistente lato backend da FIX-002) usato
  solo da `FasciaOrariaPage`; l'hook condiviso con `PrenotazioneModal` resta su `fasce-attive`
  per non esporre fasce disattivate in fase di prenotazione. Nessuna modifica al backend.
  ✅ (sessione 25/08/2026)
- **FIX-007** — Rimosso `NotFound` su lista vuota in `ZonaController` (`get-zone-attive`,
  `get-all-zone`), `PostazioneController` (`get-postazioni-attive`, `get-postazioni-disponibili`,
  `get-postazioni-per-zona`), `PrenotazioneController` (`get-prenotazioni-by-data`) — stesso
  pattern di FIX-003 esteso ai controller rimasti. `dotnet test` 28/28 verdi, verificato in
  produzione: ZonePage/PostazionePage/PrenotazionePage mostrano tabella vuota invece di
  "Errore nel caricamento". ✅ (sessione 25/08/2026)
- **CORS-001** — CORS configurabile da appsettings/env var ✅ (sessione 18/06/2026)
- **FIX-003** — FasceOrarie restituisce 200 [] su lista vuota ✅ (sessione 18/06/2026)
- **FIX-006** — Endpoint get-mie-prenotazioni per Cliente ✅ (sessione 18/06/2026)
- **FIX-005** — PrenotazioneDTO con FasciaOrariaId e ZonaId ✅ (sessione 18/06/2026)
- **FIX-002** — GET get-all-fasce + PATCH update-stato/{id} per FasceOrarie ✅ (sessione 03/07/2026)
- **FIX-004 (A/B/C)** — Guard sovrapposizione condiviso su AddAsync/UpdateAsync/UpdateStatoAsync
  di `FasciaOrariaService`, esteso anche alle fasce disattivate; `TimeSpan.TryParse` ora valida
  l'esito. 8 nuovi test. ✅ (sessione 13/08/2026)
- **CACHE-001** — Invalidazione `fasce_giorno_{n}` su tutte le scritture di `FasciaOrariaService`
  (entrambi i giorni se `UpdateAsync` cambia `GiornoSettimana`). Verificato lo stesso pattern su
  `ZonaService` (ok, chiave unica condivisa) e `PostazioneService`: trovato e corretto un buco
  identico in `AssociaPostazioneAZonaAsync` (non invalidava `PostazioniAttive`). ✅ (sessione 13/08/2026)
- **FIX-001** — `PostazioneService.AddAsync` validava già l'esistenza della zona; aggiunta la
  stessa validazione a `UpdateAsync(PostazioneUpdateDTO)`, che ne era priva. ✅ (sessione 13/08/2026)
- **NAMING-001** — `PrenotazioneDTO1.cs` rinominato in `PrenotazioneCreateDTO.cs`. ✅ (sessione 13/08/2026)
- **RBAC-001** — Allineato il modello di ruoli a quanto definito da Fabio il 13/08/2026: Staff
  ha lettura completa (aggiunto `get-all-fasce` a `AdminOrStaff`, prima solo Admin) e scrittura
  su prenotazioni limitata a crea/modifica/conferma/completa/annulla (tolto `delete-prenotazione`,
  ora solo Admin); Cliente ristretto a crea-prenotazione + lettura propria (tolti
  `update-prenotazione`/`annulla-prenotazione`, vedi RBAC-002 per la regola di cutoff da
  progettare prima di riaprirli). ✅ (sessione 13/08/2026)
- **SEC-001** — Segreti dev (`appsettings.Development.json`) mai entrati nella git history
  (verificato 12/08/2026, priorità scesa da CRITICO a BASSO). Migrati a `dotnet user-secrets`,
  file ripulito con placeholder vuoti, app testata end-to-end. Percorso store e comandi in
  `Utilities.txt`. In produzione resta da usare esclusivamente env var Railway (Fase 3).
  ✅ (sessione 13/08/2026)
