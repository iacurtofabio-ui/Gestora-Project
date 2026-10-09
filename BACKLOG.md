# Gestora v2 — cosa resta da fare

Aggiornato il **09/10/2026**. Questo è **l'unico elenco valido** delle cose aperte: se una cosa
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
| `V2-003` | richiesta | Conferma dell'email alla registrazione di un utente | — | da fare |
| `V2-004` | studio | Agente/MCP personalizzato per lavorare su Gestora | — | da fare |
| `V2-005` | studio | Documentazione del progetto come linea guida per metterne in piedi altri | — | da fare |
| `V2-010` | bug | La sessione scade dopo 60 minuti senza rinnovo: lo Staff viene buttato fuori ogni ora | Claude | in corso: sviluppata, da provare a vista |
| `V2-011` | bug (sicurezza) | Registrazione e verifica della disponibilità (pubbliche) senza limite di richieste | — | da fare |
| `V2-012` | bug | Spegnere una zona non controlla le prenotazioni future | — | da fare |
| `V2-013` | bug | `NomeCliente` senza limite di lunghezza nel validator | — | da fare |
| `V2-014` | bug | Registro attività: eliminazione non nella stessa transazione, modifica di nome/email non registrata | — | da fare |

Tolte il 09/10/2026, perché chiuse e in produzione: `V2-001`, `V2-002`, `V2-007`, `V2-008`,
`V2-009`, e `V2-006` scartata. La loro storia resta nel tracker (fogli *Fix e Bug* e *Task*) e
nella cronologia Git di questo file.

### `V2-003` — conferma dell'email
Alla registrazione l'utente dovrebbe confermare l'indirizzo prima di poter prenotare. Oggi non
esiste nessun invio di email: va scelto anche *come* inviarle (servizio esterno), quindi si
collega all'idea *Email di conferma e promemoria* qui sotto.

### `V2-010` — la sessione scade dopo 60 minuti
Punto 3 della code review dell'08/10/2026. Il token valeva 60 minuti e non si rinnovava: in
servizio lo Staff veniva buttato fuori ogni ora e perdeva il form che stava compilando.

**Soluzione scelta** (09/10/2026, strada A fra tre, sviluppa Claude): **rinnovo del token**.
- Backend: nuovo `POST api/AuthenticationUser/rinnova-token`, per qualsiasi utente con un token
  ancora valido. Rilegge ruoli e security stamp e dà un token nuovo da 60 minuti, **mai oltre 12
  ore dal login** (`JwtSettings:MaxSessionHours`, in configurazione e controllato all'avvio). Il
  claim `inizio_sessione` porta l'istante del login da un rinnovo all'altro.
- Frontend: `AuthProvider` chiede il rinnovo da solo 10 minuti prima della scadenza, finché la
  pagina è aperta. Se la rete non risponde riprova ogni minuto; al limite delle 12 ore smette, e
  alla scadenza si torna al login con «Sessione scaduta».
- Nessuna modifica al database. Il security stamp continua a respingere subito i token di un
  utente modificato o eliminato.

Scartate: B, refresh token in una tabella nuova (migration e molto codice per poca sicurezza in
più, con il token in `localStorage`); C, solo allungare la durata (la scadenza arriverebbe comunque
a metà turno).

351 test backend (+6, `JwtTokenGeneratorTests`), 82 frontend (+8). Chiude l'idea *Sessione con
rinnovo automatico del token*. Manca la prova a vista di Fabio (task `V2-010-T06`).

### Code review dell'08/10/2026 — punti ancora da affrontare
Revisione di Claude su backend e accesso del frontend. I punti 1 e 2 sono stati `V2-008` e
`V2-009`, il punto 3 è `V2-010` (sopra). Gli altri si prendono uno alla volta, decidendo ogni volta
chi sviluppa.
- `V2-011` (punto 4, **medio**) — registrazione e verifica della disponibilità (pubbliche) senza
  limite di richieste: account in massa, e il calcolo più pesante dell'applicazione ripetibile
  all'infinito.
- `V2-012` (punto 5, **medio**) — spegnere una zona non controlla le prenotazioni future (per i
  tavoli sì): restano assegnate a tavoli di una zona spenta, senza avviso.
- `V2-013` (punto 6, **basso**) — `NomeCliente` senza limite di lunghezza nel validator: oltre i
  200 caratteri del database l'utente vede un «errore interno».
- `V2-014` (punto 7, **basso**) — registro attività: eliminazione della prenotazione e sua traccia
  non nella stessa transazione; la modifica di nome/email di un utente non viene registrata.

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
