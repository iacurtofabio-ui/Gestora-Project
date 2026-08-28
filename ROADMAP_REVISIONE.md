# Roadmap delle sistemazioni — Gestora

Data: 28/08/2026 · Fonte: `REVISIONE_END_TO_END.md`
Obiettivo: chiudere la v1 in modo pulito, senza difetti noti aperti.

**Legenda**
- 🧑 **Fabio** — task che devi fare tu
- 🤖 **Claude** — task che faccio io

---

## Decisioni prese — 28/08/2026

Queste scelte sono già state fatte e valgono per tutto il percorso.

1. **Capienza della fascia oraria** = numero massimo di **coperti**, non di prenotazioni. Il campo
   viene rinominato `MaxCoperti` ovunque.
2. **Modifiche al database**: restano manuali. Preparo io la migration, la applichi tu quando
   decidi. Le fasi che ne richiedono una sono la 2 e la 3.
3. **Assegnazione dei tavoli**: cambia logica. L'unità base è il tavolo da 2; i tavoli si uniscono
   in base ai coperti richiesti. **Unendo più tavoli si contano anche le due testate**: 2 tavoli =
   6 posti, 3 tavoli = 8 posti (somma delle capienze + 2).
4. **Criterio di scelta**: sempre la soluzione con **meno posti sprecati**, tavolo singolo o
   unione che sia. Se resta libero solo un tavolo grande, viene assegnato comunque: rifiutare un
   cliente è peggio che sprecare posti.
5. **Capienza dei tavoli**: qualsiasi numero da 1 in su. Sparisce il vincolo che oggi ammette solo
   2, 4 e 8, così ogni locale mappa la sala com'è davvero.
6. **Primo amministratore**: creato da una schermata di primo avvio, non più da un endpoint
   pubblico.
7. **Creazione automatica delle postazioni**: rinviata al backlog v2.0.

---

## Come leggere questa roadmap

Le fasi vanno in ordine: ognuna poggia su quella prima. Ogni fase si chiude così: io aggiorno
tracker e documenti di progetto, tu fai commit e push. Nessuna fase è chiusa senza `dotnet test` e
`npm run build` verdi.

I tuoi task sono pochi e concentrati: li trovi tutti raccolti nel riepilogo finale.

---

## Fase 0 — Preparazione

*Partire dal punto giusto.*

🧑 **Fabio**
- Posizionarsi sul branch `dev` e verificare `git status`
- Allineare il database locale: `dotnet ef database update`
- Committare `JobsController.cs`, che oggi esiste solo sul tuo PC
- **Controllare che valore hai messo in produzione nel campo capienza delle fasce orarie.** Se
  avevi scritto "10" pensando a 10 prenotazioni, oggi il locale accetta 10 persone in tutto:
  vanno corretti prima di andare avanti

🤖 **Claude**
- Nessun task

**Chiusura**: branch pulito, database locale allineato, capienze di produzione verificate.

---

## Fase 1 — Fondamenta di deploy

*Rendere il rilascio riproducibile e non più affidato al pannello di Railway.*

🤖 **Claude**
- Portare nel repository la configurazione di build e deploy, oggi presente solo nel pannello
  Railway: se lo perdi, non è ricostruibile
- Far controllare al health check anche la raggiungibilità del database, così un rilascio con
  database disallineato risulta fallito invece che "sano"
- Aggiungere all'avvio un avviso esplicito quando il database non è allineato al codice: visto che
  le migration restano manuali, questo è ciò che ti evita di scoprirlo dal primo errore
- Correggere la migration `StatoAsEnum`, oggi vuota e senza effetto
- Allineare le versioni dei pacchetti disallineate e rimuovere quelli inutilizzati

🧑 **Fabio**
- Su Railway: togliere il comando di build personalizzato, ora che la configurazione sta nel
  repository
- Verificare che il rilascio vada a buon fine e che `/health` risponda

**Chiusura**: un push su `main` produce un rilascio corretto senza configurazione nascosta.

---

