# Gestora v2 — cosa resta da fare

Aggiornato il **07/10/2026**. Questo è **l'unico elenco valido** delle cose aperte: se una cosa
non è scritta qui, non è in programma.

Il foglio *Fix e Bug* del tracker resta il registro dettagliato dei difetti; questo file è la
vista d'insieme che si guarda per decidere cosa fare.

---

## Come si legge

**Sigla**: una sola sequenza, `V2-001`, `V2-002`… Non si riusa mai un numero, nemmeno se una voce
viene scartata.

**Tipo**: `bug` (qualcosa che non funziona come dovrebbe) · `richiesta` (una cosa nuova chiesta
da chi usa Gestora) · `miglioramento` (una cosa che c'è, fatta meglio) · `refactoring` (stesso
comportamento, codice più pulito) · `studio` (documentazione, analisi, crescita).

**Chi sviluppa**: si decide **prima di iniziare**, ogni volta (vedi `CLAUDE.md`, *Come
lavoriamo*): `io` (Fabio, guidato da Claude) oppure `Claude`. Finché non si è deciso, resta `—`.

**Stato**: `da fare` · `in corso` · `fatto` · `scartato`. Le voci `fatto` e `scartato` restano
qui fino al rilascio successivo, poi si tolgono.

---

## 📥 Segnalazioni dal locale — in arrivo

Quello che emerge dall'uso nei turni di servizio. Si annota qui **così come arriva**, senza
analizzarlo. Quando lo si prende in mano diventa una voce `V2-xxx` nella sezione sotto e si decide
chi la sviluppa.

Modello da copiare:

```
- Data: gg/mm/aaaa — Turno: pranzo/cena — Chi: (ruolo: Admin / Staff / Cliente)
  Schermata: (es. Prenotazioni, Dashboard, pagina pubblica)
  Cosa è successo: ...
  Cosa ci si aspettava: ...
  Quanto blocca: blocca il servizio / fastidioso / si può aspettare
```

*Nessuna segnalazione ancora.*

---

## 🔵 Da fare

| Sigla | Tipo | Cosa | Chi sviluppa | Stato |
|---|---|---|---|---|
| `V2-001` | studio | Riallineare documentazione e file di supporto all'avvio della v2 | Claude | fatto (05/10/2026) |
| `V2-002` | miglioramento | Tetto dei coperti per prenotazione configurabile (strada A) | io | fatto (06/10/2026) |
| `V2-003` | richiesta | Conferma dell'email alla registrazione di un utente | — | da fare |
| `V2-004` | studio | Agente/MCP personalizzato per lavorare su Gestora | — | da fare |
| `V2-005` | studio | Documentazione del progetto come linea guida per metterne in piedi altri | — | da fare |
| `V2-006` | richiesta | Gestire il tetto di 100 coperti e renderlo raggiungibile | — | scartato (06/10/2026) |
| `V2-007` | bug | Pagina pubblica: «Disponibilità residua: 58 coperti» e insieme «Pieno» quando mancano i tavoli | io + Claude | fatto (08/10/2026), su `v2` |

### `V2-002` — tetto dei coperti per prenotazione configurabile ✅
Scelta la strada A: il tetto sta in configurazione (`Prenotazioni:MaxCopertiPerPrenotazione` in
`appsettings.json`, **50**), controllato all'avvio, letto dai due validatori e passato al
frontend da `GET api/Prenotazione/limiti-prenotazione` (pubblico). Nella pagina pubblica, oltre il
tetto, il campo «Persone» resta bloccato dal browser e compare «Per prenotazioni superiori a N
persone contatta direttamente il ristorante». Per cambiarlo in produzione: variabile d'ambiente di
Azure `Prenotazioni__MaxCopertiPerPrenotazione` (la mette Fabio), nessun rilascio. 294 test
backend (+2), 64 frontend.

### `V2-003` — conferma dell'email
Alla registrazione l'utente dovrebbe confermare l'indirizzo prima di poter prenotare. Oggi non
esiste nessun invio di email: va scelto anche *come* inviarle (servizio esterno), quindi si
collega all'idea *Email di conferma e promemoria* qui sotto.

