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
> reale è nell'elenco "Fix completate" più sotto. I due punti di debito tecnico ancora aperti
> (RBAC-002, AUDIT-001) sono ora nella sezione "Backlog post-v1.0" in fondo al file — deciso con
> Fabio il 13/08/2026 di non bloccare il rilascio (Fase 3+) per chiuderli prima.

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

### GAP-001 — Nessuna UI per creare un nuovo utente (gap frontend, non backend)

**Problema:**
Il backend espone `POST /api/AuthenticationUser/register` (pubblico, assegna sempre ruolo
Cliente) ma **nessuna pagina del frontend lo richiama**: `LoginPage.tsx` non ha un link/route di
registrazione, e il pannello Admin Utenti (`AdminUtentiPage.tsx` + `useAdminUtenti.ts`) espone
solo `useUtenti`, `useUpdateUser`, `useDeleteUser`, `useAssignRole`, `useRemoveRole`,
`useResetPassword` — nessun `useCreateUser`. In produzione, senza passare da Postman/Swagger,
non esiste modo di far nascere un nuovo utente (Cliente o Staff) nel sistema.

**Quando si verifica:**
Emerso in Fase 5 testando il pannello Admin Utenti su un DB di produzione con solo l'Admin
seedato: nessun Cliente/Staff da usare per i test 17-18 (assegna/rimuovi ruolo, reset password).

**Cosa fare (da decidere prima del rilascio v1.0, non è solo cosmetico):**
- Pagina pubblica `/register` che chiama `POST register` (assegna sempre Cliente) — per i
  clienti che si auto-registrano, oppure
- Bottone "Crea utente" nel pannello Admin (Admin/Staff creano account per conto del cliente),
  oppure entrambi.
Decisione rimandata a fine Fase 5 per non bloccare il test in corso — nel frattempo gli utenti
di test per Fase 5 sono stati creati a mano via Postman chiamando `register` direttamente.

**File da modificare (quando si implementa):**
- `gestora-frontend/src/pages/LoginPage.tsx` o nuova `RegisterPage.tsx` + route in `router/index.tsx`
- `gestora-frontend/src/pages/AdminUtentiPage.tsx` + nuovo hook `useCreateUser` in `useAdminUtenti.ts`

---

## Backlog post-v1.0 (non bloccante per il rilascio)

> Deciso con Fabio il 13/08/2026: questi punti sono debito tecnico legittimo, ma nessuno dei tre
> blocca il rilascio (Fase 3-8 di `PIANO_RILASCIO.md`). Si affrontano dopo, a mente libera, senza
> far slittare il deploy.

### RBAC-002 — Regola di cutoff per modifica/annullamento self-service del Cliente

**Problema:**
Fino al 13/08/2026 il Cliente poteva modificare (`update-prenotazione`) e annullare
(`annulla-prenotazione`) liberamente le proprie prenotazioni, senza alcun vincolo temporale.
Un annullamento o una modifica dell'ultimo minuto fa perdere al locale la possibilità di
riassegnare il tavolo a un altro cliente — un rischio operativo concreto, non solo di UX.

**Decisione presa il 13/08/2026 (interim):**
`update-prenotazione` e `annulla-prenotazione` sono state ristrette a `Roles.AdminOrStaff`
(tolto `Cliente`) finché non viene progettata la regola definitiva. Nel frattempo il Cliente
può modificare/annullare una prenotazione solo tramite Staff/Admin (telefono, di persona, ecc.),
non più in autonomia dall'app. Questo interim resta valido anche in produzione — non è bloccante,
è il comportamento accettato per v1.0.

**Cosa fare (feature da progettare, non ancora un fix definito):**
Introdurre una finestra di cutoff configurabile (es. "annullabile/modificabile solo fino a N ore
prima dell'orario prenotato") e riaprire `update-prenotazione`/`annulla-prenotazione` al Cliente
con quel vincolo. Decisioni aperte da prendere prima di implementare:
- Quante ore di preavviso minimo?
- Oltre la soglia: azione bloccata del tutto, o richiede approvazione Staff?
- Il vincolo vale anche per Admin/Staff che agiscono per conto del cliente, o solo per il ruolo
  Cliente?

**File da modificare (quando si progetta la regola):**
- `Services/Prenotazioni/PrenotazioneService.cs` (`UpdateAsync`, `AnnullaPrenotazioneAsync`)
- `Controllers/PrenotazioneController.cs` (riportare `Roles.AdminOrStaffOrCliente` una volta
  che il vincolo di cutoff è implementato nel service)

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

### NAMING-001-residuo — Nome file controller disallineato dalla classe

**Problema:**
`Controllers/FasciaOrariaController.cs` contiene la classe `FasceOrarieController` (route base
`/api/FasceOrarie/`). Il DTO omonimo (`PrenotazioneDTO1.cs`) è già stato rinominato il 13/08/2026
(NAMING-001 originale) — questo è un secondo caso dello stesso problema, non ancora sistemato.

**Cosa fare:**
Rinominare il file in `FasceOrarieController.cs` con `git mv` (preserva la history). Puramente
cosmetico, zero rischio funzionale — rimandabile senza costi.

**File da modificare:**
- `Controllers/FasciaOrariaController.cs` → `Controllers/FasceOrarieController.cs`

---

## Fix completate

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