## Fase 2 — Logica di prenotazione e assegnazione tavoli

*La fase più importante. Non è solo una correzione: cambia il modo in cui il sistema assegna i
tavoli. Per ogni intervento scrivo prima il test che dimostra il problema, poi la soluzione.*

🤖 **Claude — nuova logica di assegnazione**
- Calcolare la capienza di un'unione di tavoli come somma delle capienze **più 2 posti** per le
  testate
- Scegliere sempre la combinazione con meno posti sprecati, valutando insieme tavolo singolo e
  unioni (oggi il tavolo singolo vince sempre, anche quando spreca)
- Non occupare più un tavolo da 8 per 2 persone quando esistono alternative migliori
- Registrare quanti posti vengono realmente usati su ogni tavolo, oggi mai salvato
- Togliere il vincolo che ammette solo tavoli da 2, 4 e 8: capienza libera da 1 in su

🤖 **Claude — correzioni di logica**
- **Disponibilità sempre piena**: l'endpoint pubblico oggi risponde "tutto libero" in ogni caso;
  usa il dato dei posti occupati che finalmente viene salvato
- **Staff bloccato**: permettere ad Admin e Staff di modificare la prenotazione di un cliente,
  come previsto dai ruoli
- Rinominare `MaxPrenotazioni` in `MaxCoperti` in tutto il progetto, database compreso, e mettere
  un'etichetta chiara nel form (oggi il campo non ne ha nessuna)
- Far usare alla verifica di disponibilità la stessa logica dell'assegnazione: oggi sono due
  algoritmi diversi che possono dare risposte opposte
- Escludere zone e tavoli disattivati dalla verifica di disponibilità
- Registrare nel log attività anche la modifica di una prenotazione, oggi l'unica azione non
  tracciata
- Un solo orologio per tutto il progetto: niente più mix di orario locale, UTC e ora italiana

🧑 **Fabio**
- Applicare la migration in produzione (rinomina del campo capienza)
- Provare: prenotazione da 2 con solo tavoli grandi liberi, prenotazione da 8 che unisce 3 tavoli,
  verifica disponibilità con locale pieno, modifica di una prenotazione da account Staff

**Chiusura**: la nuova logica di assegnazione è coperta da test e i casi sopra si comportano come
previsto.

---

## Fase 3 — Prenotazioni simultanee

*Impedire che due clienti prenotino lo stesso tavolo nello stesso momento.*

🤖 **Claude**
- Racchiudere creazione e modifica della prenotazione in un'unica operazione atomica
- Aggiungere il controllo di modifica concorrente sulla prenotazione (migration)
- Ripristinare in altra forma la garanzia "una prenotazione al giorno per cliente", persa quando è
  stato rimosso il vincolo sul database
- Scrivere un test che simula due prenotazioni simultanee

🧑 **Fabio**
- Applicare la migration in produzione
- Provare a prenotare lo stesso tavolo da due browser diversi contemporaneamente

**Chiusura**: la doppia prenotazione non è più possibile, né in locale né in produzione.

---

## Fase 4 — Sicurezza e primo avvio

*Chiudere i punti che oggi lasciano il prodotto esposto.*

🤖 **Claude**
- **Schermata di primo avvio**: se non esiste ancora nessun amministratore, l'app mostra una
  pagina dedicata per crearlo; appena esiste, la pagina sparisce da sola. L'endpoint pubblico
  attuale viene rimosso
- Bloccare l'account dopo N tentativi di accesso falliti e limitare le chiamate ripetute alla
  pagina di login
- Smettere di restituire al client i messaggi d'errore interni del database
- Applicare la stessa policy password anche al reset fatto dall'Admin, oggi aggirabile
- Togliere l'email dai log di accesso
- Distinguere "non autenticato" da "non autorizzato": oggi un permesso negato provoca il logout
- Impedire al cliente di scrivere il campo riservato a Staff e Admin

🧑 **Fabio**
- Niente

