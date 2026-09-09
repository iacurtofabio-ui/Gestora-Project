# Gestora

> **Documento di ingresso del progetto.** Si legge a inizio di ogni sessione.
> Cosa è aperto → `BACKLOG.md` · Come si fa una cosa → `RUNBOOK.md` · Com'è andata → `docs/archivio/STORICO_FASI.md`

---

## 1. Stato — aggiornato al 08/09/2026

**Il progetto è finito e in produzione.** Ultima versione pubblicata: **`v1.0.6`**.

| | |
|---|---|
| Backend | `https://gestora-project-production.up.railway.app` — Railway |
| Frontend | `https://gestora-project-xi.vercel.app` — Vercel |
| Database | PostgreSQL su Railway, stesso progetto del backend |
| Test | **239** backend (xUnit) + **26** frontend (Vitest), tutti verdi |
| Modifiche al database | 7 applicate, in locale e in produzione (elenco nel foglio *Migration* del tracker) |
| Vulnerabilità note nelle librerie | 0 |
| Branch | `main` = `dev` = `origin`, allineati |

**Ultima cosa fatta**: Fase 13 — identità visiva e pagina pubblica. L'app aveva la tavolozza
grigia di partenza mai cambiata, un tema scuro scritto ma mai attivato, e `/` che rimandava
dritto al login: chi apriva il link trovava un form di accesso e se ne andava. Ora c'è una
tavolozza calda verificata per leggibilità, il tema scuro funziona, e la radice è una vetrina
pubblica dove si può controllare la disponibilità **senza registrarsi**.

A fine giornata Fabio ha provato l'app in locale e ha riscritto alcuni testi della pagina
pubblica (titolo, i tre passaggi, le zone). Nessuna modifica al codice, solo parole.

**Cosa viene dopo**: un giro di correzioni sull'aspetto, deciso guardando l'app in funzione
(`UI-001` in `BACKLOG.md`). Poi la fase sul file di appunti d'uso (`DOC-001`).

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
└── TrackAttività_Gestora.xlsx
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
segnalazioni numerate, chiuse in 11 fasi, più due fasi aggiunte dopo (documentazione e aspetto).

| Fase | Cosa ha portato | Tag |
|---|---|---|
| 1 | Fondamenta di deploy: Dockerfile, health check sul database | — |
| 2 | Nuovo algoritmo di assegnazione tavoli, disponibilità unificata | — |
| 3 | Prenotazioni simultanee: indice unico, transazioni | — |
| 4 | Sicurezza e schermata di primo avvio | `v1.0.1` |
| 5 | Test del backend: da 31 a 74 | — |
| 6 | Bug del frontend: schermata bianca, modifica ed eliminazione prenotazione | `v1.0.2` |
| 7 | Robustezza del backend: paginazione, prestazioni, indirizzo IP reale | `v1.0.3` |
| 8 | Robustezza del frontend: errori centralizzati, primi 26 test | `v1.0.4` |
| 9 | Pulizia del codice, 0 vulnerabilità nelle librerie | — |
| 10 | Esperienza d'uso: responsive, accessibilità, semaforo disponibilità | `v1.0.5` |
| 11 | Chiusura: fix sovrapposizione fasce | `v1.0.6` |
| 12 | Ordine e pulizia della documentazione | — |
| 13 | Identità visiva, tema scuro e pagina pubblica | — |

Il racconto completo, con i difetti emersi e **le 9 regole di metodo imparate sbagliando**, è in
`docs/archivio/STORICO_FASI.md`. Vale la pena rileggerlo: è la parte più utile da raccontare a
un colloquio.

---

## 6. Cosa è aperto

Il dettaglio è in **`BACKLOG.md`**.

1. **`UI-001`** — correzioni sull'aspetto, raccolte provando l'app in funzione. **È il prossimo
   lavoro.**
2. **`DOC-001`** — formalizzare il file di appunti d'uso di Fabio.
   ⚠️ Il file `AppuntiFix.txt` **non si apre** finché non parte quella fase.
3. **`OPS-001`** — reset completo dei due database, quando tutte le implementazioni sono chiuse
4. **Pulizie minori** e le idee per la **v2.0**

---

## 7. Come lavoriamo insieme

### Chi è Fabio

Developer con 4 anni di esperienza su Dynamics 365 / Power Platform, in passaggio verso il full
stack .NET + React. Conosce le basi di C#, .NET, Entity Framework, JavaScript, SQL Server, Git.
**Poca esperienza sul frontend.** Obiettivo dichiarato: farsi assumere come full stack developer.

### Come parlargli — regola aggiornata l'08/09/2026

**Linguaggio tecnico di base.** Termini semplici, frasi corte, niente vocabolario da esperto per
sembrare esperti. Se serve un termine tecnico si usa, ma va spiegato in una riga.

L'obiettivo non è **saper parlare** da senior, è **saper fare** il lavoro. Un concetto spiegato
bene con parole semplici vale più di uno spiegato con le parole giuste che non lascia niente.

