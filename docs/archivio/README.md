# Archivio documenti — Gestora

I file in questa cartella sono **chiusi**: servono a ricostruire come si è arrivati allo stato
attuale, non a sapere a che punto siamo oggi. **Non vanno aggiornati**: se un'informazione qui
dentro serve ancora, va portata nei documenti vivi (`CLAUDE.md`, `BACKLOG.md`, `RUNBOOK.md` in
radice).

Una sottocartella per ogni versione chiusa.

---

## `v1/` — Gestora v1 (fino al 02/10/2026, ultimo tag `v1.2.0`)

Archiviata il 05/10/2026, all'avvio della v2. I percorsi citati **dentro** questi file sono
quelli di allora (per esempio `docs/archivio/STORICO_FASI.md` oggi è `docs/archivio/v1/STORICO_FASI.md`).

| File | Cosa contiene |
|---|---|
| `RIEPILOGO_v1.md` | **Da leggere per primo.** Obiettivi, funzionalità completate, stato finale della v1 |
| `stato-finale/` | Copia di `CLAUDE.md`, `RUNBOOK.md` e dei due `CLAUDE.md` tecnici com'erano alla chiusura della v1, con tutte le sigle e le note storiche |
| `BACKLOG_v1.md` | Il `BACKLOG.md` alla chiusura: tutte le voci chiuse (`OPS-*`, `SEC-*`, `CAP-001`, `UI-001`, `DOC-001`, `AUD-*`) e le decisioni |
| `STORICO_FASI.md` | Il racconto delle fasi di revisione e le regole di metodo imparate sbagliando. Il più utile da rileggere |
| `ROADMAP_REVISIONE.md` | Il piano delle 11 fasi di revisione e il testo integrale delle 10 decisioni di prodotto |
| `REVISIONE_END_TO_END.md` | La revisione del 28/08/2026: cosa vuol dire una sigla `REV-xxx` |
| `PIANO_RILASCIO.md` | L'analisi prima del primo rilascio `v1.0.0` |
| `BACKEND_FIX_TODO.md` | Il primo elenco di fix del backend (`FIX-xxx`, `CORS-001`) |
| `Utilities.txt` | Vecchi appunti sugli User Secrets (oggi in `RUNBOOK.md` §7) |
| `Gestora - Deploy con Docker.docx` | Il deploy di quando il backend stava su Railway |
| `TrackAttività_Gestora.xlsx` | Il tracker dei primi mesi |
| `TrackGestora_fino_al_02-10-2026.xlsx` | Il tracker prima del rifacimento del 02/10/2026: fogli Oggi, Referto (audit), Fatte, Decisioni, Diario |
| `script-sql/` | Script SQL già applicati a mano in produzione e lo script di azzeramento di Neon |
| `v1.1/` | La chiusura della v1.1: consegna fase per fase, diario, prompt di lavoro, checklist del redesign «Turno» |
| `README.md` | Il vecchio indice dell'archivio, com'era alla chiusura della v1 |
