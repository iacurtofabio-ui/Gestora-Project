# Guida Pattern CRUD — Frontend Gestora

Questo documento descrive il pattern standard usato in tutti i moduli CRUD del frontend.
Serve come riferimento rapido per chi legge il codice o aggiunge nuovi moduli.

---

## Struttura per ogni modulo CRUD

```
src/types/{modulo}.ts             - tipi TypeScript (DTO ricevuto + form DTO inviato)
src/hooks/use{Modulo}.ts          - query + mutation (React Query)
src/components/{Modulo}Modal.tsx  - form create/edit (React Hook Form)
src/pages/{Modulo}Page.tsx        - pagina principale con tabella e stato UI
```

---

## 1. Types

Ogni modulo ha due tipi principali:
- DTO ricevuto dal backend (include id)
- DTO inviato al backend (solo i campi del form, senza id)

Regola: mai usare il DTO pieno come tipo del form. Il backend decide l'id, non il frontend.

---

## 2. Hook — pattern standard

QUERY (legge dati):
- queryKey include tutti i parametri che cambiano il risultato (es. filtri, pagina)
- queryFn chiama apiClient e restituisce r.data

MUTATION (scrive dati):
- onSuccess: invalida la cache con invalidateQueries + toast.success
- onError: legge errors[] dal backend, fallback su message, fallback su testo statico
- Se mutationFn ha bisogno sia di id che di body, si raggruppano in un oggetto: { id, data }

---

## 3. Modal (Create/Edit)

Un solo modal gestisce sia create che edit:
- Se la prop zona e undefined = create (form vuoto)
- Se zona e valorizzata = edit (form ripopolato via useEffect + reset)

Il useEffect con reset e fondamentale: ripopola il form ogni volta che cambia l'oggetto selezionato.

---

## 4. Stato UI nella pagina

Pattern standard:
- oggettoSelezionato (undefined | DTO) — passato al modal; undefined = create, valorizzato = edit
- modalAperto (boolean) — controlla visibilita modal
- idDaEliminare (undefined | id) — controlla ConfirmDialog; undefined = chiuso

---

## 5. ConfirmDialog

Componente riusabile in src/components/ConfirmDialog.tsx.
Usato su tutti i moduli per la conferma prima di eliminare un record.
Il pattern idDaEliminare !== undefined controlla l'apertura del dialog.

---

## 6. Error handling unificato

Pattern identico in tutti gli onError delle mutation:
1. Legge errors[] (array campo/messaggio dal backend FluentValidation)
2. Fallback su message (messaggio generico del backend)
3. Fallback su testo statico di default

---

## Moduli implementati

Zone         - zona.ts          - useZone.ts          - ZonaModal.tsx           - ZonePage.tsx
Postazioni   - postazione.ts    - usePostazioni.ts    - PostazioneModal.tsx     - PostazionePage.tsx
FasceOrarie  - fasciaOraria.ts  - useFasceOrarie.ts   - FasciaOrariaModal.tsx   - FasciaOrariaPage.tsx
Prenotazioni - prenotazione.ts  - usePrenotazioni.ts  - PrenotazioneModal.tsx   - PrenotazionePage.tsx
AdminUtenti  - utente.ts        - useAdminUtenti.ts   - EditUserModal + GestisciRuoliModal + ResetPasswordModal - AdminUtentiPage.tsx
