# Progetto Gestora — Full Stack

## LEGGI QUESTO PRIMA DI TUTTO — STATO SESSIONE

Ultima sessione: 27/08/2026
Ultima cosa fatta: **FASE 7 COMPLETATA — testing integrato in produzione (Vercel + Railway +
PostgreSQL). Prossimo passo: FASE 8 (Rilascio).**

URL produzione: backend `https://gestora-project-production.up.railway.app`, frontend
`https://gestora-project-xi.vercel.app`. CORS allineato (`AllowedOrigins__1` su Railway con
l'URL Vercel, verificato con preflight OPTIONS).

Nessun backlog residuo prima del rilascio v1.0: chiusi tutti i punti sospesi dalla Fase 5
(GAP-001, RBAC-002, AUDIT-001, NAMING-001-residuo, DEAD-CODE-001, dettaglio in
`BACKEND_FIX_TODO.md` sezione "Fix completate") e tutti i bug emersi nel testing integrato di
Fase 7:
- **404 Vercel su refresh e su login fallito**: mancava `vercel.json` con rewrite verso
  `index.html` (Vercel non sa che le route sono gestite da React Router lato client). In più,
  l'interceptor Axios faceva redirect a pagina intera su *qualunque* 401, incluso quello di un
  login con password sbagliata — ora solo se la richiesta aveva un token allegato (sessione
  scaduta).
- **Pulsante Annulla mancante per il Cliente**: era condizionato a `isStaff`, mai aggiornato
  dopo la riapertura di RBAC-002 al Cliente (cutoff 2h) — rimossa la condizione.

dotnet test 31/31 verdi, `npm run build`/`tsc --noEmit` puliti.

Prossimo passo — **Fase 8 (Rilascio)**: aggiornare questo file con gli URL definitivi (fatto in
questo aggiornamento), commit finale con tag `v1.0.0`, documentare le credenziali Admin iniziali
in luogo sicuro (non nel repo), aggiornare il tracker con gli stati finali (fatto).

---

### Storico — Fase 5-6-7 (26-27/08/2026, per riferimento)

**Fase 5** (26-27/08): risolti i 5 bug della checklist manuale di Fabio (`Fix Fase 5.txt`) — 403
del Cliente su prenotazioni (endpoint sbagliato), pulsanti Staff visibili su Zone/Postazioni/Fasce
quando non dovevano, vincolo "una prenotazione al giorno" che bloccava Staff/Admin su prenotazioni
per conto cliente (richiesta una **migration EF Core**, applicata a mano su Railway via
`railway connect Postgres` + `psql`, stesso procedimento di FIX-009), campo `NomeCliente`
aggiunto. Poi chiuso il backlog rimasto in sospeso da sessioni precedenti: GAP-001 (UI creazione
utenti — pagina pubblica `/register` + bottone Admin), RBAC-002 (cutoff 2h per annullo/modifica
self-service Cliente, non applicato ad Admin/Staff), AUDIT-001 (log attività esteso a
Zone/Postazioni/Fasce), NAMING-001-residuo (rinominato `FasciaOrariaController.cs`), DEAD-CODE-001
(filtro postazioni disponibili). Emersi altri 2 bug durante la ri-verifica: filtro
`GetPostazioniPerZonaAsync` troppo restrittivo, Fasce Orarie non filtrate per giorno in creazione
prenotazione — entrambi corretti.

**Fase 6** (27/08): deploy frontend su Vercel (root directory `gestora-frontend`,
`VITE_API_URL` verso Railway), CORS aggiornato su Railway con l'URL Vercel.

**Fase 7** (27/08): testing integrato completo sui 3 ruoli in produzione. 2 bug emersi e risolti
(vedi sopra, "404 Vercel" e "pulsante Annulla Cliente").

⚠️ **Nota di processo (dalla Fase 5)**: durante una sessione precedente le modifiche erano state
fatte per errore a working-tree su branch `main` invece di `dev` (poi corretto, nessuna perdita).
Controllare sempre `git status`/branch corrente a inizio sessione prima di modificare file.

---

### Storico — Fase 4 (25/08/2026, per riferimento)
**FASE 4 COMPLETATA — backend verificato in produzione.**

Checklist Fase 4 eseguita con token Admin reale via Postman:
- `GET /api/Zona/get-all-zone` → 404 "Nessuna zona trovata" (comportamento noto, vedi FIX-007)
- `GET /api/FasceOrarie/fasce-attive` → 200 `[]` (conferma che FIX-003 tiene anche in produzione)
- `GET /api/Dashboard/giornaliera?data=2026-08-25` → 200 con tutti i contatori a zero (l'aggregato
  non soffre dell'anti-pattern di FIX-007)
- Log Railway del servizio .NET puliti, nessuna eccezione durante le chiamate di verifica

Nessun fix applicato in questa sessione: FIX-007 resta aperto, da decidere in Fase 5 se e come
sistemarlo prima di collegare il frontend (vedi `BACKEND_FIX_TODO.md`).

Prima di partire con la Fase 4 è stato anche riallineato il repo, rimasto disallineato dalla
sessione del 14/08: `dev` era avanti di 2 commit non ancora mergiati su `main` (chiusura Fase 3),
e la cartella `.vs/` di Visual Studio non era esclusa dal tracking (`.gitignore` aveva la riga
scritta come commento `# .vs/` invece che come regola attiva — corretto in `.vs/`). Mergiati
`dev` → `main` (PR #3) e poi `main` → `dev`, così i due branch sono di nuovo allineati.

Prossimo passo: **FASE 5 — fix/verifica frontend contro l'URL Railway**. È qui che va deciso
cosa fare di FIX-007 (il frontend gestisce già le liste vuote come errore o come stato vuoto?).

---

### Storico — Fase 3 (14/08/2026, per riferimento)
**FASE 3 COMPLETATA — backend online su Railway.**

URL produzione backend: `https://gestora-project-production.up.railway.app`
Progetto Railway: `romantic-enthusiasm` (environment `production`), contiene DUE servizi:
`Postgres` e il servizio .NET collegato al repo GitHub `Gestora-Project`, branch `main`.

### Causa del blocco della sessione precedente (risolta)

La variabile `ConnectionStrings__DefaultConnection` non si risolveva perché **database e
applicazione erano stati creati in due progetti Railway distinti**. I riferimenti tra variabili
(`${{Postgres.PGHOST}}`) funzionano solo tra servizi dello **stesso progetto** — per questo
`Postgres` non compariva nell'autocomplete. Nessun problema di sintassi né di permessi.
Soluzione: ricreato il servizio .NET dentro il progetto del database; il progetto orfano è stato
eliminato. Regola da ricordare: **su Railway un progetto = un'applicazione con tutti i suoi
servizi**, non un servizio per progetto.

### Modifiche al codice di questa sessione (commit `4801060`, mergiato su main via PR)

Tutte in `GestoraWebApi/Program.cs` salvo dove indicato:
1. **Fail-fast sulla configurazione**: se `ConnectionStrings:DefaultConnection` o
   `JwtSettings:Secret` mancano, l'avvio si ferma con un messaggio esplicito (prima l'errore
   emergeva come `The ConnectionString property has not been initialized` dentro `RoleSeeder`,
   illeggibile). Incluso il controllo di lunghezza minima 256 bit del segreto JWT.
2. **Connection resiliency** (`EnableRetryOnFailure`, 5 tentativi / 10s): la rete privata tra
   container non è raggiungibile nei primi secondi dopo l'avvio. Verificato che nel progetto non
   ci sono transazioni esplicite (`BeginTransaction`), quindi l'opzione è sicura.
3. **Endpoint `/health`** (`AddHealthChecks` + `MapHealthChecks`, nessun pacchetto aggiuntivo),
   impostato come Healthcheck Path su Railway: un deploy rotto ora risulta fallito invece di
   andare in crash loop silenzioso.
4. **Serilog**: sink su file spostato in `appsettings.Development.json`; in produzione solo
   console (il filesystem del container è effimero, Railway raccoglie lo stdout).
5. `appsettings.Development.json` **rimosso da `.gitignore` e versionato** — non contiene più
   segreti (sono negli User Secrets dal 13/08) ed è configurazione per ambiente. Attenzione: la
   configurazione .NET sovrascrive gli array **per posizione**, quindi il sink Console va
   riconfermato all'indice 0 in quel file o si perde.

### Configurazione Railway del servizio .NET (per riferimento)

- Source: repo `Gestora-Project` (NON `GestoraWebApi`, repo vecchio/obsoleto), branch `main`,
  Root Directory `GestoraWebApi`
- Build: Custom Build Command `dotnet publish GestoraWebApi.csproj -c Release -o out`
  (il default builda l'intera solution, test inclusi, e fallisce)
- Deploy: Healthcheck Path `/health`
- Variabili: `ConnectionStrings__DefaultConnection` (composta con riferimenti
  `${{Postgres.PGHOST}}` / `PGPORT` / `PGDATABASE` / `PGUSER` / `PGPASSWORD`, creati con
  l'autocomplete del campo — incollati da fuori NON si attivano), `JwtSettings__Secret`,
  `AllowedOrigins__0=http://localhost:5173` (URL Vercel da aggiungere in Fase 6),
  `PORT=8080`, `ASPNETCORE_URLS=http://0.0.0.0:${{PORT}}`

### Verifiche eseguite in produzione (14/08/2026)

- build Railway ok, `[1/1] Healthcheck succeeded`
- `GET /health` da internet → 200 `Healthy`
- `GET /api/Zona/get-all-zone` senza token → 401 (autenticazione attiva)
- primo Admin creato con `POST /api/AuthenticationUser/seed-admin` (endpoint autobloccante)
- login in produzione → token JWT valido (373 caratteri)
- chiamata autenticata al DB → risposta dal service (404 su lista vuota, vedi FIX-007)

> Nota: la rotta base dei controller è `/api/[nome classe controller]`, quindi gli endpoint di
> autenticazione stanno sotto `/api/AuthenticationUser/...`, non `/api/Auth/...` come scritto in
> vecchia documentazione.

### Trovato durante la verifica: FIX-007 (registrato in BACKEND_FIX_TODO.md)

Sei endpoint in `ZonaController`, `PostazioneController` e `PrenotazioneController` restituiscono
404 invece di `200 []` su lista vuota — stesso anti-pattern già corretto con FIX-003 sulle sole
fasce orarie. Si manifesta **sempre su un DB di produzione appena creato**. Da valutare in Fase 5:
il frontend potrebbe mostrare errori dove dovrebbe mostrare uno stato vuoto.

### Prossimo passo: FASE 4 — verifica backend in produzione

Con il token Admin: `GET /api/Zona/get-all-zone`, `GET /api/FasceOrarie/fasce-attive`,
`GET /api/Dashboard/giornaliera?data=`, controllo dei log Railway. Poi FASE 5 (frontend contro
l'URL Railway) — è lì che va deciso cosa fare di FIX-007.

### Iter di progetto — SEQUENZA OBBLIGATORIA (aggiornata post SA Assessment)
1. ~~Completare il frontend~~ ✅ FATTO
2. ~~Fix backend~~ ✅ FATTO (vedi `BACKEND_FIX_TODO.md` e `PIANO_RILASCIO.md`)
3. ~~Test backend (dotnet test + verifica manuale Swagger)~~ ✅ FATTO (31/31 test verdi)
4. ~~Deploy backend su Railway~~ ✅ FATTO
5. ~~Verifica backend in produzione~~ ✅ FATTO
6. ~~Fix/verifica frontend contro Railway URL~~ ✅ FATTO
7. ~~Deploy frontend su Vercel~~ ✅ FATTO (`https://gestora-project-xi.vercel.app`)
8. ~~Testing integrato su produzione~~ ✅ FATTO
9. Rilascio v1.0.0 ← **prossimo passo**

### File fix backend — LEGGERE AD OGNI SESSIONE
Ogni problema backend trovato va registrato in:
`C:\Users\Carlo Taranto\Progetti_Tech\02_Personali\Gestora\BACKEND_FIX_TODO.md`

### Nota React 19
Il progetto è stato inizializzato con React 19 (non 18 come da piano). Non è un problema — React 19 è stabile.

### Prossima cosa da fare (in ordine)

1. ~~Inizializzare progetto Vite + React + TypeScript~~ ✅ FATTO (React 19)
2. ~~Creare struttura cartelle src/ + Configurare Prettier (ESLint già presente)~~ ✅ FATTO
3. ~~Integrare shadcn/ui + Tailwind CSS~~ ✅ FATTO
4. ~~Setup React Router v6 con route protette per ruolo~~ ✅ FATTO
5. ~~Setup Axios con interceptor JWT (attach token + refresh/logout su 401)~~ ✅ FATTO
6. ~~Setup React Query (TanStack Query v5)~~ ✅ FATTO
7. ~~Pagina Login + hook useAuth con Context API~~ ✅ FATTO
8. ~~Layout shell: sidebar, header, area contenuto~~ ✅ FATTO
9. ~~Pagina Dashboard (consuma GET /Dashboard/giornaliera e /settimanale)~~ ✅ FATTO
10. ~~CRUD Zone, Postazioni, Fasce Orarie (Admin)~~ ✅ FATTO
11. ~~Gestione Prenotazioni (Staff + Cliente)~~ ✅ FATTO
12. ~~Pannello Admin utenti (consuma endpoint Auth)~~ ✅ FATTO
13. ~~Deploy su Vercel~~ ✅ FATTO


---

## Contesto progetto

Applicativo per la gestione organizzativa di attivita commerciali (ristoranti, pub, pizzerie).
Funzionalita principali: gestione postazioni, fasce orarie, prenotazioni online con assegnazione automatica tavoli.

### Stack tecnologico completo

BACKEND (completato):
- ASP.NET Core 9, C#, Entity Framework Core 9, PostgreSQL (Npgsql)
- Auth: ASP.NET Identity + JWT Bearer, 3 ruoli: Admin, Staff, Cliente
- Extra: Quartz.NET 3.15, FluentValidation 11, AutoMapper, Serilog, IMemoryCache
- Test: xUnit + Moq, 18 test unitari (tutti verdi)
- Deploy: Railway (backend + PostgreSQL)

FRONTEND (completato — deploy pendente):
- React 19 + TypeScript + Vite
- shadcn/ui + Tailwind CSS
- React Query (TanStack Query v5)
- React Hook Form + Zod
- React Router v6
- Axios con interceptor JWT
- Deploy: Vercel

MOBILE (fase futura):
- React Native + Expo


---

## Riferimento tecnico per area

- Backend (architettura, endpoint reali, note tecniche): `GestoraWebApi\CLAUDE.md`
- Frontend (stack, pattern CRUD, routing): `gestora-frontend\CLAUDE.md`

---

## Come affiancarmi

Sono un developer con 4 anni di esperienza su Dynamics 365 / Power Platform.
Sto costruendo questo progetto per fare uno switch di carriera verso il full stack.
Obiettivo dichiarato: trovare un'azienda che mi assuma come full stack developer.

Come mi devi affiancare (aggiornato 13/08/2026 — vedi nota sotto):
- Ruolo: senior developer che lavora insieme a me, io sono un middle developer che impara
- Implementa direttamente le modifiche di codice, frontend incluso — niente più procedura guidata
  passo-passo dove indichi lo step e aspetti che lo scriva io. La guida passo-passo ha rallentato
  troppo su fix piccoli/meccanici; l'ho chiesto io di cambiare approccio.
- Quando c'è un concetto nuovo o non ovvio, spiegalo comunque (breve, non un tutorial) — ma senza
  bloccare l'implementazione in attesa che lo scriva io
- Indica quando qualcosa non e production-ready e perche
- Suggerisci le best practice del settore, non solo la soluzione che funziona
- Obiettivo finale: progetto portfolio-ready che dimostri competenze full stack reali
- Il backend e completato — non modificarlo salvo regressions, bug critici, o richieste esplicite
  di cambio architetturale (es. RBAC)
- Ricorda comunque che non ho esperienza pregressa sul frontend — se introduci un pattern nuovo
  spiegalo, solo senza il cerimoniale a step

> Nota: fino al 13/08/2026 questo file richiedeva un protocollo di affiancamento passo-passo
> rigido (mostrare solo esempi parziali, un'istruzione alla volta, mai scrivere codice al posto
> di Fabio). Rimosso su sua richiesta esplicita per accelerare i fix piccoli — vedi
> TrackAttività_Gestora.xlsx, foglio Appunti e Step, per il contesto.

---

## Tracker attivita — PRIORITA MASSIMA

Il tracker **unico e ufficiale** si trova in: `TrackAttività_Gestora.xlsx` (stessa cartella di
questo file). Claude lo legge e aggiorna tramite PowerShell + Excel COM — nessun allegato
necessario.

> Non esistono altri tracker validi. Se in futuro compare un secondo file xlsx/md che sembra un
> tracker di progetto (è già successo con `Gestora_Piano_Operativo.xlsx`, generato da una
> sessione Claude esterna e rimosso il 12/08/2026 perché duplicava e disallineava lo stato),
> non aggiornarlo — segnalarlo a Fabio come possibile doppione prima di usarlo.

### Procedura standard di ripresa sessione

1. Leggere il blocco "LEGGI QUESTO PRIMA DI TUTTO — STATO SESSIONE" in cima a questo file
2. Leggere il foglio **Appunti e Step** di `TrackAttività_Gestora.xlsx`
3. `git status` — se ci sono modifiche non committate da prima, capire cosa sono prima di
   assumere che siano lavoro "in corso"
4. Se si riprende un task a metà: eseguire la skill `verifica-gestora` per un'evidenza fresca
   (build/test) invece di fidarsi dell'ultimo stato scritto a mano
5. Procedere con il lavoro richiesto

### Protocollo sessione (tracker)

1. Inizio — leggere questo file + foglio Appunti e Step del tracker
2. Dopo ogni implementazione — aggiornare stato nel tracker da "Da fare" a "Completato"
3. Nuova decisione architetturale — aggiungere riga in "Note e Decisioni" nel tracker
4. Fine sessione — aggiornare il blocco "LEGGI QUESTO PRIMA DI TUTTO" qui sopra + commit Git

### Protocollo commit Git

- Il commit e il push li fa SEMPRE Fabio, mai Claude
- Claude deve fornire il messaggio di commit con l'elenco delle modifiche effettuate
- Formato messaggio:

feat: breve descrizione cosa hai fatto

Se incompleto:
feat: WIP - descrizione cosa stavi facendo (da completare)

### Operazioni delicate — REGOLA OBBLIGATORIA

Per operazioni che coinvolgono sicurezza o configurazione sensibile (es. .env, .gitignore, credenziali, variabili d'ambiente, rimozione file da tracking Git) Claude deve:
1. Spiegare cosa sta per succedere e perché
2. Passare le istruzioni a Fabio che le esegue
3. MAI eseguire queste operazioni autonomamente


---

## Protocollo aggiornamento tracker — REGOLE OBBLIGATORIE

Ogni volta che si aggiorna il tracker applicare SEMPRE queste regole su TUTTI i fogli:

### Fogli da aggiornare ad ogni sessione
1. Dashboard — aggiornare la data "Aggiornato: gg/mm/aaaa"
2. Appunti e Step — aggiornare data header + stati task
3. Roadmap — aggiornare stati
4. Piano di Sviluppo — aggiornare stato settimana + date inizio/fine
5. Fix e Bug — aggiornare stati
6. Controllers — aggiungere SUBITO ogni nuovo endpoint o componente implementato

### Colori stati (applicare SEMPRE, nessuna eccezione)
- Completato     → verde         (#C6EFCE)
- Da fare        → giallo        (#FFEB9C)
- Parziale       → giallo        (#FFEB9C)
- In corso       → giallo        (#FFEB9C)
- Non necessario → grigio        (#D9D9D9)
- Pianificato    → azzurro       (#DDEBF7)
- Futuro         → grigio chiaro (#EDEDED)

### Regole generali
- Aggiornare TUTTI i fogli, non solo uno
- Quando si completa un task aggiornarlo su Appunti, Roadmap, Piano di Sviluppo e Fix e Bug contemporaneamente
- Le decisioni architetturali vanno aggiunte SUBITO in "Note e Decisioni" in Appunti e Step
- La data va aggiornata in Dashboard e nell'header di Appunti e Step

## graphify

This project has a knowledge graph at graphify-out/ with god nodes, community structure, and cross-file relationships.

Rules:
- For codebase questions, first run `graphify query "<question>"` when graphify-out/graph.json exists. Use `graphify path "<A>" "<B>"` for relationships and `graphify explain "<concept>"` for focused concepts. These return a scoped subgraph, usually much smaller than GRAPH_REPORT.md or raw grep output.
- If graphify-out/wiki/index.md exists, use it for broad navigation instead of raw source browsing.
- Read graphify-out/GRAPH_REPORT.md only for broad architecture review or when query/path/explain do not surface enough context.
- After modifying code, run `graphify update .` to keep the graph current (AST-only, no API cost).
