# Verifica manuale del redesign «Turno»

Lista di controllo per provare a mano il redesign. Ogni voce dice **dove andare**, **cosa fare** e
**cosa devi vedere**: il risultato atteso è scritto in modo che tu possa rispondere sì o no senza
sapere niente di frontend.

Non c'è una colonna di esito: i risultati me li riporti tu e li sistemo dopo.

Le voci marcate **⚠️** sono quelle dove mi aspetto che si rompa. Il perché è scritto in fondo alla
voce.

---

## Prima di cominciare

### Preparare i dati

Dalla cartella `GestoraWebApi`, su una riga sola:

```
dotnet run -- --seed-sviluppo
```

Cancella i dati di dominio del database **locale** e li riscrive con un dataset costruito per
rompere il layout. Non parte se l'ambiente non è Development, se manca l'argomento, o se la
stringa di connessione non punta a questa macchina.

Poi avvia backend e frontend come sempre (`dotnet run` e `npm run dev`).

### Entrare

| Utente | Email | Password | A cosa serve |
|---|---|---|---|
| Admin | `admin@gestora.local` | `Sviluppo1!` | quasi tutti i controlli |
| Staff | `staff@gestora.local` | `Sviluppo1!` | controlli sui permessi |
| Cliente | `cliente@gestora.local` | `Sviluppo1!` | controlli sui permessi |

Se hai già un tuo utente e vuoi solo rientrare, senza rifare il seed:

```
dotnet run -- --reimposta-password tua@email.it NuovaPassword1!
```

### Simulare il touch (serve davvero)

L'hover con il mouse nasconde metà dei difetti: su un telefono non esiste, quindi lo stato «a
riposo» è l'unico che l'utente vedrà. Per escluderlo davvero non basta stringere la finestra.

**Chrome / Edge:** F12 → l'icona del telefono in alto a sinistra nel pannello (o `Ctrl+Shift+M`) →
scegli **iPhone SE** dall'elenco dei dispositivi. In quella modalità il browser dichiara un
puntatore grosso, quindi l'hover non parte e le aree di tocco maggiorate si attivano.

⚠️ Non usare solo il righello per stringere la finestra: quello cambia la larghezza ma **lascia
l'hover attivo**, e i controlli sul touch risulterebbero superati per il motivo sbagliato.

### Le tre larghezze

- **Desktop** — finestra massimizzata
- **768px** — nel pannello dispositivi, larghezza 768
- **375px** — iPhone SE

---

## Le prime dieci voci

Se hai mezz'ora, fai queste e fermati. Sono quelle che pagano di più: toccano le decisioni
strutturali del redesign e le pagine che nessuno ha ancora guardato.

