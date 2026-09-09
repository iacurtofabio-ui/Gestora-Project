# gestora-frontend — Frontend

Questo file vale solo quando si lavora dentro `gestora-frontend/`. Per stato del progetto,
decisioni e protocollo tracker vedi il `CLAUDE.md` alla radice del repo — resta valido sempre.
Per gli endpoint backend vedi `GestoraWebApi/CLAUDE.md`. Per i comandi vedi `RUNBOOK.md`.

## Stack

React 19 + TypeScript + Vite, shadcn/ui + Tailwind CSS, TanStack Query v5 (React Query),
React Hook Form + Zod, **React Router v7** (`createBrowserRouter` — attenzione: qualunque nota
più vecchia che parla di v6 è superata, il progetto usa `react-router-dom ^7.x`), Axios con
interceptor JWT (attach token da localStorage, logout + redirect su 401).

## Tavolozza e tema — leggere prima di scrivere un colore

La direzione visiva si chiama **«Turno»**: la sala vista nel tempo. L'unità di base non è la card,
è la fascia oraria come banda che si riempie di coperti. Freddo, denso, industriale.

**Non scrivere mai un colore fisso** (`bg-white`, `text-gray-500`, `text-red-500`). Si usano
sempre i token del tema, altrimenti quella zona resta bianca in tema scuro.

| Serve... | Si usa |
|---|---|
| testo normale | `text-foreground` |
| testo secondario | `text-muted-foreground` |
| sfondo della pagina | `bg-background` |
| sfondo di una card | `bg-card` |
| dialog, menu, popover | `bg-popover` |
| azione principale | `bg-primary` / `text-primary` |
| errore, eliminazione | `text-destructive` / `bg-destructive` |
| esito positivo, disponibile | `text-success` |
| avviso, quasi pieno | `text-warning` |
| **riempimento** della banda | `bg-banda-attenzione` / `bg-banda-pieno` |
| fondo su cui corre la banda | `bg-traccia` |
| voce di menu evidenziata | `bg-evidenza` |
| bordo decorativo | `border` |
| bordo di un campo | `border-input` |

### Le tre cose da sapere

1. **Il tema scuro ha croma zero.** I neutri sono scritti `oklch(L 0 0)`: è verificabile a
   colpo d'occhio. Prima avevano croma su tinta 55 (arancio), ed è per questo che il tema scuro
   tendeva al marrone.

2. **Quattro livelli di superficie, un gradino da 0,042 di chiarezza ciascuno.** Fondo `0.145`
   → righe e sidebar `0.187` → card e header `0.231` → dialog e menu `0.273`. Il fondo non è il
   nero pieno di proposito: con `#000000` il primo gradino varrebbe `0.187`, quattro volte gli
   altri, e la scala smetterebbe di leggersi come scala. Su IPS il nero pieno non è nemmeno più
   nero — la retroilluminazione resta accesa.

3. **Testo e campitura dello stesso stato devono avere la stessa tinta.** `--warning` è scuro
   perché deve restare leggibile come testo; `--banda-attenzione` è più acceso perché è una
   campitura che non porta testo. Stessa H di oklch, diversa L e C. Lo verifica lo script.

### Tipografia

**Archivo Variable**, self-hosted via Fontsource, un solo asse (`wght` 100–900). Sei ruoli
dichiarati come token, da usare al posto delle classi improvvisate:

`text-readout` (i numeri grandi) · `text-readout-sm` · `text-titolo` (uno per schermata) ·
`text-sezione` (intestazione di blocco) · `text-corpo` · `text-orario` (orari e quantità in
tabella, con `tabular-nums`) · `text-nota`.

Il ripiego anti-CLS (`@font-face 'Archivo Ripiego'` in `index.css`) ha tre numeri **misurati**,
non stimati: si rigenerano con `node scripts/metriche-ripiego.mjs`.

### Raggi: tre scaglioni, non uno

`2px` bande e righe · `6–8px` controlli · `14px` ciò che galleggia (dialog, menu). Una sola
ombra, riservata a ciò che galleggia davvero. Il raggio dice *che cosa è* una cosa.

### Le azioni di riga

