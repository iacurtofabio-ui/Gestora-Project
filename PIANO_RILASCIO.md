# Piano di Rilascio — Gestora
# Documento: Solution Architecture Assessment
# Data: 11/06/2026
# Versione: 1.0

---

## STATO AGGIORNATO — 13/08/2026

> Il resto del documento (sezioni 1-8) è l'assessment originale dell'11/06/2026, lasciato
> intatto come riferimento storico. Questo blocco riporta lo stato reale delle fasi.

- **FASE 1 — Fix e stabilizzazione backend: ✅ COMPLETATA.** Tutti i fix di §6 chiusi (dettaglio
  in `BACKEND_FIX_TODO.md`, sezione "Fix completate"). In più, non pianificato in origine: SEC-001
  (migrazione a `dotnet user-secrets`) e una ridefinizione del perimetro RBAC di Staff/Cliente
  (RBAC-001, con RBAC-002 aperto per una regola di cutoff self-service — vedi
  `BACKEND_FIX_TODO.md`).
- **FASE 2 — Test backend: ✅ COMPLETATA.** `dotnet test` 28/28 verdi. Verifica manuale via
  Swagger/curl con token Admin reale su tutti i fix: FIX-004 A/B/C, CACHE-001, FIX-001, CORS —
  tutti confermati. Durante questa fase è emerso e risolto un bug frontend non pianificato
  (gestione ruoli multipli, vedi `gestora-frontend/CLAUDE.md`).
- **FASE 3 — Deploy backend su Railway: ✅ COMPLETATA (14/08/2026).**
  Backend online su `https://gestora-project-production.up.railway.app`.
  PostgreSQL provisionato, migration EF Core + script Quartz applicati, servizio .NET collegato al
  repo GitHub `Gestora-Project` (branch `main`, Root Directory `GestoraWebApi`, Custom Build Command
  `dotnet publish GestoraWebApi.csproj -c Release -o out` — necessario perché il default builda
  anche `GestoraWebApi.Tests`, mai restorato, e fallisce). `main` allineato a `dev`.
  **Causa del blocco del 14/08 mattina**: database e applicazione erano in due *progetti* Railway
  distinti; i riferimenti tra variabili (`${{Postgres.PGHOST}}`) funzionano solo tra servizi dello
  stesso progetto. Risolto ricreando il servizio .NET nel progetto del database ed eliminando quello
  orfano. In più, non pianificato in origine: irrigidimenti di avvio in `Program.cs` (fail-fast sulla
  configurazione, connection resiliency Npgsql, endpoint `/health` usato come Healthcheck Path
  Railway) e log su console-only in produzione. Dettaglio in `CLAUDE.md`, blocco "STATO SESSIONE".
  Verificato in produzione: `/health` 200, 401 senza token, primo Admin creato via `seed-admin`,
  login con token JWT valido.
  **Trovato durante la verifica**: FIX-007 (404 invece di `200 []` su lista vuota in sei endpoint di
  Zona/Postazione/Prenotazione) — registrato in `BACKEND_FIX_TODO.md`, da valutare in Fase 5.
