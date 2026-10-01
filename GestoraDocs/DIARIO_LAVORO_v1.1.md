# Diario del lavoro verso v1.1 — versione semplice

> Questo file racconta **cosa è stato fatto e perché**, in linguaggio semplice, senza codice.
> Per i dettagli tecnici (file toccati, messaggi di commit, righe di log) c'è
> `GestoraDocs/CONSEGNA_v1.1.md`. Questo file qui serve per capire velocemente lo stato del
> lavoro e cosa provare a mano.

**Aggiornato al:** 21/09/2026 — lavoro concluso. Fasi 0b-7, 9 e 10 completate per intero; fasi 8 e
11 parziali (dettaglio nei punti corrispondenti più sotto).
**Dove si trova il lavoro:** branch `dev`, tutto in locale. Fabio farà commit e push da Visual
Studio; i messaggi di commit sono già pronti nel file tecnico.

---

## Il punto più urgente: risolto ✅

Un file (`envDBNeon.txt`) con la password del database di produzione era finito per sbaglio nel
repository pubblico. **Fabio ha già cambiato la password su Neon e su Azure, verificato che il
sito funzioni, e tolto il file dal repository.** Non c'è più niente da fare su questo punto.

---

## Cosa è stato fatto finora

### 1. Il tetto dei coperti ora è rispettato davvero

**Il problema.** Ogni fascia oraria ha un numero massimo di coperti che può accettare. Il
controllo però aveva un buco: se due persone prenotavano nello stesso identico momento, il
programma poteva accettarle entrambe anche se insieme superavano il limite. Era già capitato
durante i test.

**La soluzione.** Ora, quando arriva una prenotazione, il programma "blocca" temporaneamente la
fascia oraria mentre controlla i posti disponibili, così una seconda richiesta arrivata nello
stesso istante deve aspettare il suo turno invece di passare comunque. Ho anche aggiunto un
segnale visivo: se per qualche motivo una fascia finisse comunque sforata, la dashboard lo mostra
chiaramente invece di nasconderlo.

**Da provare a mano:** aprire l'app da due browser diversi, prenotare nello stesso momento sugli
ultimi posti disponibili di una fascia. Una prenotazione deve andare a buon fine, l'altra deve
essere rifiutata con un messaggio chiaro.

### 2. Piccoli difetti visti durante l'uso, corretti

- Nella pagina di registrazione c'era un testo scritto male ("SOTTOCrea il tuo account"),
  sostituito con una frase vera.
- Un utente a cui viene tolto ogni ruolo restava bloccato in un giro senza uscita fra due
  pagine. Ora vede subito un messaggio chiaro e un pulsante per uscire.
- Lo Staff vedeva un pulsante per configurare le fasce orarie che non può usare (solo l'Admin
  può farlo). Ora quel pulsante compare solo per l'Admin.
- Da login e registrazione non era ovvio come tornare alla pagina pubblica del locale: aggiunto
  un link visibile.
- Il menu a tendina per scegliere la fascia oraria, nel modulo di prenotazione, in tema scuro
  poteva diventare illeggibile (testo chiaro su sfondo chiaro). Corretto.
- Il menu laterale è stato riordinato in modo più logico: prima le cose che si usano ogni
  giorno (Dashboard, Prenotazioni), poi la configurazione della sala, poi la gestione utenti.
- Se qualcuno visita un indirizzo che non esiste, ora vede una pagina "non trovata" invece di un
  errore tecnico.

**Da provare a mano:** aprire l'app in tema scuro, verificare menu e tendine; provare a togliere
tutti i ruoli a un utente di prova e vedere cosa succede al login.

### 3. Le prenotazioni dimenticate ora si vedono

**Il problema.** Se una prenotazione veniva creata ma nessuno la confermava (né il cliente né lo
staff), restava per sempre nello stato "da confermare", anche a distanza di mesi, come se fosse
ancora attuale.

**La soluzione.** Ora esiste uno stato nuovo, "Non presentata": ogni notte il programma controlla
le prenotazioni del giorno prima mai confermate e le sposta in questo stato. Da lì si possono solo
cancellare (come le prenotazioni annullate), non modificare né confermare più.

**Da provare a mano:** creare una prenotazione per oggi, non confermarla, forzare il controllo
notturno da pannello Admin, verificare che passi allo stato "Non presentata".

### 4. Informazioni che il programma aveva ma non mostrava

Due dati esistevano già nel database ma non arrivavano mai a schermo:

