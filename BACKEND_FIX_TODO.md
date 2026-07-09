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

### FIX-001 — Errore non parlante quando la zona non esiste

**Problema:**
Quando si crea una postazione inserendo un ID zona che non esiste nel sistema,
il backend restituisce un messaggio tecnico di Entity Framework:
*"An error occurred while saving the entity changes. See the inner exception for details."*
Questo messaggio non è comprensibile per l'utente finale.

**Quando si verifica:**
Pagina Postazioni → Aggiungi → si inserisce un numero di zona inesistente → Salva

**Cosa fare:**
Nel service o controller di Postazione, aggiungere un try/catch attorno al salvataggio
su database. Se Entity Framework lancia un'eccezione di violazione di foreign key
(zona non trovata), restituire un messaggio chiaro come:
*"La zona specificata non esiste. Seleziona una zona valida."*

**File da modificare:**
- `Services/Postazioni/PostazioneService.cs` (metodo CreaPostazione)
- oppure `Controllers/PostazioneController.cs`

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
Aggiungere endpoint `GET /api/FasciaOraria/get-all-fasce` che restituisce tutte le fasce senza filtro su `Attiva`.
Aggiungere anche `PATCH /api/FasciaOraria/update-stato/{id}?attiva=true` per attivare/disattivare (come già esiste per Zone).

**File da modificare:**
- `Services/FasceOrarie/FasciaOrariaService.cs`
- `Controllers/FasciaOrariaController.cs`

---

### FIX-004 — Nessuna validazione unicità fascia oraria per giorno

**Problema:**
È possibile creare due fasce orarie con la stessa ora di inizio nello stesso giorno della settimana.
Non esiste un controllo di unicità sul campo `(OrarioInizio, GiornoSettimana)`.

**Quando si verifica:**
Pagina Fasce Orarie → Aggiungi → si inserisce stessa ora inizio e stesso giorno di una fascia esistente → Salva.

**Cosa fare:**
Aggiungere un controllo nel service prima del salvataggio:
verificare che non esista già una fascia con la stessa `OrarioInizio` e `GiornoSettimana`.
Se esiste, restituire un errore `400 Bad Request` con messaggio chiaro.

**File da modificare:**
- `Services/FasceOrarie/FasciaOrariaService.cs` (metodo CreaFasciaAsync)

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

### SEC-001 — Segreti in chiaro in appsettings versionati ⚠️ SICUREZZA

**Problema:**
`appsettings.Development.json` contiene la password del DB (PostgreSQL) e il JWT Secret in chiaro,
ed è tracciato da git → i segreti finiscono nella history del repository.

**Quando si verifica:**
Sempre: chiunque abbia accesso al repo (anche alla sola history) legge le credenziali.

**Cosa fare:**
- Spostare i segreti fuori dal file versionato: **User Secrets** in sviluppo (`dotnet user-secrets`)
  ed **env var / secret store** in produzione (Railway).
- Lasciare in `appsettings*.json` solo placeholder; se il file contiene segreti, aggiungerlo a `.gitignore`.
- **Ruotare** le credenziali già esposte (password DB + JWT Secret): restano comunque nella git history.

**File da modificare:**
- `appsettings.Development.json` (rimuovere i segreti)
- `.gitignore`
- configurazione Railway (env vars)

> Nota: annotato durante la riorganizzazione dei progetti (spostamento in `02_Personali\Gestora`).
> Da affrontare insieme agli sviluppi backend.

---

## Fix completate

- **CORS-001** — CORS configurabile da appsettings/env var ✅ (sessione 18/06/2026)
- **FIX-003** — FasceOrarie restituisce 200 [] su lista vuota ✅ (sessione 18/06/2026)
- **FIX-006** — Endpoint get-mie-prenotazioni per Cliente ✅ (sessione 18/06/2026)
- **FIX-005** — PrenotazioneDTO con FasciaOrariaId e ZonaId ✅ (sessione 18/06/2026)
- **FIX-002** — GET get-all-fasce + PATCH update-stato/{id} per FasceOrarie ✅ (sessione 03/07/2026)