> Nota: la schermata di primo avvio resta raggiungibile solo finché non esiste un Admin. In
> produzione l'Admin c'è già, quindi è di fatto chiusa da subito.

**Chiusura**: nessun endpoint sensibile raggiungibile senza autenticazione.

---

## Fase 5 — Test del backend

*Coprire il percorso che il prodotto esiste per fare. Oggi i test verdi non toccano la
prenotazione.*

🤖 **Claude**
- Test su creazione e modifica di una prenotazione: capienza, giorno corretto, data passata,
  limite giornaliero, preavviso minimo del cliente
- Test sull'assegnazione reale del tavolo, nuova logica compresa (oggi è coperto solo un metodo
  gemello, non quello usato)
- Test sulla verifica di disponibilità e sulla dashboard
- Test sui ruoli: chi può fare cosa
- Test sui due processi automatici notturni

🧑 **Fabio**
- Niente

**Chiusura**: il flusso di prenotazione è coperto end-to-end e tutti i test passano.

---

## Fase 6 — Bug del frontend

*I quattro problemi che l'utente incontra davvero.*

🤖 **Claude**
- **Schermata bianca**: oggi un dato di sessione corrotto blocca l'app in modo irrecuperabile,
  nemmeno il login è raggiungibile
- **Scelta della zona**: il campo non è collegato al modulo, si prenota "nessuna preferenza"
  credendo di aver scelto una zona
- **Dashboard**: tra mezzanotte e le due mostra i dati del giorno prima
- **Indirizzo del server**: se la variabile non è impostata l'app fallisce in silenzio; aggiungo
  un errore esplicito
- Far scadere la sessione in modo pulito invece che con un errore improvviso

🧑 **Fabio**
- Verificare su Vercel che la variabile con l'indirizzo del backend sia impostata

**Chiusura**: i quattro casi sono verificati sull'ambiente di produzione.

---

## Fase 7 — Robustezza del backend

*Correggere i problemi che non bloccano ma degradano il servizio.*

🤖 **Claude**
- Svuotare correttamente la cache quando una fascia oraria viene eliminata
- Validare il numero di pagina, oggi un valore sbagliato manda l'API in errore
- Rendere stabile l'ordinamento delle liste paginate, oggi righe possono sparire o ripetersi
- Alleggerire le query pesanti: elenco utenti, processi notturni, assegnazione del tavolo
- Registrare l'indirizzo IP reale nel log attività, oggi registra quello del server intermedio
- Aggiungere i controlli mancanti sui dati in ingresso, in particolare sull'endpoint pubblico
- Restituire una lista vuota invece di un errore quando non ci sono risultati
- Non cancellare lo storico delle prenotazioni quando si elimina un utente
- Contare i tavoli occupati per fascia e non per giornata intera, nella dashboard
- Rendere il log attività consultabile: indici sulla tabella e endpoint di lettura per l'Admin

🧑 **Fabio**
- Niente

**Chiusura**: nessuna segnalazione backend di priorità media aperta.

---

## Fase 8 — Robustezza del frontend

🤖 **Claude**
- Svuotare la cache al logout: oggi cambiando account si vedono i dati del precedente
- Eliminare la gestione errori copiata 21 volte, sostituendola con una funzione unica
- Togliere le notifiche doppie sulla pagina Postazioni
- Aggiungere la paginazione: oltre 100 prenotazioni i dati oggi spariscono senza avviso
- Disabilitare i pulsanti durante il salvataggio, per evitare doppie chiamate
- Rendere riutilizzabile la finestra di conferma, oggi dice "Elimina" anche quando annulla
- Attivare i controlli stretti di TypeScript
- Togliere gli strumenti di sviluppo dal pacchetto di produzione
- Introdurre i primi test frontend su accesso, ruoli e scelta della fascia oraria

🧑 **Fabio**
- Niente

**Chiusura**: build e controlli dei tipi puliti, primi test frontend verdi.

---

## Fase 9 — Pulizia

*Togliere quello che confonde chi legge il codice, incluso un recruiter.*