Altre regole:
- **In italiano**, sempre, salvo nomi tecnici e codice
- **Risposte concise**: non ripetere quello che ha appena detto
- Affiancarlo come **senior che lavora insieme a lui**: spiegare le scelte, non consegnare solo
  il codice
- **Implementare direttamente** le modifiche, frontend incluso. Niente procedura guidata passo
  passo con attesa che sia lui a scrivere: rallentava troppo sui fix piccoli
- Quando c'è un concetto nuovo, spiegarlo comunque — breve, non un tutorial
- Dire **subito** se qualcosa è sbagliato o migliorabile
- Dire quando qualcosa **non è pronto per la produzione**, e perché

### Regole non negoziabili

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

### Modello e consumo

- Partire da **Sonnet**. Suggerire `/model opus` solo per architettura complessa o refactoring
  grossi. Il modello lo cambia Fabio.
- **Stop ai giri a vuoto**: se un tentativo fallisce 3 volte di fila, fermarsi, dire di fare
  `/compact` o `/clear`, e analizzare il problema alla radice invece di ritentare.
- **Letture mirate**: percorsi specifici, mai "tutto il progetto", mai `node_modules`, `dist`,
  `bin`, `obj`.

---

## 8. Il tracker

`TrackAttività_Gestora.xlsx`, in questa cartella. È il **tracker unico e ufficiale**: se compare
un altro file che sembra un tracker, è un doppione — segnalarlo a Fabio, non aggiornarlo.

> Nota pratica: il nome ha caratteri accentati, trovare il file con `Get-ChildItem Track*.xlsx`.
> Claude lo legge e scrive da PowerShell con Excel COM, i fogli si indirizzano per **numero**
> perché i nomi contengono emoji.

### A cosa serve ogni foglio

| # | Foglio | Cosa contiene |
|---|---|---|
| 1 | Dashboard | Il colpo d'occhio: stato dei moduli e numeri |
| 2 | Appunti e Step | **Il diario di lavoro**: cosa si è fatto, sessione per sessione |
| 3 | Roadmap | Le funzionalità, implementate e no |
| 4 | Piano di Sviluppo | Le fasi con date di inizio e fine |
| 5 | Fix e Bug | **I difetti**, con stato |
| 6-13 | Refactoring, Modelli, Repository, Services, Controllers, Test, Auth & Security, Jobs, Migration | Inventari tecnici |

### Protocollo di aggiornamento

**A inizio sessione**: leggere questo file + il foglio *Appunti e Step*.

**Quando si completa qualcosa**: aggiornare *Appunti e Step*, *Roadmap*, *Piano di Sviluppo* e
*Fix e Bug* **insieme**, non solo uno.

**Quando si prende una decisione architetturale**: aggiungerla subito in *Note e Decisioni*
dentro *Appunti e Step*.

**Quando si aggiunge un endpoint o un componente**: registrarlo subito nel foglio corrispondente.

**A fine sessione**: aggiornare la sezione 1 di questo file, poi Fabio committa.

### Colori degli stati — obbligatori, nessuna eccezione

| Stato | Colore |
|---|---|
| Completato | verde `#C6E7CE` |
| Da fare / Parziale / In corso | giallo `#FFEB9C` |
| Pianificato | azzurro `#DDEBF7` |
| Non necessario | grigio `#D9D9D9` |
| Futuro | grigio chiaro `#EDEDED` |

> Il verde è `#C6E7CE`, non `#C6EFCE`: il protocollo scritto e il file dicevano due cose diverse,
> l'08/09/2026 si è scelto quello già usato in tutte le celle.

---

## 9. Dove sta cosa

| Cerchi... | Vai in... |
|---|---|
| Stato del progetto, decisioni, come lavoriamo | questo file |
| Cosa resta da fare | `BACKLOG.md` |
| Come si resetta il database, si applica una migration, si pubblica | `RUNBOOK.md` |
| Endpoint, architettura e note del backend | `GestoraWebApi/CLAUDE.md` |
| Pattern, routing e note del frontend | `gestora-frontend/CLAUDE.md` |
| Diario di lavoro e inventari tecnici | `TrackAttività_Gestora.xlsx` |
| Com'è andata una fase, cosa abbiamo imparato | `docs/archivio/STORICO_FASI.md` |
| Cosa vuol dire una sigla `REV-xxx` | `docs/archivio/REVISIONE_END_TO_END.md` |
| Il testo integrale delle 10 decisioni | `docs/archivio/ROADMAP_REVISIONE.md` |
| Perché un documento è stato archiviato | `docs/archivio/README.md` |

### Il grafo del codice (graphify)

In `graphify-out/` c'è una mappa del codice generata automaticamente. Per domande sul codice
conviene partire da lì invece di cercare a mano nei file:

- `graphify query "<domanda>"` — la porzione di codice che riguarda la domanda
- `graphify path "<A>" "<B>"` — come sono collegate due cose
- `graphify explain "<concetto>"` — spiegazione mirata

Dopo aver modificato il codice: `graphify update .` (non costa nulla, legge solo i file).
