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

## Ruoli utente — sempre array, mai stringa singola

⚠️ Un utente **può avere più ruoli** (Admin+Staff+Cliente insieme è un caso d'uso legittimo, non
un'anomalia — vedi `GestoraWebApi/CLAUDE.md` sezione RBAC). Il JWT serializza il claim
`http://schemas.microsoft.com/ws/2008/06/identity/claims/role` come **stringa singola** se
l'utente ha un solo ruolo, come **array** se ne ha più di uno — comportamento standard di
ASP.NET Identity, non un bug backend.

`AuthUser.roles` (`src/context/auth-context.ts`) è quindi tipizzato `string[]`, **mai** `string`.
`AuthContext.tsx` normalizza sempre il claim grezzo con `normalizeRoles()` prima di metterlo in
`roles` — non leggere mai il claim direttamente altrove. Per controllare i ruoli:
- singolo controllo: `user?.roles.includes('Admin')`
- controllo su un elenco di ruoli consentiti: `allowedRoles.some(r => user.roles.includes(r))`
  (pattern usato in `ProtectedRoute.tsx`)

Mai `user.roles === 'Admin'` o `allowedRoles.includes(user.roles)` — confronterebbe un array
con una stringa, sempre falso (bug reale, risolto il 13/08/2026, vedi tracker).

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
| AdminUtenti | utente.ts | useAdminUtenti.ts | EditUserModal + GestisciRuoliModal + ResetPasswordModal + CreateUserModal | AdminUtentiPage.tsx |

Nuovi moduli CRUD devono seguire esattamente questa struttura — non introdurre varianti senza
motivarle qui.

### Nota GAP-001 — creazione utenti (non un CRUD standard)

Il backend non ha un endpoint "crea utente con ruolo": `POST register` è pubblico e assegna
sempre `Cliente`. `useCreateUser` (in `useAdminUtenti.ts`) compone tre chiamate esistenti:
`register` → `get-users` (per recuperare l'id appena creato, l'endpoint register non lo
restituisce) → `assign-role`/`remove-role` se l'Admin ha scelto un ruolo diverso da Cliente.
Pagina pubblica `/register` (`RegisterPage.tsx`, linkata da `LoginPage.tsx`) chiama `register`
direttamente, senza passare da `useCreateUser`.

## Config

`.env.local` → `VITE_API_URL` punta all'API locale (`https://localhost:7175/api` in sviluppo,
URL Railway in produzione dopo il deploy).
