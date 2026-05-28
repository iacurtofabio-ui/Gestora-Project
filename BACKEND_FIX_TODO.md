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

## Fix completate

*(nessuna ancora)*
