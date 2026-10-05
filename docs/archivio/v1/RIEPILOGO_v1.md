# Gestora v1 — riepilogo

Scritto il 05/10/2026, all'avvio della v2. Il dettaglio completo è negli altri file di questa
cartella; questo è il quadro in una pagina.

## Obiettivi iniziali

Un gestionale per attività con posti a sedere (ristorante, pub, pizzeria) che:
1. configura la sala: zone, tavoli con la loro capienza, fasce orarie con un tetto di coperti
2. prende prenotazioni dallo staff (anche al telefono) e dal cliente da solo
3. assegna il tavolo da solo, unendo più tavoli quando serve, scegliendo la combinazione che
   spreca meno posti

con tre ruoli (Admin, Staff, Cliente), anche sommabili sullo stesso utente.

## Come è andata, in breve

| Periodo | Cosa |
|---|---|
| giugno – agosto 2026 | Sviluppo e analisi pre-rilascio (`PIANO_RILASCIO.md`, `BACKEND_FIX_TODO.md`) |
| 27/08/2026 | Primo rilascio `v1.0.0` (backend su Railway) |
| 28/08 – 08/09/2026 | Revisione completa: 99 segnalazioni `REV-xxx`, 11 fasi, 10 decisioni di prodotto, tag `v1.0.1`…`v1.0.6` (`REVISIONE_END_TO_END.md`, `ROADMAP_REVISIONE.md`, `STORICO_FASI.md`) |
| settembre 2026 | Chiusura v1.1: redesign «Turno», concorrenza sul tetto dei coperti (`CAP-001`), tag `v1.1.0` (`v1.1/`) |
| 16 – 18/09/2026 | Migrazione da Railway ad Azure App Service + Neon |
| 01 – 02/10/2026 | Analisi completa (`AUD-A1`…`A6`, `AUD-M1`…`M11`), reset dei database, tag **`v1.2.0`** |
| 02/10/2026 | Riordino della cartella e tracker rifatto con fogli separati per backend e frontend |

## Funzionalità completate

- **Sala**: zone, tavoli con capienza libera da 1 in su, fasce orarie con tetto in coperti,
  riepilogo della sala in cima alla pagina Postazioni
- **Prenotazioni**: da Staff e da Cliente, stati `Attiva → InCorso → Completata`, `NonPresentata`
  e `Annullata`; modifica e annullamento del Cliente fino a 2 ore prima; una prenotazione al
  giorno per Cliente
- **Assegnazione automatica dei tavoli**: motore puro e testato, unioni fino a 4 tavoli della
  stessa zona, bonus testate per le unioni di soli tavoli da 2, meno posti sprecati
- **Concorrenza**: niente doppio tavolo (indice unico), tetto dei coperti protetto da lock
- **Utenti**: registrazione, login con blocco dopo 5 tentativi, primo Admin da schermata di primo
  avvio, gestione utenti e ruoli dall'Admin, token invalidati al cambio di ruolo/email/password
- **Dashboard** giornaliera e settimanale, compreso il conteggio dei no-show
- **Pagina pubblica** con verifica della disponibilità senza account
- **Job notturni** (Quartz): completamento/no-show automatici e pulizia dopo 6 mesi
- **Aspetto**: redesign «Turno», tema chiaro/scuro, responsive, controllo del contrasto
- **Registro delle attività** (audit log) sulle scritture

## Stato finale (02/10/2026)

| | |
|---|---|
| Ultimo tag | `v1.2.0` (commit `a4e5672`), provato in produzione: tutto ok |
| Test | 292 backend (xUnit) + 64 frontend (Vitest), tutti verdi |
| Database | 7 migration EF + tabelle Quartz, in locale e su Neon |
| Infrastruttura | Azure App Service F1 (backend) · Vercel (frontend) · Neon (database) |
| Rimasto aperto | Tetto dei 50 coperti per prenotazione fisso nel codice (`CAP-002`) → diventato `V2-002` |
| Rischi accettati | AutoMapper 12.0.1 (`SEC-001`), `AUD-M5`, `AUD-M7`, `AUD-M10` parte Azure — riportati in `BACKLOG.md` della v2 senza sigle |
