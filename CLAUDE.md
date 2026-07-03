# Progetto Gestora — Full Stack

## LEGGI QUESTO PRIMA DI TUTTO — STATO SESSIONE

Ultima sessione: 03/07/2026
Ultima cosa fatta: FASE 1 parziale — FIX-002 completato. GET get-all-fasce + PATCH update-stato/{id} su FasceOrarie. 21 test verdi. Fix regressione PrenotazioniServiceTests (mock IHttpContextAccessor).
Prossima cosa: FIX-004 — validazione unicità (OrarioInizio, GiornoSettimana) in FasciaOrariaService.

### Iter di progetto — SEQUENZA OBBLIGATORIA (aggiornata post SA Assessment)
1. ~~Completare il frontend~~ ✅ FATTO
2. Fix backend (vedi `BACKEND_FIX_TODO.md` e `PIANO_RILASCIO.md`)
3. Test backend (dotnet test + verifica manuale Swagger)
4. Deploy backend su Railway
5. Verifica backend in produzione
6. Fix/verifica frontend contro Railway URL
7. Deploy frontend su Vercel
8. Testing integrato su produzione
9. Rilascio v1.0.0

### File fix backend — LEGGERE AD OGNI SESSIONE
Ogni problema backend trovato va registrato in:
`C:\Users\Carlo Taranto\Personale\GestoraProject\BACKEND_FIX_TODO.md`

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
13. Deploy su Vercel


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

## Backend — riferimento rapido per il frontend

### URL base API

- Locale: https://localhost:{porta}/api
- Produzione: {URL Railway — da aggiornare dopo deploy}

### Autenticazione

- JWT Bearer — il token si ottiene da POST /api/AuthenticationUser/login
- Header da allegare ad ogni richiesta autenticata: Authorization: Bearer {token}
- Formato date in tutti gli endpoint: yyyy-MM-dd (es. 2026-05-14)
- Errori: formato unificato { statusCode, message, errors: [{field, error}] }

### Ruoli disponibili

- Admin: accesso completo
- Staff: operativo (conferma/completa/annulla prenotazioni, lettura dati)
- Cliente: limitato (gestisce solo le proprie prenotazioni)

### Endpoint per area

Auth:
- POST /api/AuthenticationUser/register
- POST /api/AuthenticationUser/login  <-- restituisce il JWT
- POST /api/AuthenticationUser/assign-role
- DELETE /api/AuthenticationUser/remove-role
- GET /api/AuthenticationUser/get-users
- GET /api/AuthenticationUser/get-user/{id}
- PUT /api/AuthenticationUser/update-user/{id}
- DELETE /api/AuthenticationUser/delete-user/{id}
- POST /api/AuthenticationUser/reset-password/{id}

Dashboard (Admin + Staff):
- GET /api/Dashboard/giornaliera?data=yyyy-MM-dd
- GET /api/Dashboard/settimanale?dataInizio=yyyy-MM-dd

Zone:
- GET /api/Zona/get-zone-attive
- GET /api/Zona/get-all-zone
- GET /api/Zona/get-zona/{id}
- POST /api/Zona/crea-zona
- PUT /api/Zona/update-zona
- PATCH /api/Zona/update-stato/{id}?attiva=true
- DELETE /api/Zona/delete-zona/{id}

Postazioni:
- GET /api/Postazione/get-postazioni-attive
- GET /api/Postazione/get-postazioni-disponibili
- GET /api/Postazione/get-postazioni-per-zona
- GET /api/Postazione/get-postazione-id
- POST /api/Postazione/crea-postazione
- PUT /api/Postazione/update-postazione
- PUT /api/Postazione/associa-postazione-a-zona
- DELETE /api/Postazione/delete-postazione

