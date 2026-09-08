# Archivio documenti — Gestora

I file in questa cartella sono **chiusi**. Servono a ricostruire come si è arrivati allo stato
attuale, non a sapere a che punto siamo oggi.

**Non vanno aggiornati.** Se un'informazione qui dentro serve ancora, va portata nei documenti
vivi, non modificata qui.

Archiviati l'**08/09/2026**, a chiusura della Fase 12 (ordine e pulizia).

## I documenti vivi, per orientarsi

| File | Dove | A cosa serve |
|---|---|---|
| `CLAUDE.md` | cartella principale | Cos'è il progetto, stato attuale, come lavoriamo, indice |
| `BACKLOG.md` | cartella principale | Tutto ciò che è ancora aperto |
| `RUNBOOK.md` | cartella principale | Le procedure operative: reset, migration, deploy |
| `GestoraWebApi/CLAUDE.md` | backend | Riferimento tecnico del backend |
| `gestora-frontend/CLAUDE.md` | frontend | Riferimento tecnico del frontend |
| `TrackAttività_Gestora.xlsx` | cartella principale | Il tracker delle attività |

---

## Cosa c'è qui dentro

### `STORICO_FASI.md` — la storia del progetto

**Scritto**: 08/09/2026, estraendo il racconto delle fasi dal vecchio `CLAUDE.md`.

Il racconto delle 11 fasi di revisione: cosa è stato fatto, quali difetti sono emersi, gli
incidenti e — la parte più utile — le **9 regole di metodo** imparate sbagliando.

È l'unico file dell'archivio che vale la pena rileggere per intero. È anche il materiale migliore
da raccontare a un colloquio: non "cosa ho costruito", ma "cosa ho capito costruendolo".

### `ROADMAP_REVISIONE.md` — il piano di lavoro delle 11 fasi

**Periodo**: 28/08 – 08/09/2026. **Stato**: tutte le fasi chiuse.

Era il documento operativo da seguire: ordine delle fasi, cosa entrava in ognuna, chi faceva
cosa. Conteneva anche la *Definition of Done* (quando una fase si può dire chiusa davvero).

**Cosa ne è stato salvato:**
- le **10 decisioni di prodotto** del 28/08 → riassunte in `CLAUDE.md`, il testo integrale resta
  qui nella sezione *Decisioni prese*
- il **backlog v2.0** (14 idee per il futuro) → spostato in `BACKLOG.md`
- la **procedura per le migration in produzione** → spostata in `RUNBOOK.md`

> ⚠️ La sua *Fase 11* elencava 12 punti di pulizia documentale (REV-083…REV-096) che allora non
> furono fatti: la fase venne ristretta al solo fix del codice. Quel lavoro è stato completato
> nella Fase 12, cioè con l'archiviazione che stai leggendo.

### `REVISIONE_END_TO_END.md` — la revisione da cui è nato tutto

**Periodo**: 28/08/2026. **Stato**: tutte le segnalazioni chiuse.

La revisione completa del progetto alla v1.0.0, con 99 segnalazioni numerate `REV-001`…`REV-099`.
È la fonte di tutte le sigle REV citate negli altri documenti: **se trovi un `REV-xxx` e non sai
cos'è, si cerca qui**.

Contiene anche una valutazione di come il prodotto rispecchiava l'idea iniziale e le incoerenze
di dominio trovate. Utile da rileggere se un giorno si riprende il progetto da fermo.

### `PIANO_RILASCIO.md` — l'analisi architetturale iniziale

**Periodo**: 11/06 – 13/08/2026. **Stato**: tutte le 8 fasi completate, rilascio `v1.0.0` fatto
il 27/08/2026.

L'analisi dello stato del progetto prima del primo rilascio, con i problemi divisi per gravità e
la sequenza di lavoro consigliata (fix backend → test → deploy → verifica → frontend → rilascio).

> ⚠️ **Contiene un'imprecisione nota (REV-089)**: descrive l'architettura come
> "Clean Architecture + DDD". Non è così: la struttura reale è **a livelli** (controller →
> service → repository) dentro un unico progetto. È corretto nel `CLAUDE.md` attuale.

### `BACKEND_FIX_TODO.md` — il primo elenco di fix del backend

**Periodo**: fino al 31/08/2026. **Stato**: tutte le fix chiuse.

Il primo registro dei difetti del backend, con sigle `FIX-001`…`FIX-009` e `CORS-001`.
Superato dalla revisione end-to-end.

> ⚠️ **Dichiarava "nessun backlog residuo" quando non era più vero** — è la segnalazione REV-083.
> È il motivo per cui oggi esiste **un solo** file per le cose aperte, `BACKLOG.md`.

### `Utilities.txt` — appunti pratici

**Periodo**: 25/08/2026.

Percorsi e comandi degli User Secrets del backend (le credenziali che vivono solo sulla macchina
di sviluppo, fuori dal repository).

**Cosa ne è stato salvato:** tutto il contenuto è confluito in `RUNBOOK.md`, sezione
*User Secrets*.

---

## Cosa NON è archiviato

- `AppuntiFix.txt` — il file di appunti d'uso personale di Fabio, nella cartella principale.
  Non è un documento di progetto e non va toccato: sarà letto e formalizzato in una fase dedicata
  (vedi `BACKLOG.md`, voce `DOC-001`).
- `docs/Gestora - Deploy con Docker.docx` — documentazione del deploy, ancora valida.