🤖 **Claude**
- Eliminare cartelle vuote, codice morto e file mai usati (backend e frontend)
- Rimuovere il costruttore inutilizzato che, se selezionato, farebbe partire il servizio senza
  dipendenze
- Uniformare il nome di "fascia oraria", oggi scritto in quattro modi diversi
- Accorpare le duplicazioni: algoritmo copiato due volte, funzioni ripetute in quattro servizi,
  costanti ripetute
- Rimuovere le implementazioni che lanciano errore solo per soddisfare un'interfaccia
- Correggere i nomi di file troncati e i refusi
- Applicare la formattazione automatica a tutto il progetto

🧑 **Fabio**
- Niente

**Chiusura**: nessun file morto, nessuna duplicazione evidente, formattazione uniforme.

---

## Fase 10 — Esperienza d'uso

*Rendere il prodotto presentabile, anche da telefono.*

🤖 **Claude**
- Rendere l'app utilizzabile su smartphone: menu laterale richiudibile, tabelle scorrevoli,
  layout adattivo (oggi da telefono è inutilizzabile)
- Aggiungere i messaggi per le liste vuote, oggi si vede una tabella vuota che sembra un errore
- Sostituire il "Caricamento…" a schermo intero con un caricamento parziale
- Uniformare i pulsanti e i colori a un unico stile
- Sistemare l'accessibilità: etichette collegate ai campi, chiusura delle finestre con ESC
- Mostrare nome e ruolo dell'utente e la pagina attiva nel menu
- Sistemare titolo, lingua e icona del sito

🧑 **Fabio**
- Un giro di prova da telefono sui tre ruoli

**Chiusura**: l'app è usabile e coerente su desktop e mobile.

---

## Fase 11 — Chiusura

🤖 **Claude**
- Aggiornare `BACKEND_FIX_TODO.md` con tutto quello che è stato chiuso
- Aggiornare il tracker Excel su tutti i fogli e il blocco di stato di `CLAUDE.md`
- Aggiornare il piano di test manuale, fermo a marzo e non più allineato al prodotto
- Documentare la nuova regola di assegnazione dei tavoli, che oggi non è scritta da nessuna parte
- Scrivere quali requisiti iniziali sono stati implementati e quali esclusi
- Correggere il percorso sbagliato del progetto nei documenti
- Aggiungere lo schema del database e l'elenco delle modifiche applicate
- Rigenerare il grafo del progetto, fermo al 14/08

🧑 **Fabio**
- Verifica finale in produzione sui tre ruoli
- Merge su `main` e tag `v1.1.0`

**Chiusura**: nessuna segnalazione aperta, documentazione allineata al prodotto reale.

---

## Riepilogo — cosa devi fare tu

1. **Fase 0** — allineare il database locale, committare `JobsController.cs` e **verificare le
   capienze delle fasce in produzione**
2. **Fase 1** — togliere il comando di build personalizzato su Railway
3. **Fase 2** — applicare una migration in produzione
4. **Fase 3** — applicare una migration in produzione
5. **Fase 6** — verificare una variabile su Vercel
6. **Fasi 2, 3, 6, 10** — quattro giri di prova
7. **A ogni fase** — commit e push
8. **Fase 11** — verifica finale, merge e tag

Tutto il resto è a mio carico, aggiornamento di tracker e documenti compreso.

---

## Backlog v2.0 — fuori da questa roadmap

- Creazione automatica delle postazioni in base ai coperti richiesti
- Turnover del tavolo (durata seduta, due turni nella stessa fascia)
- No-show come stato reale, con storico e policy per cliente
- Lista d'attesa quando la fascia è piena
- Email di conferma e promemoria
- Zona come preferenza con ripiego, invece che vincolo
- Overbooking controllato per fascia
- Chiusure straordinarie e orari speciali
- Export CSV/PDF dei report
- Unione e separazione tavoli come azione manuale dello Staff
- Recupero password autonomo
- Sessione con rinnovo automatico
- Gestione di più locali sullo stesso impianto