- **FASE 4 — Verifica backend in produzione: ✅ COMPLETATA (25/08/2026).**
  Checklist eseguita con token Admin reale via Postman: `GET /api/Zona/get-all-zone` → 404
  (comportamento noto, FIX-007), `GET /api/FasceOrarie/fasce-attive` → 200 `[]` (FIX-003 tiene
  in produzione), `GET /api/Dashboard/giornaliera` → 200 con contatori a zero. Log Railway del
  servizio .NET puliti, nessuna eccezione. Nessun fix di codice applicato in questa fase.
  Prima di iniziare, riallineato il repo: `dev` era avanti di 2 commit non mergiati su `main`
  (chiusura Fase 3) e il `.gitignore` non escludeva davvero `.vs/` (riga scritta come commento).
  Sistemato con merge `dev` → `main` (PR #3) e `main` → `dev`.
- **FASE 5 — Verifica e fix frontend: IN CORSO (avviata 25/08/2026).**
  `.env.local` puntato su Railway. Deciso di chiudere FIX-007 lato backend (stesso pattern di
  FIX-003), non lato frontend — vedi `BACKEND_FIX_TODO.md`. Checklist di test manuale eseguita
  fino al blocco Admin CRUD (Zone, Postazioni, Fasce Orarie, Prenotazioni Admin/Staff); trovati e
  risolti in sessione tre problemi non pianificati: FIX-008 (fasce disattivate irraggiungibili in
  UI), UI-001 (etichetta "In Corso" fraintesa), FIX-009 (vincolo una-prenotazione-al-giorno non
  escludeva le annullate — richiesta una migration EF Core, applicata a mano su Railway via
  tunnel Railway CLI + `psql`, verificata). Restano da testare: ruolo Staff, ruolo Cliente,
  sicurezza/sessione (route protette, logout su token scaduto, assenza errori CORS in console).
- **FASE 6-8: DA FARE.**
- **Decisione 13/08/2026**: il debito tecnico residuo (RBAC-002, AUDIT-001, NAMING-001-residuo —
  dettaglio in `BACKEND_FIX_TODO.md`, sezione "Backlog post-v1.0") non blocca la Fase 3 né le
  successive. Si affronta dopo il rilascio v1.0.0, per non far slittare il deploy inseguendo
  rifiniture non bloccanti.

---

## 1. Executive Summary

Il progetto Gestora è composto da un backend ASP.NET Core 9 e un frontend React 19.
Il codice di entrambi i livelli è funzionante in locale. Nessuno dei due è ancora in produzione.

L'analisi del codice sorgente ha rilevato problemi che vanno oltre quelli già registrati
in BACKEND_FIX_TODO.md. In particolare, è stato identificato un blocco critico per il
deploy in produzione non precedentemente registrato: la configurazione CORS hardcoded
sul solo localhost impedirà qualsiasi comunicazione tra Vercel e Railway.

Il documento descrive la sequenza di lavoro raccomandata per portare il prodotto
in produzione in modo stabile e professionale.

---

## 2. Analisi Stato Attuale

### 2.1 Backend

| Area | Stato | Note |
|---|---|---|
| Architettura | OK | Clean Architecture, Repository+Service pattern corretto |
| Autenticazione JWT | OK | ASP.NET Identity + JWT Bearer, 3 ruoli |
| Validazione input | OK | FluentValidation su tutti i DTO |
| Error handling | OK | ExceptionMiddleware centralizzato, risposta uniforme |
| Logging | OK | Serilog con output su console e file |
| Cache | OK | IMemoryCache 30 min con invalidazione su write |
| Jobs schedulati | OK | Quartz.NET con persistent store su PostgreSQL |
| Unit test | PARZIALE | 18 test su 3 service — nessun test su controller, auth, dashboard |
| Configurazione produzione | DA FARE | Secrets vuoti in appsettings.json (correto), Railway env vars non configurate |
| CORS | CRITICO | Hardcoded su localhost:5173 — blocca produzione |
| HTTPS redirect | DA DECIDERE | Commentato nel codice con nota aperta |
| Migration DB | DA FARE | Le migration EF Core devono girare su Railway al primo avvio |

### 2.2 Frontend

| Area | Stato | Note |
|---|---|---|
| Setup e infrastruttura | OK | Vite, React 19, TypeScript, shadcn/ui, Tailwind |
| Autenticazione | OK | JWT in localStorage, interceptor Axios, AuthContext con id/email/role |
| Routing role-based | OK | Route protette per ruolo, redirect per ruolo al login |
| CRUD Zone/Postazioni/FasceOrarie | OK | Completo con modal, ConfirmDialog, error handling |
| Gestione Prenotazioni (Admin/Staff) | OK | Funzionante, filtri, azioni stato |
| Gestione Prenotazioni (Cliente) | BLOCCATO | Mostra messaggio 403 — pending FIX-006 backend |
| Pannello Admin Utenti | OK | Lista, modifica, ruoli, reset password, elimina |
| Build produzione | OK | npm run build verde, bundle 600 kB |
| Variabili ambiente | DA FARE | VITE_API_URL non ancora configurato per produzione |
| Test | ASSENTE | Nessun test unitario o di integrazione frontend |

---

## 3. Problemi Rilevati

Classificazione: CRITICO > ALTO > MEDIO > BASSO

### CRITICO — Blocca il deploy in produzione

**CORS-001 — CORS hardcoded su localhost**
File: GestoraWebApi/Program.cs riga 107
Problema: policy.WithOrigins("http://localhost:5173") è scritto fisso nel codice.
In produzione il frontend sarà su https://{nome}.vercel.app — tutte le chiamate
API saranno bloccate dal browser con errore CORS.
Soluzione: leggere gli origin consentiti da configurazione (appsettings / env var Railway)
e aggiungere l'URL Vercel alla policy in produzione.
Non registrato in BACKEND_FIX_TODO.md — aggiungere.

### ALTO — Compromette funzionalità core

**FIX-006 — Nessun endpoint prenotazioni per Cliente**
Il Cliente ottiene 403 su get-all-prenotazioni. La funzionalità è bloccata per un intero ruolo.
Soluzione: aggiungere get-mie-prenotazioni filtrato per UserId dal JWT, oppure
modificare get-all-prenotazioni per filtrare automaticamente per ruolo.

**FIX-005 — PrenotazioneDTO manca FasciaOrariaId e ZonaId**
La modifica di una prenotazione non può funzionare senza questi campi nel DTO.
La funzionalità di edit è incompleta.

**FIX-003 — FasceOrarie restituisce 404 invece di array vuoto**
FasceOrarieController.GetFasceAttive() riga 57: if (!fasce.Any()) return NotFound(...)
Comportamento scorretto: 404 significa "risorsa non trovata", non "lista vuota".
La lista vuota è un risultato valido e deve tornare Ok([]).
Stesso pattern presente anche in fasce-per-giorno (riga 78) — da correggere insieme.

### MEDIO — Funzionalità incomplete o degradate

**FIX-002 — Nessun endpoint get-all fasce orarie**
L'admin non può vedere né riattivare le fasce disattivate dalla UI.

**FIX-004 — Nessuna validazione unicità fascia oraria**
Possibile creare fasce duplicate sullo stesso giorno e orario.

### BASSO — Qualità del codice e UX

**FIX-001 — Messaggio errore zona inesistente non parlante**
Il messaggio di Entity Framework non è comprensibile per l'utente.

**NAMING-001 — File PrenotazioneDTO1.cs**
Il file GestoraWebApi/Services/Prenotazioni/DTOs/PrenotazioneDTO1.cs contiene
la classe PrenotazioneCreateDTO. Il nome del file non corrisponde al contenuto.
Rinominare in PrenotazioneCreateDTO.cs per chiarezza.

---

## 4. Sequenza di Lavoro Raccomandata

La sequenza segue lo standard enterprise: stabilizzare il backend, deployarlo,
verificarlo, poi muoversi sul frontend, poi testing integrato su produzione.

```
FASE 1 — Fix e stabilizzazione backend
FASE 2 — Test backend
FASE 3 — Deploy backend (Railway)
FASE 4 — Verifica backend in produzione
FASE 5 — Verifica e fix frontend
FASE 6 — Deploy frontend (Vercel)
FASE 7 — Testing integrato su produzione
FASE 8 — Rilascio
```

---

## 5. Dettaglio Fasi

### FASE 1 — Fix e stabilizzazione backend

Ordine di esecuzione (dal più critico al meno critico):

1. CORS-001: rendere gli origin CORS configurabili da appsettings/env var
2. FIX-003: rimuovere NotFound su lista vuota in fasce-attive e fasce-per-giorno
3. FIX-006: aggiungere endpoint get-mie-prenotazioni per Cliente
4. FIX-005: aggiungere FasciaOrariaId e ZonaId a PrenotazioneDTO
5. FIX-002: aggiungere endpoint get-all-fasce + PATCH update-stato/{id}
6. FIX-004: aggiungere validazione unicità (OrarioInizio, GiornoSettimana)
7. FIX-001: migliorare messaggio errore zona inesistente
8. NAMING-001: rinominare PrenotazioneDTO1.cs
9. Decidere sulla HTTPS redirect (riga 163 Program.cs)

Nota su HTTPS: Railway espone già HTTPS sul proprio dominio tramite reverse proxy.
Il container interno non deve fare HTTPS redirect. Lasciare commentato è corretto.

### FASE 2 — Test backend

Eseguire i 18 unit test esistenti: dotnet test
Verificare manualmente via Swagger (localhost) tutti gli endpoint modificati:
- fasce-attive con DB vuoto → deve tornare 200 []
- fasce-per-giorno con DB vuoto → deve tornare 200 []
- get-mie-prenotazioni con token Cliente → deve tornare solo le proprie
- get-all-prenotazioni con token Admin → deve tornare tutte
- CORS: chiamata da browser con Origin http://localhost:5173 → OK
- Tutti gli endpoint delle fix applicate

### FASE 3 — Deploy backend su Railway

Prerequisiti prima del deploy:
- Connection string PostgreSQL Railway configurata come env var
- JWT Secret configurato come env var (mai in appsettings.json committato)
- CORS origin produzione configurato come env var (URL Vercel)
- Decidere se usare Railway Migrate automatica o run manuale (dotnet ef database update)

Checklist deploy:
1. Creare servizio PostgreSQL su Railway
2. Creare servizio .NET su Railway collegato al repo GitHub
3. Impostare variabili ambiente: ConnectionStrings__DefaultConnection, JwtSettings__Secret,
   AllowedOrigins (URL Vercel da inserire dopo il deploy frontend)
4. Verificare che le Quartz tables vengano create automaticamente al primo avvio
5. Eseguire seed-admin una sola volta per creare il primo utente Admin

### FASE 4 — Verifica backend in produzione

Prima di toccare il frontend, verificare che il backend Railway funzioni correttamente:
- Login con utente Admin → ottieni token
- GET /api/Zona/get-all-zone → lista zone
- GET /api/FasceOrarie/fasce-attive → 200 (anche se vuoto)
- GET /api/Dashboard/giornaliera?data=oggi → risposta dashboard
- Logs Railway visibili e senza errori di avvio

### FASE 5 — Verifica e fix frontend

Con l'URL Railway noto, aggiornare VITE_API_URL e testare in locale contro produzione:
- Creare .env.local con VITE_API_URL=https://{url-railway}/api
- Testare tutti i flussi: login, CRUD, prenotazioni
- Verificare che le chiamate API raggiungano Railway senza errori CORS
- Applicare eventuali fix frontend che emergono dai test

### FASE 6 — Deploy frontend su Vercel

Prerequisiti:
- Repo GitHub aggiornato con ultimo commit
- URL Railway backend confermato funzionante

Passi:
1. Collegare repo GitHub a Vercel
2. Impostare root directory: gestora-frontend
3. Impostare variabile ambiente: VITE_API_URL = https://{url-railway}/api
4. Deployare
5. Copiare URL Vercel (es. https://gestora-xyz.vercel.app)
6. Tornare su Railway e aggiornare CORS AllowedOrigins con l'URL Vercel
7. Fare un nuovo deploy Railway (o restart) per applicare la nuova variabile CORS

### FASE 7 — Testing integrato su produzione

Testare su ambiente reale (Vercel + Railway + PostgreSQL Railway) tutti i flussi:

Flusso Admin:
- Login → redirect a /dashboard
- Dashboard carica KPI
- CRUD Zone, Postazioni, Fasce Orarie (crea, modifica, elimina)
- Gestione Prenotazioni: visualizza, conferma, completa, annulla
- Pannello Utenti: visualizza, modifica, assegna ruoli, reset password, elimina

Flusso Staff:
- Login → redirect a /dashboard
- Dashboard, visualizzazione entità in sola lettura
- Prenotazioni: conferma, completa, annulla

Flusso Cliente:
- Login → redirect a /prenotazioni
- Visualizza solo le proprie prenotazioni
- Crea nuova prenotazione
- Annulla prenotazione propria

Flusso non autenticato:
- / → redirect a /login
- Route protetta senza token → redirect a /login
- Route con ruolo sbagliato → /unauthorized

### FASE 8 — Rilascio

- Aggiornare CLAUDE.md con URL Railway e URL Vercel
- Commit finale con tag di versione (es. v1.0.0)
- Documentare credenziali Admin iniziali in luogo sicuro (non nel repo)
- Aggiornare tracker con stati finali

---

## 6. Riepilogo Priorità Fix Backend

| Codice | Descrizione | Priorità | Fase |
|---|---|---|---|
| CORS-001 | CORS hardcoded localhost | CRITICO | Fase 1 (prima di tutto) |
| FIX-003 | 404 invece di [] su liste vuote | ALTO | Fase 1 |
| FIX-006 | Nessun endpoint prenotazioni Cliente | ALTO | Fase 1 |
| FIX-005 | PrenotazioneDTO manca FasciaOrariaId/ZonaId | ALTO | Fase 1 |
| FIX-002 | Nessun get-all fasce + PATCH stato | MEDIO | Fase 1 |
| FIX-004 | Nessuna validazione unicità fascia oraria | MEDIO | Fase 1 |
| FIX-001 | Messaggio errore zona inesistente | BASSO | Fase 1 |
| NAMING-001 | File PrenotazioneDTO1.cs mal nominato | BASSO | Fase 1 |

---

## 7. Note Architetturali

**Su HTTPS in produzione:**
Railway termina HTTPS a livello di reverse proxy. L'applicazione ASP.NET Core
gira su HTTP internamente. Non abilitare UseHttpsRedirection nel container Railway
perché causerebbe redirect loop. Il commento nel codice è corretto — lasciare così.

**Su Quartz.NET in produzione:**
Quartz usa PostgreSQL come persistent store. Al primo avvio tenterà di creare
le tabelle QRTZ_* se non esistono. Verificare che l'utente DB abbia permessi DDL,
oppure eseguire lo script SQL Quartz manualmente prima del primo avvio.

**Su JWT Secret in produzione:**
Il segreto JWT deve essere almeno 256 bit (32 caratteri). Non deve mai essere
committato nel repo. Usare esclusivamente Railway environment variables.

**Su Session/Token expiry:**
Il token ha scadenza 60 minuti (appsettings.json). Non c'è refresh token.
Allo scadere del token il frontend fa logout automatico (interceptor Axios su 401).
Per un portfolio è accettabile. In produzione reale si implementerebbe un refresh token.
