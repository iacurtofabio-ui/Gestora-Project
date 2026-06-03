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

## Fix completate

*(nessuna ancora)*
