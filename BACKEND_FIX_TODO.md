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
Aggiungere endpoint `GET /api/FasceOrarie/get-all-fasce` che restituisce tutte le fasce senza filtro su `Attiva`.
Aggiungere anche `PATCH /api/FasceOrarie/update-stato/{id}?attiva=true` per attivare/disattivare (come già esiste per Zone).

**File da modificare:**
- `Services/FasceOrarie/FasciaOrariaService.cs`
- `Controllers/FasciaOrariaController.cs`

---

### FIX-004 — Buchi nella validazione sovrapposizione fasce orarie

> ⚠️ **Riformulato il 12/08/2026 dopo lettura del codice.** La formulazione originale chiedeva
> una validazione di unicità su `(OrarioInizio, GiornoSettimana)`. È **ridondante**: `AddAsync`
> (L34-50) e `UpdateAsync` (L206-221) hanno già un controllo di sovrapposizione orari, e due
> fasce con stessa ora di inizio nello stesso giorno si sovrappongono sempre → il duplicato
> esatto è già bloccato. I buchi reali sono altri tre, elencati qui sotto.

**Problema A — `UpdateStatoAsync` non controlla la sovrapposizione (regressione da FIX-002)**

Il metodo aggiunto con FIX-002 (L258) fa `fascia.Attiva = attiva` e salva, senza alcun controllo.
Riattivare una fascia disattivata che si sovrappone a una fascia attiva passa senza errori.
È la via più semplice per portare il DB in stato incoerente, ed è stata aperta da FIX-002.

**Problema B — il controllo filtra `f.Attiva`, ignorando le fasce disattivate**

La query di sovrapposizione considera solo `f.Attiva == true`. È quindi possibile creare una
fascia sovrapposta a una disattivata; poi basta riattivare quest'ultima (problema A) per
ritrovarsi con due fasce attive sovrapposte. A e B vanno chiusi insieme.

**Problema C — l'esito di `TimeSpan.TryParse` viene scartato** (L30-31 e L188-189)

```csharp
TimeSpan.TryParse(dto.OrarioInizio, out var orarioInizio);  // il bool di ritorno è ignorato
```

Un orario non parsabile diventa `00:00` in silenzio e viene salvato senza errori.

**Quando si verifica:**
- A: Fasce Orarie → riattiva una fascia disattivata sovrapposta a una attiva → salva senza errore
- B: Fasce Orarie → Aggiungi una fascia sull'orario di una disattivata → passa
- C: chiamata API con `orarioInizio` non valido → salvato come 00:00

**Cosa fare:**
Estrarre un metodo privato condiviso e chiamarlo da tutti e tre i punti di scrittura:

```csharp
private async Task GuardSovrapposizioneAsync(long idDaEscludere, DayOfWeek giorno,
                                             TimeSpan inizio, TimeSpan fine)
```

Chiamarlo in `AddAsync`, `UpdateAsync` e `UpdateStatoAsync` (solo quando `attiva == true`).
Per C, gestire l'esito `false` del `TryParse` con `ArgumentException` → il middleware la mappa a 400.
Coerente col pattern del progetto: i service lanciano eccezioni tipizzate, mai risposte HTTP.

**File da modificare:**
- `Services/FasceOrarie/FasciaOrariaService.cs` (`AddAsync`, `UpdateAsync`, `UpdateStatoAsync`)
- `GestoraWebApi.Tests/Services/FasciaOrariaServiceTe.cs` (test sui tre casi)

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

### CACHE-001 — Cache `fasce-per-giorno` mai invalidata dopo una scrittura

**Problema:**
`FasciaOrariaService` usa due chiavi di cache: `CacheKeys.FasceAttive` (`fasce_attive`) e
`CacheKeys.FascePerGiorno + (int)giorno` (`fasce_giorno_0..6`, usata da `GetFasceByGiornoAsync`, L141).
`AddAsync`, `UpdateAsync` e `UpdateStatoAsync` invalidano **solo** `FasceAttive`.
Le chiavi per giorno restano in cache con i dati vecchi per tutta la durata (30 minuti).

**Quando si verifica:**
Si crea, modifica o disattiva una fascia → `GET /api/FasceOrarie/fasce-per-giorno?giorno=X`
continua a restituire i dati precedenti fino allo scadere dei 30 minuti.
Impatta anche il flusso di prenotazione, che si appoggia alle fasce del giorno.

**Cosa fare:**
Nei tre metodi di scrittura, invalidare anche la chiave del giorno interessato:
`_cache.Remove(CacheKeys.FascePerGiorno + (int)giorno)`.
Attenzione a `UpdateAsync`: se il `GiornoSettimana` cambia vanno invalidati **entrambi** i
giorni, quello vecchio e quello nuovo.

**File da modificare:**
- `Services/FasceOrarie/FasciaOrariaService.cs` (`AddAsync`, `UpdateAsync`, `UpdateStatoAsync`)

> Nota: rilevato il 12/08/2026 leggendo il codice. Lo stesso pattern va verificato su
> `ZonaService` e `PostazioneService`, che usano anch'essi chiavi multiple.

---

### SEC-001 — Segreti in appsettings ⚠️ SICUREZZA — RIDIMENSIONATO

**Problema (rivalutato il 12/08/2026):**
`appsettings.Development.json` contiene la password del DB (PostgreSQL) e il JWT Secret in chiaro.
Si temeva che fosse tracciato da git, con i segreti finiti nella history.

**Verifica effettuata (12/08/2026):**
- Il file è ignorato da `GestoraWebApi/.gitignore:12`
- `git ls-files "*appsettings*"` → risulta tracciato **solo** `appsettings.json`
- `git log --all -- GestoraWebApi/appsettings.Development.json` → **nessun commit**

→ I segreti **non sono mai entrati nella git history**. Nessuna credenziale da ruotare.
La priorità scende da CRITICO a BASSO.

**Cosa resta da fare:**
- Migrare i segreti di sviluppo a **User Secrets** (`dotnet user-secrets`) — buona pratica,
  evita che un `git add -f` accidentale li esponga in futuro.
- In produzione usare esclusivamente **env var Railway**, mai file versionati.
- Verificare che `appsettings.json` (quello tracciato) contenga solo placeholder vuoti.

**File da modificare:**
- `appsettings.Development.json` (migrazione a user-secrets)
- configurazione Railway (env vars)

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
