# gestora-frontend — Frontend

Questo file vale solo quando si lavora dentro `gestora-frontend/`. Per stato sessione, iter di
progetto e protocollo tracker vedi il `CLAUDE.md` alla radice del repo — resta valido sempre.
Per gli endpoint backend vedi `GestoraWebApi/CLAUDE.md`.

## Stack

React 19 + TypeScript + Vite, shadcn/ui + Tailwind CSS, TanStack Query v5 (React Query),
React Hook Form + Zod, **React Router v7** (`createBrowserRouter` — attenzione: qualunque nota
più vecchia che parla di v6 è superata, il progetto usa `react-router-dom ^7.x`), Axios con
interceptor JWT (attach token da localStorage, logout + redirect su 401).

## Routing

`src/router/index.tsx` — route protette per ruolo tramite `ProtectedRoute` che avvolge gruppi
di route: Admin/Staff, Admin/Staff/Cliente, Admin-only. Nuove pagine vanno registrate qui,
dentro il gruppo di ruolo corretto — non creare controlli di ruolo ad-hoc nella pagina stessa.

## Pattern CRUD standard

Ogni modulo CRUD segue questa struttura a 4 file:

```
src/types/{modulo}.ts             - tipi TypeScript (DTO ricevuto + form DTO inviato)
src/hooks/use{Modulo}.ts          - query + mutation (React Query)
src/components/{Modulo}Modal.tsx  - form create/edit (React Hook Form)
src/pages/{Modulo}Page.tsx        - pagina principale con tabella e stato UI
```

**Types** — due tipi per modulo: DTO ricevuto dal backend (include id) e DTO inviato (solo
campi form, senza id). Il backend decide l'id, mai il frontend.

**Hook**:
- query: `queryKey` include tutti i parametri che cambiano il risultato (filtri, pagina);
  `queryFn` chiama `apiClient` e restituisce `r.data`
- mutation: `onSuccess` invalida la cache con `invalidateQueries` + `toast.success`; `onError`
  legge `errors[]` dal backend, fallback su `message`, fallback su testo statico. Se serve sia
  `id` che `body`, si raggruppano in `{ id, data }`

**Modal** — un solo modal gestisce sia create che edit: prop oggetto `undefined` = create
(form vuoto), valorizzata = edit (form ripopolato via `useEffect` + `reset`, fondamentale per
ripopolare ogni volta che cambia l'oggetto selezionato).

**Stato UI pagina** — pattern standard: `oggettoSelezionato` (undefined | DTO, passato al
modal), `modalAperto` (boolean), `idDaEliminare` (undefined | id, controlla `ConfirmDialog`).

**ConfirmDialog** — componente riusabile in `src/components/ConfirmDialog.tsx`, usato su tutti
i moduli per conferma prima di eliminare. Pattern: `idDaEliminare !== undefined` controlla
l'apertura.

**Error handling** — identico in tutti gli `onError`: 1) legge `errors[]` (campo/messaggio da
FluentValidation), 2) fallback su `message`, 3) fallback su testo statico di default.

## Moduli implementati

| Modulo | types | hook | modal | page |
|---|---|---|---|---|
| Zone | zona.ts | useZone.ts | ZonaModal.tsx | ZonePage.tsx |
| Postazioni | postazione.ts | usePostazioni.ts | PostazioneModal.tsx | PostazionePage.tsx |
| FasceOrarie | fasciaOraria.ts | useFasceOrarie.ts | FasciaOrariaModal.tsx | FasciaOrariaPage.tsx |
| Prenotazioni | prenotazione.ts | usePrenotazioni.ts | PrenotazioneModal.tsx | PrenotazionePage.tsx |
| AdminUtenti | utente.ts | useAdminUtenti.ts | EditUserModal + GestisciRuoliModal + ResetPasswordModal | AdminUtentiPage.tsx |

Nuovi moduli CRUD devono seguire esattamente questa struttura — non introdurre varianti senza
motivarle qui.

## Config

`.env.local` → `VITE_API_URL` punta all'API locale (`https://localhost:7175/api` in sviluppo,
URL Railway in produzione dopo il deploy).
