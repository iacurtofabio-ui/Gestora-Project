# Gestora

> **Documento di ingresso del progetto.** Si legge a inizio di ogni sessione.
> Cosa è aperto → `BACKLOG.md` · Come si fa una cosa → `RUNBOOK.md` · Com'è andata → `docs/archivio/STORICO_FASI.md`

---

## 1. Stato — aggiornato al 17/09/2026

**Il progetto è finito.** Ultima versione pubblicata: **`v1.0.6`**.

> ⚠️ **Migrazione hosting in corso** — Railway (backend + database) è offline dal 16/09/2026,
> trial scaduto. Nuova infrastruttura in `BACKLOG.md`, voce `OPS-006`.

| | |
|---|---|
| Backend | `https://gestora-api-emdvdqegg7g8gmaq.canadacentral-01.azurewebsites.net` — Azure App Service (F1) |
| Frontend | `https://gestora-project-xi.vercel.app` — Vercel (punta ancora al vecchio backend Railway, offline: da aggiornare, vedi `OPS-006`) |
| Database | PostgreSQL su Neon (gratuito permanente), schema riallineato (7 migration EF + tabelle Quartz) |
| Test | **239** backend (xUnit) + **35** frontend (Vitest), tutti verdi |
| Modifiche al database | 7 applicate, in locale e nel nuovo database Neon (elenco nel foglio *Migration* del tracker) |
| Vulnerabilità note nelle librerie | 0 |
| Branch | `main` = `dev` = `origin`, allineati |

**Ultima cosa fatta**: il redesign **«Turno»** (banda dei coperti, tema scuro neutro, gerarchia
visiva). Scritto e misurato, **non ancora verificato a mano** — checklist in
`GestoraDocs/verifica-redesign.md`, verifica in carico a Fabio (`UI-001` in `BACKLOG.md`). Per
dati veri su cui provare: `dotnet run -- --seed-sviluppo` da `GestoraWebApi`. Racconto completo
del redesign e dei difetti emersi in `docs/archivio/STORICO_FASI.md`.

---

## 2. Cos'è Gestora

Applicativo per gestire l'organizzazione di un'attività commerciale con posti a sedere —
ristorante, pub, pizzeria.

Fa tre cose:
1. **Configurare la sala**: zone, tavoli e loro capienza, fasce orarie con un tetto di coperti
2. **Prendere prenotazioni**, sia dallo staff (anche al telefono) sia dal cliente da solo
3. **Assegnare il tavolo da solo**, unendo più tavoli quando serve, scegliendo la combinazione
   che spreca meno posti

Tre ruoli: **Admin** (configura tutto e gestisce gli utenti), **Staff** (prenotazioni e
dashboard), **Cliente** (solo le proprie prenotazioni).

Un utente può avere **più ruoli insieme** — è normale, non un errore.

---

## 3. Com'è fatto

### Struttura

**A livelli, dentro un progetto solo**: `Controller → Service → Repository → database`.
Non è "Clean Architecture" né "DDD": documenti vecchi lo dicevano, ma non corrisponde al vero.
Per un progetto di questa dimensione la struttura a livelli è la scelta giusta.

```
Gestora/
├── GestoraWebApi/        backend (.NET) — vedi il suo CLAUDE.md
├── gestora-frontend/     frontend (React) — vedi il suo CLAUDE.md
├── docs/archivio/        documenti chiusi, sola lettura
├── CLAUDE.md             questo file
├── BACKLOG.md            cosa resta da fare
├── RUNBOOK.md            come si fanno le operazioni
├── TrackGestora_v2.xlsx  il tracker attivo
└── TrackAttività_Gestora.xlsx   congelato, archivio dei primi mesi
```

### Backend — ASP.NET Core 9

Entity Framework Core 9 su PostgreSQL, autenticazione con ASP.NET Identity + token JWT.
In più: **Quartz** per i due lavori notturni programmati, **FluentValidation** per i controlli
sui dati in ingresso, **AutoMapper**, **Serilog** per i log, cache in memoria.
Test con **xUnit + Moq**. Deploy con **Dockerfile** (non con il rilevamento automatico di
Railway, che con .NET 9 non funzionava).

### Frontend — React 19 + TypeScript + Vite

**React Router v7** (attenzione: qualsiasi nota che parli di v6 è vecchia), **TanStack Query v5**
per le chiamate al server, **React Hook Form + Zod** per i form, **shadcn/ui + Tailwind CSS v4**
per l'aspetto, **Axios** con un intercettore che allega il token.
Tavolozza calda propria con tema chiaro/scuro, verificata per leggibilità con
`gestora-frontend/scripts/contrasto.mjs`.
Test con **Vitest + Testing Library**. Deploy su **Vercel**, che ripubblica da solo a ogni push
su `main`.

**`/` è pubblica**: presenta il locale e lascia controllare la disponibilità senza registrarsi.
⚠️ Su quella pagina non c'è nessun token: l'unico endpoint chiamabile è `check-disponibilita`.

### Due ambienti separati in modo stabile

- **Locale**: frontend su `localhost:5173`, backend su `localhost:5099`, database PostgreSQL
  sulla macchina. Il frontend locale legge `.env.local` (non versionato).
- **Produzione**: Vercel + Railway. **Non legge mai** `.env.local`: usa la propria variabile
  impostata nel pannello Vercel.

Cambiare `.env.local` non ha nessun effetto sulla produzione. Aprire `localhost:5173` mostra
sempre i dati del database locale: è voluto.