### `V2-007` — coerenza fra fasce, tavoli e prenotazioni ✅
Segnalato il 06/10/2026 provando la pagina pubblica. Esempio: 50 persone, fascia con tetto 60
coperti, in sala solo 2 tavoli da 2 posti. L'utente legge «Disponibilità residua: 58 coperti» e
accanto «Pieno»: i due dati si contraddicono. Ripreso il 07/10/2026: riprodotto in locale con il
seed di sviluppo (anche la fascia già finita compare come «Pieno»).

**Causa.** Il backend (`DisponibilitaService.CheckDisponibilitaAsync`) distingue già 4 motivi di
fascia non prenotabile — terminata, tetto esaurito, posti residui meno di quelli chiesti, tavoli
che non bastano — e ne scrive uno in `messaggio`. La pagina pubblica
(`components/landing/VerificaDisponibilita.tsx`, riga della fascia non libera) **ignora il
motivo** e mostra sempre «Disponibilità residua: N» + «Pieno». La riga che mostrava il motivo è
stata sostituita nel restyle (commit `a211a8b`), il commento sopra è rimasto. Stesso difetto per
una fascia già finita, che compare come «Pieno». I testi di `messaggio` sono pensati per lo Staff
(li usa `PrenotazioneModal`), troppo tecnici per un cliente.

**Soluzione scelta** (07/10/2026, idea di Fabio, sviluppa Fabio guidato):
1. **Motivo vero nella pagina pubblica**: campo `Motivo` in `FasciaDisponibilitaDTO` (Libera,
   Terminata, TettoEsaurito, PostiInsufficienti, TavoliInsufficienti) e un solo stato per riga.
   Serve comunque: un tavolo va sempre intero a un gruppo, quindi anche con una sala coerente
   possono restare posti sotto il tetto senza un tavolo che li accolga. Da solo chiude il bug.
2. **Limite online di 20 persone** per il Cliente e la pagina pubblica (oltre: «contatta il
   ristorante»); lo Staff resta a un limite tecnico di 50.
3. **Tetto della fascia ≤ posti della sala** (tavoli attivi in zone attive, somma semplice, senza
   bonus testate): controllato salvando o attivando una fascia **e** modificando tavoli e zone (la
   modifica che porterebbe i posti sotto un tetto è bloccata). Prima i tavoli, poi le fasce.
4. **Niente più limite di 4 tavoli per unione** (sempre nella stessa zona): il motore smette di
   aggiungere tavoli appena l'unione copre il gruppo.

Cambiano le decisioni di prodotto 4 e 8 e se ne aggiunge una (limite online): si scrivono in
`CLAUDE.md` §5 a lavoro finito. Produzione ancora vuota (setup non fatto): nessun dato da
correggere. Dettaglio e avanzamento: task `V2-007-T01`…`T28` nel foglio *Task* del tracker.

**Stato al 08/10/2026.** Analisi di Fabio, guidato (T01–T03). Sviluppo, test e documenti di Claude,
su richiesta di Fabio (T04–T22), tutto verificato anche dal vivo in locale. Decisioni di prodotto 4
e 8 riscritte e aggiunta la 11 in `CLAUDE.md` §5. Variabili Azure controllate da Fabio: nessuna da
impostare (T23).

**Dai test visivi di Fabio (08/10/2026)**, corretti da Claude dentro questa voce:
- T26 — nuovo testo del blocco su tavoli e zone («Errore: i posti della sala non possono scendere
  sotto il tetto delle fasce…»), con l'elenco delle fasce che bloccano;
- T27 — un tavolo disattivato spariva dalla pagina Tavoli e non si poteva più riattivare: nuovo
  elenco per la gestione con anche i tavoli disattivati (`get-tavoli-zona-gestione`);
- T28 — l'elenco prenotazioni partiva dallo storico più vecchio: ora prima oggi e il futuro, poi il
  passato; dopo una creazione lo Staff va al giorno della prenotazione.

Non erano difetti: il Cliente che vede 33 prenotazioni (il seed gliele assegna davvero) e il filtro
per data (prova errata). 338 test backend e 74 frontend verdi. T26–T28 verificate a vista da Fabio.

**Chiusa l'08/10/2026** (commit `a0b6bc2` su `v2`). **Rilascio rimandato per scelta**: `main` resta
fermo finché il locale non comincia a usare Gestora; il merge `v2` → `main` (T24) si fa quando lo
decide Fabio e porterà insieme `V2-002` e `V2-007`.