- **Quanti posti occupa ogni tavolo** in una prenotazione con più tavoli uniti (prima si vedeva
  solo il numero del tavolo, non quanti posti gli erano stati assegnati).
- **Il numero del turno** (1º turno, 2º turno...) accanto all'orario, utile quando in una serata
  ci sono più fasce.

Ho anche sistemato l'ordine con cui vengono elencate le fasce orarie nella pagina di
configurazione: prima non avevano un ordine, ora sono in ordine di giorno della settimana e
orario.

**Da provare a mano:** guardare la tabella delle prenotazioni e la pagina delle fasce orarie.

### 5. I log (il diario tecnico del programma) sono più leggibili

Questo è lavoro "dietro le quinte", utile a chi in futuro deve capire cosa è successo su
Azure guardando i log. Ho reso ogni riga di log più chiara e ho scoperto e corretto un difetto
sottile: certi errori "normali" (per esempio: "quella fascia è già piena") venivano segnati nei
log come errori gravi del server, quando in realtà erano solo rifiuti normali già gestiti bene.
Ora vengono segnati correttamente.

**Non richiede nessuna prova visiva**: è un miglioramento che si vede solo guardando i log su
Azure dopo la pubblicazione.

### 6. L'aspetto dell'app è stato rifatto, ispirandosi a Docker Hub

Fabio aveva chiesto un aspetto più moderno, prendendo come esempio il sito Docker Hub (non per
copiarlo: solo per la sensazione d'insieme, senza usare nulla che sia loro — nessun logo, nessun
nome, nessun testo).

**Cosa è cambiato guardando l'app:**

- **Il menu è passato da laterale a una barra in alto**, fissa, sempre visibile mentre si scorre
  la pagina. Le voci sono in fila orizzontale, quella della pagina in cui ci si trova è
  sottolineata in blu. In alto a destra c'è un cerchietto con le iniziali dell'utente: cliccandolo
  si apre un piccolo menu con email, ruoli e il pulsante per uscire.
- **Il carattere del testo è cambiato** (da uno più "da cartellone" a uno più neutro e pulito,
  che si legge come un programma, non come un'insegna).
- **Gli angoli arrotondati sono più semplici**: prima c'erano tre misure diverse a seconda
  dell'elemento, ora sono solo due (un raggio piccolo per pulsanti/caselle/tabelle, uno un po'
  più grande per le finestre che si aprono sopra la pagina, tipo i moduli di modifica).
- **Le tabelle sono più leggibili**: l'intestazione delle colonne ora ha uno sfondo leggermente
  diverso ed è in maiuscolo piccolo, e le righe sono un po' più alte, meno compresse.
- **Le finestre che si aprono** (per esempio "Nuova prenotazione") ora hanno una linea sottile
  che separa il titolo dal resto, oltre a quella che già separava i pulsanti in basso — durante
  il lavoro ho anche trovato e corretto un piccolo difetto di allineamento che c'era già da prima
  in quelle finestre (un margine leggermente sbagliato, invisibile finché non ho cambiato gli
  angoli arrotondati e si è visto).
- **La pagina pubblica del locale** (quella che vede chi non ha un account) ora ha un'intestazione
  fissa in alto come il resto dell'app, il pulsante principale porta dritto al modulo per
  controllare la disponibilità invece che alla registrazione, e le tre zone del locale sono
  diventate tre schede separate invece di un unico riquadro diviso da righe.

**Verificato guardando davvero le pagine** (non solo controllando che il codice compili): ho
aperto l'app in un browser controllato dal programma, sia in tema chiaro che scuro, su schermo
grande e su schermo piccolo come un telefono, controllando dashboard, prenotazioni, fasce orarie,
un modulo di prenotazione e il menu del telefono aperto. Tutto funziona bene insieme alle
funzionalità aggiunte nelle fasi precedenti (turno, coperti per tavolo, stato "Non presentata").
Le immagini di controllo sono salvate nella cartella del progetto, per chi vuole vederle.

**Cosa non ho potuto controllare con lo strumento automatico**: come appare il menu a tendina
delle fasce orarie quando è aperto — quella parte la disegna il sistema operativo del computer,
non la pagina web, quindi uno strumento automatico non riesce a fotografarla. Il codice che lo
regola è comunque corretto; andrà controllato a occhio la prima volta che si usa l'app davvero.

### 7. La dashboard è diventata utile durante il servizio, non solo a inizio giornata

Prima la dashboard mostrava solo la giornata di oggi, ferma finché non si ricaricava la pagina a
mano. Ora:

- **Si può scorrere avanti e indietro fra i giorni** con due frecce, un pulsante "Oggi" e un
  campo data, senza dover cambiare pagina.
- **Si aggiorna da sola ogni minuto**, e in alto a destra dice a che ora l'ha fatto l'ultima
  volta. Se qualcuno crea o conferma una prenotazione da un'altra pagina del programma, la
  dashboard lo vede subito, senza aspettare il minuto.
- **Cliccando su una fascia oraria o su un giorno della settimana**, si viene portati dritti
  all'elenco delle prenotazioni di quel momento, già filtrato: prima bisognava andare
  nell'altra pagina e impostare i filtri a mano.
- **È comparso un blocco "In arrivo"**: le prossime cinque prenotazioni del giorno, a partire
  da adesso, con il pulsante per confermarle direttamente da lì — utile durante il servizio,
  quando si vuole sapere velocemente "chi arriva ora" senza scorrere tutta la tabella.
- **La tabella della settimana ora mostra anche quanti coperti su quanti possibili** per ogni
  giorno, con una piccola barra colorata come quella grande in cima, e per i giorni già passati
  quante prenotazioni non si sono presentate.

**Verificato guardando davvero le pagine**: ho fatto login, cliccato le frecce per cambiare
giorno, cliccato su un giorno della settimana e controllato che la pagina delle prenotazioni si
aprisse già filtrata su quella data — funziona.

### 8. Ho iniziato il controllo a occhio del nuovo aspetto (non finito)

C'era una lunga lista di controlli da fare a mano sull'app (quasi 90 punti), pensata perché
Fabio la eseguisse con calma. Ne ho fatti io una parte — quelli più importanti, segnalati come
prioritari nella lista stessa — usando uno strumento che apre l'app in un browser controllato dal
programma.

**Ho trovato e corretto un problema vero**: nella pagina "Tavoli", se ci sono molte fasce orarie
configurate (come nei dati di prova), l'elenco riassuntivo in cima diventava così lungo da
spingere il resto della pagina — inclusa la tabella dei tavoli veri — fuori dalla prima
schermata. Ora quell'elenco sta dentro un riquadro con una barra di scorrimento propria, e il
resto della pagina resta sempre visibile.

Ho anche controllato che il tema scuro sia davvero un grigio neutro (misurandolo, non solo
guardandolo) e che il tasto Tab della tastiera mostri chiaramente dove ci si trova, entrambi
punti che erano segnalati come delicati.

**Cosa resta**: circa l'80% di quella lista (quasi 80 punti su 90) non l'ho ancora controllata —
in particolare tutto quello che va provato su schermo medio e su telefono, e il tocco vero con le
dita (uno strumento automatico non può simularlo davvero). Questa parte tocca a Fabio, con la
lista già pronta e aggiornata in `GestoraDocs/verifica-redesign.md`.

### 9. Pulizie tecniche

Piccole cose lasciate indietro durante lo sviluppo:

- Una libreria usata solo per debug (`react-query-devtools`) era elencata fra quelle necessarie
  al funzionamento vero dell'app; ora è al suo posto corretto, e ho verificato che non finisca
  comunque nel pacchetto pubblicato online.
- Tolti gli ultimi file rimasti dal vecchio hosting Railway (un file di configurazione e una
  cartella vuota), ormai inutili dopo il passaggio ad Azure.
- Ho controllato che non ci siano librerie con problemi di sicurezza noti: sul frontend nessuna,
  sul backend **una** — una libreria di supporto (AutoMapper) segnala un avviso di sicurezza di
  livello alto nella versione usata. La correzione richiederebbe di saltare a una versione molto
  più recente, che però ha cambiato le condizioni di licenza (diventa a pagamento sopra una certa
  soglia di fatturato aziendale): non è una scelta solo tecnica, quindi non l'ho fatta da solo.
  **Serve una decisione di Fabio.**

### 10. La documentazione ora racconta la verità

Molti file di istruzioni (per me e per chi riprende il progetto) parlavano ancora di Railway,
il vecchio hosting abbandonato a metà settembre. Li ho riscritti per Azure e Neon (quelli veri,
oggi): come si fa un backup, come si cambia la password del database, come si pubblica una
nuova versione, cosa fare se qualcosa va storto.

