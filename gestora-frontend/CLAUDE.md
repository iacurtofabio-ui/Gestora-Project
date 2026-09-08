# gestora-frontend — Frontend

Questo file vale solo quando si lavora dentro `gestora-frontend/`. Per stato del progetto,
decisioni e protocollo tracker vedi il `CLAUDE.md` alla radice del repo — resta valido sempre.
Per gli endpoint backend vedi `GestoraWebApi/CLAUDE.md`. Per i comandi vedi `RUNBOOK.md`.

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
- mutation: `onSuccess` invalida la cache con `invalidateQueries` + `toast.success`;
  `onError: segnalaErrore('testo di ripiego')` — **mai** scrivere la gestione errore a mano.
  Se serve sia `id` che `body`, si raggruppano in `{ id, data }`
- i path degli endpoint si prendono da `lib/endpoints.ts`, **mai scritti inline** (REV-082)

**Modal** — un solo modal gestisce sia create che edit: prop oggetto `undefined` = create
(form vuoto), valorizzata = edit (form ripopolato via `useEffect` + `reset`, fondamentale per
ripopolare ogni volta che cambia l'oggetto selezionato).

**Stato UI pagina** — pattern standard: `oggettoSelezionato` (undefined | DTO, passato al
modal), `modalAperto` (boolean), `idDaEliminare` (undefined | id, controlla `ConfirmDialog`).

**ConfirmDialog** — componente riusabile in `src/components/ConfirmDialog.tsx`, usato su tutti
i moduli per conferma prima di eliminare. Pattern: `idDaEliminare !== undefined` controlla
l'apertura.

## Helper e componenti condivisi — usare questi, non riscriverli

Nati dalle Fasi 8-10 per togliere codice ripetuto. Se stai per scrivere una di queste cose a
mano, esiste già.

| Dove | Cosa fa |
|---|---|
| `lib/apiError.ts` | `messaggioErrore` / `segnalaErrore` per le **scritture**, `messaggioErroreCaricamento` per le **letture**. Distingue anche il caso "richiesta mai partita" (server spento, rete assente) da un errore del server |
| `lib/endpoints.ts` | Tutti i path degli endpoint, raggruppati per area come nel backend |
| `lib/validazioni.ts` | Le regole condivise dei form, password inclusa: 8 caratteri, maiuscola, numero, carattere speciale. Prima ce n'erano **quattro versioni diverse** e solo una era giusta |
| `lib/date.ts` | `oggiInItalia`, `lunediSettimanaCorrenteInItalia`. Fuso `Europe/Rome` fissato nel codice: il locale è del ristorante, non del dispositivo di chi guarda. **Mai** usare `toISOString()` per una data di calendario |
| `lib/jwt.ts` | Unico punto che legge il token. È una **lettura**, non una verifica: la firma non è controllabile dal browser |
| `lib/session.ts` | Ponte fra l'intercettore Axios (che vive fuori da React) e i componenti, per la scadenza sessione senza ricaricare la pagina |
| `lib/giorni.ts` | `GIORNI_SETTIMANA`, prima duplicata in due file |
| `lib/queryClient.ts` | Impostazioni di React Query: niente nuovi tentativi sui 4xx, nessun tentativo sulle mutation (creerebbe doppioni) |
| `components/PageState.tsx` | `PageLoading` / `PageError`: sostituiscono **solo il contenuto**, lasciando intestazione e filtri a schermo |
| `components/EmptyState.tsx` | Messaggio per le liste vuote |
| `components/Paginazione.tsx` | Barra di navigazione delle pagine |
| `components/ConfirmDialog.tsx` | Conferma prima di un'azione distruttiva. Accetta `titolo` e `testoConferma`: annullare ed eliminare non devono avere la stessa faccia |
| `components/ErrorBoundary.tsx` | Rete di sicurezza **sopra** il router. Unico componente a classe del progetto: React non offre un equivalente con gli hook |
| `router/RouteErrorPage.tsx` | Rete di sicurezza **dentro** il router. Serve perché React Router cattura gli errori delle pagine prima dell'ErrorBoundary |

Tutti i pulsanti d'azione usano `<Button>` di shadcn (REV-075). Unica eccezione voluta: il link
"Logout" nell'header, che resta testo semplice.

Tutte le etichette dei form sono collegate al proprio campo con `htmlFor` / `id` (REV-072). Sui
form di Login e Registrazione l'etichetta c'è ma è `sr-only`, perché il design usa il segnaposto.

## Test

**26 test** con Vitest + Testing Library, lanciati con `npm test`:
lettura difensiva del token (10), helper degli errori (5), `ProtectedRoute` su accesso e ruoli
(6), scelta della fascia oraria in `PrenotazioneModal` (5).

> ⚠️ `npm test` **non controlla i tipi**. Serve anche `npm run build`, che li controlla: in Fase 8
> la build ha trovato un campo scritto male in un fixture che i test non vedevano.

> ⚠️ `hooks/usePrenotazioni.ts` è **mockato per intero** nei test di `PrenotazioneModal`. Per
> questo `useCheckDisponibilita` sta in un modulo separato: messo lì dentro sarebbe sparito con
> lo stesso mock.

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

## Config — due ambienti separati, non uno che cambia

`.env.local` (non versionato) → `VITE_API_URL`, letto **solo** da Vite in locale (`npm run dev`).
Da tenere stabilmente su `http://localhost:5099/api` — è l'ambiente di sviluppo, permanente, non
un valore da alternare per un test estemporaneo. Richiede il backend locale attivo
(`dotnet run` in `GestoraWebApi/`, porta fissata in `Properties/launchSettings.json`).

La produzione (`gestora-project-xi.vercel.app`) **non legge mai `.env.local`**: usa la propria
`VITE_API_URL` impostata nella dashboard Vercel, puntata a Railway — indipendente e sempre
raggiungibile, a prescindere da cosa gira in locale. Modificare `.env.local` in locale non ha
alcun effetto sulla build di produzione.

Attenzione: con questa config, aprire `localhost:5173` mostra sempre i dati del DB locale, non
quelli di produzione — comportamento voluto, non un errore (27/08/2026).