### `V2-004`, `V2-005`
Arrivano dal foglio *Appunti e Step* del tracker, dove erano segnate «Da fare». Da precisare
insieme prima di iniziare.

---

## ⚪ Idee

Non sono impegni: sono la lista da cui pescare quando una segnalazione dal locale o una scelta
nostra le rende utili.

**Prenotazioni**
- Turnover del tavolo: durata della seduta, due turni nella stessa fascia
- Lista d'attesa quando la fascia è piena
- No-show con storico e regole per cliente (oggi c'è solo lo stato `NonPresentata`)
- Overbooking controllato per fascia
- Zona come preferenza con ripiego, invece che come vincolo
- Chiusure straordinarie e orari speciali
- Vincolo "una prenotazione al giorno" per il Cliente nel database, non solo nell'applicazione:
  serve una colonna che distingua chi ha creato la prenotazione. Da riprendere insieme a
  un'eventuale app dedicata al cliente, non da sola

**Sala**
- Creazione automatica dei tavoli in base ai coperti richiesti
- Unione e separazione tavoli come azione manuale dello Staff
- Campo `PostiCapotavola` sul singolo tavolo, se un giorno serve precisione piena sul bonus
  testate anche per le unioni miste

**Utenti e comunicazione**
- Email di conferma e promemoria
- Recupero password autonomo
- Sessione con rinnovo automatico del token
- Autenticazione con cookie `HttpOnly` + protezione CSRF, al posto del token in `localStorage`.
  È una riprogettazione, non un fix: il token in `localStorage` non è un bug (vedi `RUNBOOK.md`
  §9), ma un cookie `HttpOnly` toglie anche la possibilità teorica di leggerlo da JavaScript

**Codice**
- Estrarre la logica di autenticazione da `AuthenticationUserController` in un service dedicato,
  così da poterla coprire con test (oggi è l'unico controller senza test). Buon esercizio di
  refactoring + testing

**Altro**
- Export CSV/PDF dei report — oggi **non esiste**: esistono solo i due endpoint della dashboard
- Gestione di più locali sullo stesso impianto

---

## 🚫 Rischi accettati e cose decise di non fare

Scritte qui perché tornano fuori ogni volta che qualcuno rilegge il codice. Si possono riaprire,
ma con una decisione esplicita (vedi `CLAUDE.md`, *Decisioni di prodotto*).

**AutoMapper resta alla 12.0.1**, con un avviso di sicurezza noto (`GHSA-rvv3-g6hj-g44x`, non
sfruttabile da remoto). La versione che lo risolve ha una licenza a pagamento oltre una soglia di
fatturato, e non si vuole pagare nessuna licenza. Da riaprire solo se si cambia libreria di
mapping.

**"Una prenotazione al giorno" per il Cliente è un controllo dell'applicazione.** Con due
richieste parallele su fasce diverse si può aggirare. Rischio accettato (decisione di prodotto
10); non riguarda il doppio tavolo, che è invece protetto da un vincolo vero nel database.

**Blocco del login dopo 5 tentativi in 15 minuti.** Chi conosce l'email dell'Admin può tenerlo
bloccato sbagliando la password apposta. È il prezzo della protezione contro i tentativi a forza
bruta, accettato.

**Job notturni al primo accesso del giorno.** Sul piano gratuito F1 di Azure l'applicazione si
spegne senza traffico, quindi i job Quartz partono quando arriva la prima richiesta. Per farli
partire all'ora giusta serve «Always On», cioè un piano a pagamento.

**Email unica controllata solo dall'applicazione** (Identity, `RequireUniqueEmail`), senza un
indice unico nel database: basta così.

**Nome "fascia oraria" non uniformato.** Lo stesso concetto si chiama in quattro modi diversi fra
namespace, classe, indirizzo dell'endpoint e tabella. Uniformarlo vorrebbe dire cambiare
l'indirizzo di un endpoint, cioè rompere i collegamenti del frontend, per un guadagno di sola
leggibilità. Cartelle e namespace sono già allineati, l'indirizzo resta com'è.

**Test automatici di concorrenza.** Il database finto usato nei test non applica gli indici unici
né i lock, e introdurre un database vero nei test è sproporzionato. La verifica resta manuale
(`RUNBOOK.md` §2).