In tabella non si mettono più pulsanti affiancati. Si usa `components/AzioniRiga.tsx`: **una**
azione in chiaro (variante `azione` del Button — testo nel colore segnale, si riempie
sull'hover della riga), il resto nel menu `…`, la distruttiva sotto un separatore in rosso. La
`<TableRow>` deve avere `className="group/riga"`, altrimenti il pulsante non reagisce alla riga.
Il **pieno** resta all'azione primaria della pagina, che è una sola.

> ⚠️ **Dopo ogni modifica a un colore in `src/index.css` va rieseguito:**
> ```
> node scripts/contrasto.mjs
> ```
> Verifica il contrasto di ogni coppia testo/fondo (4.5:1 per il testo, 3:1 per il bordo dei
> campi e per il ring) **e** la coerenza di tinta fra testo e campitura. Non è un controllo
> formale: ha già trovato quattro combinazioni illeggibili che a occhio sembravano a posto.

**Tema chiaro/scuro**: `context/ThemeContext.tsx` aggiunge o toglie la classe `dark` sul
documento; la scelta si fa con l'interruttore in `components/ThemeToggle.tsx` e si ricorda in
`localStorage`. Tre stati, non due: **chiaro**, **scuro** e **come il sistema**, che è il
predefinito. Questi sono i nomi da usare ovunque — nel codice, nei nomi dei file, parlandone.
Uno script dentro `index.html` applica il tema **prima** che React parta, altrimenti in
caricamento si vede un lampo bianco.

## Pagina pubblica

`/` mostra `pages/LandingPage.tsx`, raggiungibile **senza account**. Prima era un rimando secco a
`/login`.

> ⚠️ **In quella pagina non c'è nessun token.** L'unico endpoint chiamabile è
> `check-disponibilita`, che il backend dichiara pubblico. `get-zone-attive`, `get-all-fasce` e
> tutto il resto **richiedono l'accesso**: chiamarli da lì produce un 401 su una pagina pubblica.
> Per questo le zone in vetrina sono testo scritto nella pagina, non dati del database.

Due condizioni restano attive sulla radice, entrambe già presenti prima: `SetupGuard` (senza un
Admin si va comunque a `/setup`) e `RedirectSeAutenticato` (chi ha la sessione aperta va alla sua
pagina, non alla vetrina).

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
| `components/StatoBadge.tsx` | Lo stato di una prenotazione: un punto più la parola, non una pillola. Il colore non è mai l'unica informazione |
| `components/StatoAttivo.tsx` | Attiva / non attiva, per zone, tavoli e fasce. Erano tre copie scritte a mano |
| `components/AzioniRiga.tsx` | Le azioni di una riga di tabella: una in chiaro, il resto nel menu, la distruttiva separata |
| `components/AzioniPrenotazione.tsx` | La parte di dominio sopra `AzioniRiga`: quale azione ha senso in quale stato |
| `components/BandaCoperti.tsx` | La banda che si riempie di coperti, con `lib/coperti.ts` per le soglie |
| `components/IntestazionePagina.tsx` | Titolo di schermata + conteggio + azione primaria |
| `components/SchermataMessaggio.tsx` | La schermata a tutta pagina: errore, accesso negato, configurazione mancante |
| `components/PageState.tsx` → `ErroreForm` | L'errore che riguarda l'invio di un form, non un singolo campo |
| `components/Logo.tsx` | Il marchio, disegnato come SVG così prende i colori del tema |
| `components/PageState.tsx` → `TableSkeleton`, `DashboardSkeleton` | Scheletri di caricamento: disegnano la forma del contenuto in attesa, così la pagina non "salta" quando i dati arrivano. `righe` e `colonne` vanno messe su quello che la pagina mostra davvero |
| `components/ErrorBoundary.tsx` | Rete di sicurezza **sopra** il router. Unico componente a classe del progetto: React non offre un equivalente con gli hook |
| `router/RouteErrorPage.tsx` | Rete di sicurezza **dentro** il router. Serve perché React Router cattura gli errori delle pagine prima dell'ErrorBoundary |

Tutti i pulsanti d'azione usano `<Button>` di shadcn. Dalla Fase 13 **non ci sono più eccezioni**:
anche il logout nell'header, rimasto testo semplice in Fase 10, è un pulsante.

**Due menu a tendina diversi, di proposito:**
- `components/ui/select.tsx` (Radix) — nei **filtri delle pagine**. ⚠️ Radix non ammette la
  stringa vuota come valore: per "nessun filtro" serve un valore vero (es. `'tutti'`) tradotto in
  `undefined` prima della chiamata.
- `components/ui/native-select.tsx` — nei **form**. È un `<select>` vero vestito come un `Input`.
  Funziona con un semplice `{...register('campo')}` senza `Controller`, sul telefono apre la
  rotella di sistema, e nei test si pilota con `userEvent.selectOptions`. Radix in ambiente di
  test ha bisogno di sostituire a mano funzioni che jsdom non implementa ed è una fonte nota di
  test instabili: nei form non ne vale la pena.

Tutte le etichette dei form sono collegate al proprio campo con `htmlFor` / `id` (REV-072). Sui
form di Login e Registrazione l'etichetta c'è ma è `sr-only`, perché il design usa il segnaposto.

## Test

**35 test** con Vitest + Testing Library, lanciati con `npm test`:
lettura difensiva del token (10), helper degli errori (5), `ProtectedRoute` su accesso e ruoli
(6), scelta della fascia oraria in `PrenotazioneModal` (5), azioni di riga e tastiera in
`AzioniPrenotazione` (9: ordine di Tab, apertura del menu, Esc che riporta il fuoco).

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
