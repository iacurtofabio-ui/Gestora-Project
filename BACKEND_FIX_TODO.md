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

### AUDIT-001 — Nessuna tracciabilità utente sulle azioni

**Problema:**
Le azioni sulle entità principali (creazione/modifica/eliminazione zone, postazioni, fasce,
prenotazioni) non registrano quale utente le ha effettuate.

**Quando si verifica:**
Sempre — nessun log applicativo riporta id/nome utente per un'azione specifica, solo Serilog
generico sulle richieste HTTP.

**Cosa fare:**
Da valutare: aggiungere `UserId` (dal JWT) come campo di log strutturato in Serilog sulle
azioni di scrittura, oppure un campo `ModificatoDa` sulle entità. Scelta architetturale da
discutere prima di implementare — non è ancora un fix definito, solo un'esigenza registrata.

**File da modificare:**
- Da definire in fase di progettazione

> Nota: migrata il 12/08/2026 da un file sciolto (`Appunti Fix da valutare.txt`), rimosso
> perché fuori da qualunque tracker.

---

## Fix completate

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
- **SEC-001** — Segreti dev (`appsettings.Development.json`) mai entrati nella git history
  (verificato 12/08/2026, priorità scesa da CRITICO a BASSO). Migrati a `dotnet user-secrets`,
  file ripulito con placeholder vuoti, app testata end-to-end. Percorso store e comandi in
  `Utilities.txt`. In produzione resta da usare esclusivamente env var Railway (Fase 3).
  ✅ (sessione 13/08/2026)