---

## 4. Le 10 decisioni di prodotto — non si riaprono

Prese il 28/08/2026. Il testo completo è in `docs/archivio/ROADMAP_REVISIONE.md`, sezione
*Decisioni prese*.

1. **La capienza della fascia oraria è in coperti**, non in numero di prenotazioni (campo
   `MaxCoperti`)
2. **Le modifiche al database si applicano a mano**: Claude prepara, Fabio applica seguendo il
   `RUNBOOK.md`
3. **Bonus dei posti di testata solo per unioni di soli tavoli da 2** (2 tavoli = 6 posti,
   3 tavoli = 8). Per qualsiasi unione mista, capienza = somma semplice
4. **Vince sempre la combinazione con meno posti sprecati**, tavolo singolo o unione che sia.
   Massimo 4 tavoli per unione, tutti della stessa zona
5. **La capienza di un tavolo è un numero qualsiasi da 1 in su** (prima erano ammessi solo 2, 4, 8)
6. **Il primo amministratore si crea da una schermata di primo avvio**, non da un endpoint pubblico
7. **La creazione automatica dei tavoli è rimandata** alla v2.0
8. **È il tetto della fascia a decidere quando è esaurita**, non il numero di tavoli. I tavoli
   servono solo ad assegnare fisicamente il posto
9. **La pagina Postazioni mostra in cima il riepilogo della sala**: è solo informativo
10. **"Una prenotazione al giorno" per il Cliente resta un controllo dell'applicazione**, non un
    vincolo del database, per tutta la v1

---

## 5. Come sono andate le fasi di revisione

Dal 28/08 all'08/09/2026, dopo il primo rilascio, il progetto è stato rivisto da capo: 99
segnalazioni numerate, chiuse in 11 fasi, più due fasi aggiunte dopo (documentazione e aspetto),
più il redesign «Turno». Il racconto completo, con i difetti emersi e le regole di metodo
imparate sbagliando, è in `docs/archivio/STORICO_FASI.md`.

---

## 6. Cosa è aperto

Il dettaglio aggiornato è nel foglio **Oggi** del tracker e in **`BACKLOG.md`**.

**Priorità corrente**: `UI-001` — verifica a mano del redesign «Turno».

---

## 7. Regole non negoziabili del progetto

1. **Commit e push li fa sempre Fabio, mai Claude.** Claude fornisce il messaggio di commit con
   l'elenco delle modifiche. Formato: `feat: descrizione` oppure `feat: WIP - descrizione` se
   incompleto.
2. **La produzione non si tocca in autonomia.** Database, variabili d'ambiente, deploy: si
   preparano le istruzioni e le esegue Fabio.
3. **Operazioni delicate** (`.env`, `.gitignore`, credenziali, variabili d'ambiente, rimozione di
   file dal tracciamento Git): Claude spiega cosa succede e perché, poi passa le istruzioni a
   Fabio. Non le esegue.
4. **Controllare il branch** a inizio di ogni blocco di modifiche, non solo a inizio sessione.
   È già capitato tre volte di lavorare su `main` invece che su `dev`.
5. **Comandi PowerShell su una riga sola**: i blocchi su più righe si concatenano male nel suo
   terminale.
6. **Implementare direttamente** le modifiche, frontend incluso, per correzioni e fix piccoli.
   Niente procedura guidata passo passo con attesa: rallenta troppo su questo tipo di lavoro.

---

## 8. Il tracker

**`TrackGestora_v2.xlsx`**, in questa cartella, è il tracker unico e ufficiale. Aggiornamento
manuale, nessuna automazione.

> **`TrackAttività_Gestora.xlsx` è congelato**, non si aggiorna più: resta come archivio storico
> dei primi mesi, si legge solo per ritrovare qualcosa del passato.

---

## 9. Dove sta cosa

| Cerchi... | Vai in... |
|---|---|
| Stato del progetto, decisioni, regole del progetto | questo file |
| Cosa resta da fare | `BACKLOG.md` |
| Come si resetta il database, si applica una migration, si pubblica | `RUNBOOK.md` |
| Endpoint, architettura e note del backend | `GestoraWebApi/CLAUDE.md` |
| Pattern, routing e note del frontend | `gestora-frontend/CLAUDE.md` |
| Cosa manca oggi, referto dell'audit, decisioni, diario | `TrackGestora_v2.xlsx` |
| Storia dei primi mesi e inventari tecnici (congelato) | `TrackAttività_Gestora.xlsx` |
| Com'è andata una fase, cosa abbiamo imparato | `docs/archivio/STORICO_FASI.md` |
| Cosa vuol dire una sigla `REV-xxx` | `docs/archivio/REVISIONE_END_TO_END.md` |
| Il testo integrale delle 10 decisioni | `docs/archivio/ROADMAP_REVISIONE.md` |
| Perché un documento è stato archiviato | `docs/archivio/README.md` |
| Come si prova a mano il redesign, schermata per schermata | `GestoraDocs/verifica-redesign.md` |

### Il grafo del codice (graphify)

In `graphify-out/` c'è una mappa del codice generata automaticamente. Per domande sul codice
conviene partire da lì invece di cercare a mano nei file:

- `graphify query "<domanda>"` — la porzione di codice che riguarda la domanda
- `graphify path "<A>" "<B>"` — come sono collegate due cose
- `graphify explain "<concetto>"` — spiegazione mirata

Dopo aver modificato il codice: `graphify update .` (non costa nulla, legge solo i file).