Fasce Orarie:
- GET /api/FasciaOraria/fasce-attive
- GET /api/FasciaOraria/fasce-per-giorno?giorno={0-6}  (0=Dom, 1=Lun, ..., 6=Sab)
- GET /api/FasciaOraria/fasce-disponibili?fasciaId={id}&data=yyyy-MM-dd
- POST /api/FasciaOraria/crea-fascia
- PUT /api/FasciaOraria/update-fascia
- DELETE /api/FasciaOraria/delete-fascia

Prenotazioni:
- POST /api/Prenotazione/crea-prenotazione
- POST /api/Prenotazione/check-disponibilita  <-- pubblico, no auth
- GET /api/Prenotazione/get-prenotazione?id={id}
- GET /api/Prenotazione/get-all-prenotazioni  (con filtri opzionali)
- GET /api/Prenotazione/get-prenotazioni-by-data?data=yyyy-MM-dd
- PUT /api/Prenotazione/update-prenotazione
- DELETE /api/Prenotazione/delete-prenotazione
- PATCH /api/Prenotazione/conferma-prenotazione?id={id}
- PATCH /api/Prenotazione/completa-prenotazione?id={id}
- PATCH /api/Prenotazione/annulla-prenotazione?id={id}


---

## Come affiancarmi

Sono un developer con 4 anni di esperienza su Dynamics 365 / Power Platform.
Sto costruendo questo progetto per fare uno switch di carriera verso il full stack.
Obiettivo dichiarato: trovare un'azienda che mi assuma come full stack developer.

Come mi devi affiancare:
- Ruolo: senior developer che mi guida, io sono un middle developer che impara
- NON scrivere il codice al posto mio — guidami verso la soluzione
- Spiega sempre il concetto prima di mostrare il codice
- Fai domande per farmi ragionare: "cosa pensi che dovremmo fare qui?"
- Mostra esempi parziali o pattern, poi lascia che io li completi
- Se sbaglio, non correggere direttamente — dimmi dove guardare e perché
- Indica quando qualcosa non e production-ready e perche
- Suggerisci le best practice del settore, non solo la soluzione che funziona
- Dopo ogni implementazione chiedi: "cosa hai capito di questo passaggio?"
- Obiettivo finale: progetto portfolio-ready che dimostri competenze full stack reali
- Il backend e completato — non modificarlo salvo regressions o bug critici
- IMPORTANTE: ricordati che non ho esperienze sul frontend non dare nulla per scontato

### Regole di affiancamento — OBBLIGATORIE

- Ogni concetto nuovo va spiegato partendo da zero, senza dare nulla per scontato
- Prima di chiedere a Fabio di scrivere codice, mostra sempre un esempio concreto e spiegalo riga per riga
- Un passo alla volta: non dare mai più di una istruzione contemporaneamente
- Se Fabio dice "non capisco", fermati e rispiega con parole diverse e un esempio più semplice
- Non usare termini tecnici senza spiegarli la prima volta che compaiono
- Quando si introduce un pattern nuovo (es. callback, async, hooks), spiegare il PERCHÉ prima del COME
- Se Fabio commette un errore, spiegare cosa è andato storto e perché, non solo come correggerlo
- Ricordare sempre: tutto quello che facciamo è la prima volta per Fabio sul frontend

### Protocollo panoramica step — OBBLIGATORIO

Prima di iniziare qualsiasi nuovo step, fornire SEMPRE una panoramica che include:
1. **Cosa facciamo**: obiettivo dello step in una riga
2. **Perché**: motivazione tecnica o di business
3. **File coinvolti**: elenco dei file che toccheremo
4. **Concetti chiave**: se lo step introduce pattern nuovi, nominarli qui
5. **Risultato atteso**: cosa sarà diverso/funzionante al termine

Solo dopo la panoramica procedere con il codice.

---

## Tracker attivita — PRIORITA MASSIMA

Il tracker si trova in: `C:\Users\Carlo Taranto\Personale\GestoraProject\TrackAttività_Gestora.xlsx`
Claude lo legge e aggiorna tramite PowerShell + Excel COM — nessun allegato necessario.

### Protocollo sessione

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