1. → [D1](#d1) — la banda della Dashboard, il cuore della direzione
2. → [P4](#p4) ⚠️ — azione di riga a riposo, la correzione più recente
3. → [T1](#t1) ⚠️ — le quattro pagine mai viste, a partire da Tavoli
4. → [P2](#p2) ⚠️ — i nomi lunghi nella tabella Prenotazioni
5. → [X1](#x1) ⚠️ — il menu «…» a 375px
6. → [U1](#u1) — Utenti, dove c'erano quattro pulsanti per riga
7. → [X4](#x4) ⚠️ — le tendine dentro il dialogo delle prenotazioni
8. → [D3](#d3) — la Dashboard a 375px, dove la banda va a capo
9. → [X6](#x6) — il contorno del fuoco da tastiera sul tema scuro
10. → [S2](#s2) — cosa si vede quando il backend è spento

---

## D — Dashboard

### Desktop

<a id="d1"></a>
**D1 — La banda del giorno**
Vai su Dashboard.
Devi vedere, nell'ordine dall'alto: il titolo «Oggi in sala» con la data a destra; sotto, un numero
grande, la capienza del giorno più piccola accanto, e la scritta «coperti prenotati sulla capienza
del giorno»; sotto ancora, una barra orizzontale a piena larghezza riempita in proporzione.
La barra deve essere **l'elemento più evidente della pagina**: niente altro sopra la piega ha un
colore pieno di quelle dimensioni.

**D2 — L'animazione d'ingresso**
Ricarica la pagina e guarda le barre delle fasce.
Devono crescere da sinistra partendo da zero, una dopo l'altra dall'alto verso il basso, e
fermarsi. Dura poco più di un secondo. Cambiando pagina e tornando indietro deve rifarlo.
Nient'altro nella pagina deve muoversi entrando.

**D3 — Le fasce di oggi**
Il seed ne mette **quattordici**, una per ogni ora dalle 8 alle 22.
Ogni riga: orario a sinistra, barra al centro, «prenotati / capienza» a destra.
La **prima** (08:00) deve essere piena, rossa, e finire con «· pieno».
La **seconda** (09:00) deve essere anch'essa piena e rossa: ha più coperti del suo tetto, ma la
scritta dice comunque 0 disponibili — è così di proposito, il backend non mostra numeri negativi
sulla singola fascia.
La **terza** (10:00) deve essere gialla, non rossa: è al 90%.
Le altre devono essere blu.

**D4 — I numeri di contorno**
Sotto le fasce, «Il resto della giornata»: quattro numeri in fila dentro un unico riquadro,
separati da linee sottili. Devono sembrare **un blocco solo**, non quattro riquadri staccati con
spazio in mezzo.

**D5 — La settimana**
In fondo, la tabella dei sette giorni. Deve essere silenziosa: nessun colore acceso, testo delle
intestazioni piccolo e grigio. Non deve competere con la barra in cima.

<a id="d6"></a>
**D6 — Oltre il tetto** ⚠️
Rilancia il seed nella variante piena, su una riga sola:
`dotnet run -- --seed-sviluppo --giornata-piena`
Ricarica la Dashboard. Accanto alla scritta «coperti prenotati sulla capienza del giorno» deve
comparire, in rosso, «· N oltre il tetto».
⚠️ *È l'unico modo per vedere quel testo: con dati normali il totale non supera mai la capienza,
quindi è codice che non è mai stato mostrato a nessuno.*
Poi rifai il seed normale prima di continuare.

### 768px

**D7** — Le fasce restano su una riga sola: orario, barra, numeri. La barra si accorcia ma resta
più lunga del testo che le sta accanto.
**D8** — I quattro numeri passano da quattro colonne a due, sempre dentro un riquadro solo.

### 375px

<a id="d3"></a>
**D9 — La banda va a capo** ⚠️
Ogni fascia occupa **due righe**: sopra orario e numeri, sotto la barra a tutta larghezza.
La barra non deve mai essere più corta del testo sopra.
⚠️ *È il punto in cui il concetto potrebbe non reggere: se la barra si accorcia troppo smette di
dire qualcosa a colpo d'occhio, e la direzione perde il suo elemento portante.*

**D10** — Il numero grande e la scritta «coperti prenotati…» stanno su due righe distinte. Il
numero non deve mai essere spezzato con la didascalia in mezzo.
**D11** — La pagina non scorre lateralmente. Nessuna barra di scorrimento orizzontale in fondo.

---

## P — Prenotazioni

### Desktop

**P1 — L'intestazione**
Il titolo «Prenotazioni» sta **fuori** dal riquadro della tabella, con accanto il conteggio in
piccolo. Il pulsante blu pieno «Aggiungi prenotazione» è **l'unico pulsante pieno della pagina**.

<a id="p2"></a>
**P2 — I nomi lunghi** ⚠️
Nell'elenco c'è «Maria Vittoria Della Rovere Castiglioni-Buonaparte».
Il nome deve essere **tagliato con i puntini** dentro la sua colonna. Le colonne accanto non si
devono spostare, la riga non deve diventare più alta delle altre, e la tabella non deve allargarsi.
⚠️ *Le colonne hanno larghezze fisse: è esattamente la scelta che cede con i contenuti lunghi.
Se qualcosa si rompe, si rompe qui.*

**P3 — La colonna Tavoli**
Alcune righe hanno due tavoli, altre nessuno.
Con due tavoli devi vedere i numeri separati da virgola e la zona dopo un puntino. Senza tavoli
devi vedere un trattino, non una cella vuota.

<a id="p4"></a>
**P4 — L'azione di riga a riposo** ⚠️
Sposta il mouse **fuori dalla tabella** e guarda la colonna Azioni.
Su ogni riga «Attiva» devi vedere «Conferma»; su ogni riga «Confermata», «Completa».
A riposo devono essere **riconoscibili come pulsanti**: testo blu con un contorno sottile intorno,
non testo colorato e basta.
⚠️ *È la correzione più recente e non l'ho ancora vista a schermo. Su touch questo è l'unico stato
che esiste: se qui non sembra un pulsante, su telefono non lo sembra mai.*

**P5 — L'azione di riga sotto il mouse**
Passa il mouse su una riga qualsiasi.
Il pulsante di quella riga si riempie di blu con il testo bianco. Deve accendersi passando **su un
punto qualsiasi della riga**, non solo centrando il pulsante. Le altre righe non cambiano.

<a id="x1"></a>
**P6 — Il menu «…»**
Clicca i tre puntini di una riga «Attiva» da Admin.
Il menu si apre **sotto il pulsante, allineato a destra**, e contiene: «Modifica prenotazione»,
«Annulla prenotazione», una riga di separazione, «Elimina definitivamente» in rosso.
L'eliminazione non deve mai comparire come pulsante nella riga.

**P7 — Gli stati**
Nella colonna Stato ogni riga ha un pallino e una parola: giallo/Attiva, verde/Confermata,
grigio/Completata, rosso/Annullata **con la parola barrata**.
Sulle righe Completata la colonna Azioni mostra un trattino: niente pulsanti, niente menu.

**P8 — I filtri e la lista vuota**
Filtra per una data del mese prossimo in cui non c'è niente (per esempio un 25).
La tabella mostra un messaggio che dice di provare un altro giorno **e un pulsante «Azzera i
filtri»**. Premendolo, l'elenco torna pieno.

**P9 — Il dialogo di annullamento**
Menu «…» → «Annulla prenotazione».
Si apre una finestra con il titolo «Annullare questa prenotazione?». I due pulsanti in fondo
devono dire **«Torna indietro»** e **«Annulla prenotazione»**: non devono avere lo stesso verbo.

**P10 — Il dialogo di eliminazione**
Menu «…» → «Elimina definitivamente».
Il testo spiega che la prenotazione sparisce dallo storico e suggerisce di usare Annulla se serve
solo liberare i tavoli. Il pulsante di conferma è rosso.

**P11 — La paginazione**
In fondo: «1-20 di N» a sinistra, i pulsanti a destra. «Precedente» è spento sulla prima pagina.
Passando alla seconda, la tabella non deve svuotarsi né saltare.

### 768px

**P12** — La barra dei filtri va su una riga sola con il pulsante «Aggiungi prenotazione» a destra.
**P13** — La tabella scorre lateralmente **dentro il suo riquadro**. La pagina intera non scorre.

### 375px (in modalità dispositivo)

<a id="x1-mobile"></a>
**P14 — Il menu «…» sul telefono** ⚠️
Apri il menu di una riga.
Deve aprirsi **dentro lo schermo** — non tagliato a destra, non fuori dal bordo. Deve chiudersi
toccando fuori. Toccando una voce deve partire quell'azione e non un'altra.
⚠️ *È lo scenario peggiore: menu vicino al bordo destro, schermo stretto, e un dito è molto meno
preciso di un puntatore. Se qualcosa sbaglia posizione, è qui.*

**P15 — L'area di tocco** ⚠️
Sempre in modalità dispositivo, prova a toccare «Conferma» e i tre puntini mirando **al bordo**
del pulsante, non al centro.
Devono rispondere lo stesso: l'area sensibile è più grande di quello che si vede.
⚠️ *Dipende da una regola che si attiva solo quando il browser dichiara un puntatore grosso: se
hai stretto la finestra invece di usare la modalità dispositivo, questo controllo non prova niente.*

**P16** — I filtri vanno uno sotto l'altro, il pulsante «Aggiungi prenotazione» sta sopra o sotto
ma non si sovrappone a niente.

---

## T — Tavoli

> Questa pagina e le tre che seguono sono state riscritte ma **non le ha ancora guardate nessuno**.

### Desktop

<a id="t1"></a>
**T1 — Il riepilogo in cima** ⚠️
Vai su Postazioni.
In alto: titolo «Tavoli» con accanto «N attivi · M posti». Sotto, l'elenco «I tavoli bastano a
coprire il tetto?»: una riga per fascia con giorno, orario e la frase «X posti su Y di tetto».
Dove i tavoli non bastano la riga finisce con «— i tavoli non bastano» in giallo.
⚠️ *Il seed mette quattordici fasce oggi più due per ogni altro giorno: questo elenco diventa
lungo. Va verificato che resti leggibile e non spinga la tabella dei tavoli fuori dalla prima
schermata.*

**T2 — La zona vuota**
Nel menu delle zone scegli **«Sala nuova»**.
La tabella deve mostrare un messaggio che dice che in quella zona non c'è ancora nessun tavolo, e
un pulsante «Aggiungi il primo tavolo». Non deve sembrare un errore né restare vuota.

**T3 — Prima di scegliere**
Ricarica la pagina senza scegliere nessuna zona.
Devi leggere «Scegli una zona qui sopra per vedere i suoi tavoli». Il pulsante «Aggiungi tavolo»
in alto è **spento** finché non scegli una zona.

**T4 — La zona con il nome lungo** ⚠️
Scegli «Sala privata al primo piano con vista sul giardino d'inverno».
Il nome nel menu a tendina non deve sfondare il riquadro né tagliare la freccia.
⚠️ *Il menu ha larghezza fissa (200px) e quel nome è quattro volte più lungo.*

**T5 — Le azioni**
Ogni riga ha «Modifica» e i tre puntini. Nel menu, «Elimina tavolo» in rosso sotto un separatore.
Il dialogo di eliminazione deve dire che la sala perde quei posti e suggerire di disattivare il
tavolo invece di eliminarlo.

**T6 — Stato dei tavoli**
Un tavolo per zona è disattivato: nella colonna Stato deve avere pallino grigio e «Non attiva».

### 768px e 375px

**T7** — A 375px il riepilogo in cima manda la frase «X posti su Y di tetto» a capo sotto giorno e
orario, senza sovrapposizioni.
**T8** — La tabella dei tavoli scorre dentro il suo riquadro.

---

## Z — Zone

**Z1** — Titolo «Zone» fuori dal riquadro, conteggio accanto, «Aggiungi zona» pieno a destra.
**Z2** — Cinque zone. Quella con il nome lungo deve essere tagliata con i puntini, non mandare a
capo la riga.
**Z3** — «Veranda (chiusa per lavori)» ha pallino grigio e «Non attiva».
**Z4** — Menu «…» → «Elimina zona»: il dialogo dice che spariscono anche i tavoli e suggerisce di
disattivarla.
**Z5 — Da Staff** — Esci, entra come `staff@gestora.local`. La colonna Azioni e il pulsante
«Aggiungi zona» **non ci sono**: lo Staff legge e basta.
**Z6 — A 375px** — La tabella scorre dentro il riquadro, la pagina no.

---

## F — Fasce orarie

**F1** — Titolo «Fasce orarie», conteggio, «Aggiungi fascia».
**F2** — **Ventisei righe.** Orario in evidenza a sinistra, giorno, tetto a destra, stato.
Verifica che l'elenco resti leggibile e che le colonne siano allineate anche scorrendo.
**F3** — La fascia serale del **lunedì** è disattivata: pallino grigio, «Non attiva».
**F4 — Il dialogo di eliminazione** ⚠️
Menu «…» → «Elimina fascia». Il titolo dice «Eliminare questa fascia oraria?» e il testo **nomina
la fascia**, per esempio «Il turno delle 19:00–23:00 di venerdì smette di esistere…».
⚠️ *Il nome del giorno viene da una tabella indicizzata sul numero del giorno: se l'indice è
sfasato, il testo nomina il giorno sbagliato — e sarebbe un errore che si nota solo leggendo.*
**F5 — A 375px** — La tabella scorre dentro il riquadro.

---

## U — Utenti

<a id="u1"></a>
**U1 — La riga** ⚠️
Vai su Admin Utenti (da Admin).
Ogni riga: nome, email, ruoli **come testo separato da virgole** (non pillole grigie), e nella
colonna Azioni «Modifica» più i tre puntini.
⚠️ *Prima questa pagina aveva quattro pulsanti in fila più l'eliminazione: è quella che è cambiata
di più, ed è l'unica dove il menu «…» contiene due voci oltre alla distruttiva.*

**U2 — Il menu**
Tre puntini → «Gestisci i ruoli», «Reimposta la password», separatore, «Elimina utente» in rosso.
**U3 — Te stesso**
Sulla riga del tuo utente (`admin`) devi vedere «· sei tu» accanto al nome, e nel menu la voce
«Elimina utente» deve essere **spenta**.
**U4 — L'utente con tre ruoli**
La riga di «Maria Vittoria…» mostra «Admin, Staff, Cliente». L'email lunghissima deve essere
tagliata con i puntini senza allargare la tabella.
**U5 — L'utente senza ruoli**
La riga `senza.ruoli` deve mostrare «nessun ruolo» in grigio, non una cella vuota.
**U6 — I modali**
Apri «Modifica», «Gestisci i ruoli» e «Reimposta la password». In tutti e tre il pulsante di
conferma dice cosa fa («Salva modifiche», «Ho finito», «Reimposta password») e c'è una riga di
separazione sopra i pulsanti.
**U7 — A 375px** — La tabella scorre dentro il riquadro; il menu «…» si apre dentro lo schermo.

---

## L — Pagina pubblica

**L1 — Senza account**
Esci e vai su `/`. Devi vedere la vetrina, non il login.
**L2 — I tre passaggi** ⚠️
Sezione «Come funziona»: tre passaggi collegati da **una linea sottile continua**, con i numeri 1,
2, 3 come pallini **sulla linea**.
Non devono essere tre riquadri separati con il numero ripetuto sopra ogni titolo.
⚠️ *La linea è disegnata dietro i pallini e cambia direzione fra desktop e telefono: è il punto in
cui rischia di restare scoperta o disallineata rispetto ai pallini.*
**L3 — A 375px** — La linea diventa **verticale** a sinistra, i pallini restano sopra la linea, i
tre passaggi si incolonnano.
**L4 — Le zone**
Sezione «Le zone»: tre voci dentro un riquadro unico diviso da linee sottili, non tre riquadri
staccati.
**L5 — La verifica disponibilità**
Compila giorno e persone, premi «Vedi i turni liberi».
I turni liberi mostrano un pallino verde e «Libero»; quelli pieni un pallino grigio e «Pieno», con
il motivo in piccolo accanto (solo da 640px in su).
**L6 — Errore della verifica**
Spegni il backend e riprova. Deve comparire in rosso un testo che dice di riprovare o di chiamare,
e la pagina deve restare navigabile.

---

## S — Stati che di solito non si vedono

<a id="s2"></a>
**S1 — Caricamento** ⚠️
Con gli strumenti di sviluppo aperti, scheda **Rete**, imposta «Slow 4G» nel menu della velocità,
poi ricarica Dashboard e Prenotazioni.
Devi vedere delle barre grigie **della forma del contenuto che poi arriva**: sulla Dashboard un
blocco largo in cima e righe con la barra al centro; su Prenotazioni le colonne con le larghezze
giuste. Quando i dati arrivano, **la pagina non deve saltare**: le cose devono comparire dove
c'era la barra grigia.
⚠️ *Se una colonna è larga nello scheletro e stretta nella tabella vera, il salto si vede — ed è
peggio che non mettere lo scheletro.*

**S2 — Errore di caricamento**
Spegni il backend (`Ctrl+C` nella finestra di `dotnet run`), poi ricarica Prenotazioni.
Devi vedere un riquadro con bordo rosso, un triangolo di avviso, il titolo «Non riesco a caricare
i dati», una spiegazione che parla di server non raggiungibile, e un pulsante **«Riprova»**.
Riaccendi il backend e premi «Riprova»: la tabella deve popolarsi senza ricaricare la pagina.
Ripeti su Dashboard, Zone, Tavoli, Fasce e Utenti.

**S3 — Elenco vuoto**
Su Prenotazioni filtra per una data senza prenotazioni (vedi P8). Su Tavoli scegli «Sala nuova»
(vedi T2). In entrambi i casi il messaggio deve **invitare a fare qualcosa**, non solo constatare
il vuoto.

**S4 — Permesso negato**
Entra come `cliente@gestora.local` e scrivi a mano `/dashboard` nella barra dell'indirizzo.
Devi vedere una schermata con il titolo «Questa pagina non è aperta al tuo ruolo», il tuo ruolo fra
parentesi, e un pulsante «Torna alle tue pagine» che ti riporta a Prenotazioni.
Sempre da Cliente, apri Prenotazioni: devi vedere solo le tue, senza filtri di data e stato.

**S5 — Invio del form fallito**
Vai su `/login` e inserisci una password sbagliata.
Sopra il pulsante deve comparire un riquadro con bordo rosso e triangolo che dice «Email o password
non corrispondono a nessun account. Controlla e riprova.»
Deve essere **visibilmente diverso** dagli errori dei singoli campi (che sono testo rosso piccolo
sotto il campo, senza riquadro).

**S6 — Backend spento durante un salvataggio**
Apri «Aggiungi prenotazione», compila tutto, spegni il backend, premi «Crea prenotazione».
Deve comparire un avviso temporaneo in alto a destra che dice che il server non è raggiungibile.
La finestra deve restare aperta con i dati compilati, non chiudersi perdendo tutto.

**S7 — Configurazione mancante**
Rinomina `.env.local` in `.env.local.bak`, riavvia `npm run dev` e apri il sito.
Devi vedere «Manca l'indirizzo del server» con le istruzioni. Poi rimetti il nome giusto.

---

## X — Controlli trasversali

Questi vanno fatti una volta, ma **in entrambi i temi**. L'interruttore del tema è in alto a
destra, accanto a «Esci».

**X0 — Il tema scuro non è marrone** ⚠️
Passa al tema scuro e guarda uno spazio vuoto grande della pagina.
Il grigio deve essere **neutro**: nessuna sfumatura calda, nessun sentore di marrone o di seppia.
Confrontalo con il nero del testo di un editor accanto, se aiuta.
⚠️ *È il difetto numero 3 di partenza: è la cosa che va guardata per prima e con più sospetto,
perché il calore residuo si nota solo su superfici grandi.*

**X1 — I quattro livelli di superficie (tema scuro)**
Su Prenotazioni, in tema scuro, apri un dialogo qualsiasi.
Devi distinguere **quattro grigi diversi**: lo sfondo della pagina (il più scuro), la barra
laterale, il riquadro della tabella, e la finestra del dialogo (il più chiaro).
Devono essere gradini regolari: nessuno dei quattro deve sembrare uguale a quello accanto.

<a id="p4-hover"></a>
**X2 — Azione di riga: riposo, mouse, tocco** ⚠️
Ripeti su Prenotazioni, Zone, Tavoli, Fasce, Utenti.
A riposo: contorno sottile e testo blu. Sotto il mouse (entrando dalla riga): pieno blu, testo
bianco. In modalità dispositivo: resta sempre nello stato a riposo, e deve bastare a capire che è
premibile.
⚠️ *Cinque pagine, tre stati, due temi. È la superficie più ampia toccata dalla correzione, ed è
anche quella dove basta una pagina dimenticata perché resti il vecchio aspetto.*

**X3 — Il menu «…»**
Su ognuna delle cinque pagine: si apre allineato a destra sotto il pulsante, resta dentro lo
schermo, si chiude cliccando fuori o con `Esc`.
Passando il mouse su una voce, lo sfondo della voce si schiarisce di poco e **il testo resta dello
stesso colore**. Non deve esserci nessuna inversione bianco-su-colore.

<a id="x4"></a>
**X4 — Le tendine dentro i dialoghi** ⚠️
Apri «Aggiungi prenotazione», scegli una data, poi apri la tendina «Fascia oraria» e quella «Zona
preferita».
Muovendo il mouse sulle voci, l'evidenziazione deve essere **un grigio appena più chiaro del
fondo della finestra**, con il testo che resta nero (o bianco in tema scuro). Nessuna campitura
colorata, nessun testo che si inverte.
La voce evidenziata deve restare distinguibile da quella sotto.
Fai la stessa cosa sulla tendina dei filtri di Prenotazioni e su quella delle zone in Tavoli.
⚠️ *Era il difetto che avevi trovato tu. La finestra e il menu partono dalla stessa superficie: se
il gradino è troppo piccolo l'evidenziazione sparisce, se è troppo grande torna a sembrare una
campitura.*

**X5 — Selezionata contro evidenziata**
Nella tendina «Fascia oraria», dopo aver scelto una voce riaprila.
La voce **scelta** ha una spunta a destra e il testo un po' più marcato. La voce **sotto il mouse**
ha lo sfondo schiarito. Devono essere due segnali diversi, non due sfondi colorati diversi.

<a id="x6"></a>
**X6 — Il contorno del fuoco da tastiera** ⚠️
Senza toccare il mouse, premi `Tab` ripetutamente su: Prenotazioni, un dialogo aperto, un menu
aperto, la barra laterale.
Ogni elemento che riceve il fuoco deve avere **un contorno blu chiaramente visibile**, su tutte le
superfici: sfondo pagina, barra laterale, riquadro tabella, finestra del dialogo.
⚠️ *Ho misurato i numeri e stanno tutti sopra il doppio della soglia, ma il contorno può comunque
risultare poco visibile se cade a cavallo di due superfici o se viene tagliato da un bordo.*

**X7 — Tab ed Esc dentro le azioni di riga**
Su una riga «Attiva»: `Tab` fino a «Conferma», poi `Tab` porta ai tre puntini. `Invio` apre il
menu, le frecce scorrono le voci, `Esc` chiude — **e il contorno del fuoco torna sui tre puntini**,
non salta a inizio pagina.

**X8 — Il movimento ridotto**
Nelle impostazioni di Windows: Accessibilità → Effetti visivi → spegni «Effetti di animazione».
Ricarica la Dashboard: le barre devono essere **già alla loro lunghezza**, senza crescere. Tutto
il resto deve continuare a funzionare.

**X9 — Il carattere**
In qualsiasi pagina, i numeri incolonnati (coperti, orari, capienze) devono essere **allineati in
verticale**: le cifre devono stare tutte sulla stessa colonna, come su un estratto conto. Se
ballano, la cifra non sta usando la spaziatura fissa.

**X10 — Nessuno scorrimento orizzontale**
A 375px, su tutte le pagine: nessuna barra di scorrimento in fondo alla **pagina**. Le tabelle
possono scorrere, ma solo dentro il loro riquadro.

---

## Dove mi aspetto che si rompa

In ordine di sospetto:

1. **P2, T4, U4 — i contenuti lunghi contro le colonne fisse.** Ho scelto larghezze fisse perché
   le colonne non ballino da una pagina all'altra, e il taglio con i puntini le protegge. Ma è una
   scommessa che finora ho verificato solo con nomi corti.
2. **P4 e X2 — l'azione di riga a riposo.** L'ho corretta dopo la tua osservazione e non l'ho
   ancora vista a schermo, su nessuna delle cinque pagine.
3. **X4 — le tendine nei dialoghi.** Il gradino di evidenziazione è calibrato sui numeri; se
   percettivamente è troppo debole in tema scuro me ne accorgo solo guardandolo.
4. **T1 — il riepilogo della sala con quattordici fasce.** Prima erano quattro righe in una
   tabella: ora sono un elenco lungo sopra il contenuto vero, e potrebbe spingerlo troppo in basso.
5. **P14 — il menu «…» a 375px.** Vicino al bordo destro su schermo stretto è lo scenario in cui il
   posizionamento automatico sbaglia più spesso.
6. **S1 — gli scheletri di caricamento.** Le larghezze sono le stesse costanti della tabella, ma
   solo su Prenotazioni: sulle altre pagine le ho scritte a mano e potrebbero non corrispondere.

## Quello che ho già verificato, e come

Non serve rifarlo, te lo dico per sapere dove **non** cercare:

- **Contrasto di tutte le coppie testo/fondo**, in entrambi i temi: `node scripts/contrasto.mjs`
  dalla cartella `gestora-frontend`. Comprende i bordi dei campi, il contorno del fuoco sulle
  quattro superfici, l'evidenziazione dei menu e il contorno dell'azione di riga.
- **Coerenza di tinta** fra il testo e la campitura dello stesso stato: stesso script.
- **Tab, apertura del menu, `Esc` che riporta il fuoco**: nove test automatici
  (`npm test`, file `AzioniPrenotazione.test.tsx`).
- **Nessuno spostamento del layout al caricamento del carattere** (CLS = 0), misurato nel browser.
- **Nessun colore fisso fuori dai token**, verificato cercandoli nel codice.