Ho anche scritto per bene, in un punto solo, la risposta al primo dubbio che avevi sollevato
all'inizio di tutto questo lavoro: se il "codice di accesso" (token) che il browser salva sul
computer sia un rischio. La risposta resta la stessa di allora — non lo è, per come funziona
qualsiasi sito con login — ma ora è scritta in un posto preciso (`RUNBOOK.md`, sezione
Sicurezza) invece che spiegata a voce. Ho anche stretto di qualche minuto la scadenza del token,
che per una piccola distrazione tecnica durava 65 minuti invece dei 60 dichiarati.

Infine ho riletto il tuo file di appunti personali (`AppuntiFix.txt`, che non ho toccato) e
verificato che ogni singola annotazione fosse davvero stata affrontata in una delle fasi
precedenti — lo era, tranne il primo punto (separare backend e frontend in due progetti diversi),
che è il lavoro della prossima fase.

---

## I numeri di oggi

| | |
|---|---|
| Test automatici del backend | 256, tutti verdi (erano 239 all'inizio) |
| Test automatici del frontend | 45, tutti verdi (erano 35 all'inizio) |
| Verifica del codice (build) | pulita, nessun errore |
| Controllo di stile del codice (lint) | pulito |

---

## Un intoppo durante il lavoro, risolto

A un certo punto ho usato per sbaglio un comando che annulla le modifiche non salvate di un file
intero, pensando cancellasse solo l'ultima piccola modifica di prova che avevo fatto. Invece ha
cancellato **tutto** il lavoro non salvato su quel file, comprese modifiche buone fatte prima.
Me ne sono accorto subito controllando lo stato del progetto, e ho riscritto tutto da capo
verificando che i test tornassero tutti verdi come prima. Nessun lavoro è andato perso in modo
permanente (non era mai stato salvato su Git), solo riscritto. Da quel momento uso un metodo più
sicuro per fare queste prove.

---

### 11. Preparati i file per separare backend e frontend, non ancora fatto

L'ultimo appunto che avevi lasciato, il primo in ordine di importanza per te, chiedeva di
separare il backend e il frontend in due progetti Git indipendenti (oggi vivono in un unico
progetto). Ho preparato tutto il necessario — un file di presentazione per ciascuno dei due, le
istruzioni tecniche copiate al posto giusto, la procedura passo-passo scritta in `RUNBOOK.md` —
ma **non ho creato i due progetti veri né spostato nulla**: sono operazioni che toccano GitHub e
i pannelli di produzione, fuori da quello che posso fare da solo. La procedura è pronta quando
vorrai farla eseguire.

Ho anche preparato (senza eseguirlo, perché tocca il database di produzione) uno script per
ripulire i dati di prova rimasti su Neon, quando deciderai che il progetto è davvero pronto per
essere usato sul serio.

---

## Cosa resta da fare

- **Finire la verifica visiva** (fatti 14 controlli su circa 90, vedi punto 8 sopra): restano
  soprattutto le prove su schermo medio e telefono, e il tocco vero con le dita.
- **Decidere se aggiornare AutoMapper** (avviso di sicurezza, ma la versione che lo risolve ha
  cambiato le condizioni di licenza — vedi punto 9 sopra).
- **Eseguire davvero la separazione dei due progetti** (preparazione fatta, punto 11 sopra):
  richiede di creare due progetti nuovi su GitHub e ricollegare Vercel, cose che faccio solo se
  me lo chiedi esplicitamente.
- **Pulire i dati di prova sul database di produzione**, quando il progetto sarà pronto per essere
  usato sul serio (script già pronto, mai eseguito).

---

## Il lavoro è concluso, in attesa delle tue decisioni

Ho fatto tutto quello che potevo fare da solo, in autonomia, senza toccare produzione e senza
fare commit (li fai tu). Restano aperti solo i punti sopra, che o richiedono una tua decisione
(AutoMapper, verifica visiva a mano) o toccano direttamente la
produzione (reset di Neon).

**Numeri finali**: 256 test del backend verdi (erano 239 a inizio lavoro), 45 test del frontend
verdi (erano 35). Nessun errore di compilazione, nessun avviso di stile del codice, nessuna
vulnerabilità nelle librerie del frontend (una nel backend, segnalata, non risolta per la
questione della licenza).

---

## Come continua

Fabio decide se e quando far proseguire questo lavoro. Il file tecnico
`GestoraDocs/CONSEGNA_v1.1.md` ha tutti i dettagli per chi vuole entrare nel merito di cosa è
stato scritto, file per file — inclusi un giro di 20 controlli da fare a mano nel portale e la
sequenza esatta per pubblicare la versione 1.1 quando sarai pronto.
