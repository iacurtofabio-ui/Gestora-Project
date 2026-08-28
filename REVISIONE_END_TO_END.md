# Revisione end-to-end — Gestora v1.0.0

Data revisione: 28/08/2026 · Perimetro: `GestoraDocs` (gestione) + `Gestora` (codice)
Riferimenti verificati su `dev`/`main` @ `6430cc8` (tag `v1.0.0`).

---

## 1. Valutazione complessiva

**Gestione — livello alto, sopra la media di un progetto personale.**
La tracciabilità difetto→fix (`BACKEND_FIX_TODO.md` con codici, problema, causa, file, data di
chiusura) è di qualità professionale. Il registro decisioni nel tracker è un ADR-log de facto:
data, decisione e soprattutto *perché*, incluse le decisioni negative e quelle superate. La
documentazione è onesta sui fallimenti (i due progetti Railway distinti, il branch sbagliato, il
`.gitignore` con `.vs/` scritto come commento): è la cosa più rara e più professionale del
corpus. La sequenza di rilascio in 8 fasi — stabilizza, deploya, verifica, collega il frontend,
integra, rilascia — è quella corretta, e le fasi si chiudono su evidenze reali, non su opinioni.

**Gestione — dove si rompe.** Il ciclo di analisi non è mai stato chiuso. I requisiti sono fermi
al 2025, il prodotto è del 2026, e non esiste alcuna matrice requisito→implementazione: i casi
d'uso sono numerati ma quei numeri non compaiono mai in tracker, fix o commit. Conseguenza
concreta: circa il 30% di quanto richiesto è uscito dal perimetro **senza una decisione
tracciata**, e il piano di test è fermo al 20/03/2026 su un prodotto cambiato molto da allora.
Manca una Definition of Done: "Completato" a volte significa "codice scritto", a volte "testato
in produzione", a volte "deciso non necessario".

**Implementazione — buona ossatura.** Layering Controller→Service→Repository coerente, DI pulita,
middleware eccezioni centralizzato, RBAC su 3 ruoli reale e granulare, audit trail, cache con
invalidazione su write, Quartz persistente, fail-fast sulla configurazione all'avvio
(`Program.cs`), segreti mai nel repo, CI su push/PR. Il codice è compatto (~4.800 LOC C#,
~2.800 LOC TS) e nessun file è fuori controllo: è leggibile. Sul frontend, l'attenzione alle
conferme sulle azioni distruttive e alla gestione degli errori API è sopra la media di un primo
progetto React.

**Implementazione — dove si rompe.** Tre difetti di logica di dominio, elencati sotto, che oggi
non compaiono in nessun documento (`BACKEND_FIX_TODO.md` dichiara "zero fix aperte"). A questi si
aggiunge una copertura di test squilibrata: 31 test verdi danno una sensazione di sicurezza che i
test non giustificano, perché il flusso di prenotazione — cioè il prodotto — non è coperto da
nessuno di essi. Il frontend è a copertura zero.

**In una frase.** Il progetto dimostra già competenze full stack reali e un metodo di lavoro
serio; quello che oggi lo separa da un portfolio davvero solido non è aggiungere funzionalità, è
chiudere i tre difetti di logica e coprire con test il percorso che il prodotto esiste per fare.

---

## 2. Feedback sulla logica applicativa

### 2.1 Quanto rispecchia l'idea iniziale

Il nucleo dell'idea c'è ed è implementato: prenotazione online self-service, assegnazione
automatica della postazione, fasce orarie con capienza, tre ruoli, dashboard giornaliera e
settimanale, storico con archiviazione automatica. Il restringimento di scope da "piattaforma
multi-settore" a "solo ristorazione" è una decisione esplicita e ben motivata: è la scelta di
gestione migliore dell'intero progetto.

Sono invece **usciti dal perimetro senza mai passare per uno stato "Non necessario" o "Futuro"**:

- `ConfigurazioneSistema` — entità pianificata e poi sparita. Con lei spariscono le **chiusure
  straordinarie e gli orari speciali**, e le **regole di assegnazione configurabili dall'Admin**:
  l'algoritmo è hardcoded.
- **Reportistica ed export PDF/CSV** — la riga "Dashboard + Reportistica, export CSV/PDF" risulta
  *Completato* nel tracker, ma esistono solo i 2 endpoint Dashboard e **nessun endpoint di
  export**. È l'unico punto in cui il tracker afferma qualcosa di non vero.
- **Recupero password self-service** — esiste solo il reset fatto dall'Admin.
- **Unione/separazione tavoli come azione manuale dello Staff** — esiste solo l'unione automatica
  dell'algoritmo.
- **Caratteristiche postazione** oltre la capienza (interno/esterno, accessibilità) — rimane solo
  il legame con la Zona.
- **No-show** — è solo un KPI derivato ("prenotazioni Attive su date passate"), non uno stato:
  non esiste alcuna gestione operativa.
- **Rate limiting e CAPTCHA** — requisiti di sicurezza dell'analisi iniziale, mai evasi né
  rinunciati formalmente.

Non è un problema che siano fuori: è un problema che **nessun documento lo dica**. Chi legge il
tracker crede che ci siano.

### 2.2 È stato implementato correttamente?

Nel complesso sì, con tre eccezioni che rompono la logica di dominio. Sono verificate riga per
riga sul codice.

**A. L'endpoint pubblico di disponibilità dichiara sempre il locale vuoto.**
`DisponibilitaService.cs:35` calcola l'occupato sommando `pp.NumeroPosti`. Ma `NumeroPosti` non
viene **mai** valorizzato: `PrenotazioniService.cs:95-101` (creazione) e `:166-172` (modifica)
popolano solo `PostazioneId`, e il default del modello è `0`. Quindi `allocatedSumForFascia` è
sempre 0 e `TotalePostiDisponibili` è sempre la capienza totale del locale. `check-disponibilita`
è l'unico endpoint pubblico non autenticato del prodotto, cioè la vetrina: oggi risponde sempre
"tutto libero". Anche il correttivo `nonAssegnatePerFascia` (`:37-40`) guarda le prenotazioni
*senza* postazioni, caso che nel flusso attuale non esiste mai: è codice morto.

**B. Admin e Staff non possono modificare la prenotazione di un cliente.**
`PrenotazioniService.cs:144-145` controlla la proprietà della prenotazione **in modo
incondizionato**, non dentro `if (IsSelfServiceCliente())` come invece fa correttamente
`AnnullaPrenotazioneAsync:227-234`. Risultato: uno Staff che modifica la prenotazione presa al
telefono riceve `UnauthorizedAccessException`. Contraddice esplicitamente sia
`GestoraWebApi/CLAUDE.md` ("Admin/Staff senza limiti") sia il RBAC dichiarato. È un difetto
funzionale sul caso d'uso primario dello Staff.

**C. Due clienti possono prenotare lo stesso tavolo nello stesso momento.**
Dopo la migration `20260826133929` non esiste più **nessun** indice unique sulle prenotazioni; in
tutto il progetto non c'è **nessuna** transazione esplicita, nessun lock, nessun concurrency
token; e la PK di `PrenotazioniPostazioni` è `(PostazioneId, PrenotazioneId)`, quindi due
prenotazioni diverse sullo stesso tavolo non violano alcuna chiave. Due `POST crea-prenotazione`
simultanee leggono lo stesso stato, superano entrambe il controllo di capienza, ricevono entrambe
la stessa postazione e vengono entrambe salvate. Anche il vincolo "una prenotazione al giorno per
Cliente" (`:387-398`) è oggi un TOCTOU puro, perché l'indice che lo garantiva è stato rimosso.
La rimozione era corretta come decisione (bloccava Staff/Admin), ma la garanzia che offriva non è
stata sostituita da nulla.

### 2.3 Incoerenze semantiche del dominio

- **`MaxPrenotazioni` ha tre significati diversi nello stesso prodotto**: somma dei **coperti** in
  `PrenotazioniService.cs:427`, **numero di prenotazioni** in `DisponibilitaService.cs:49`,
  esposto come `MaxCoperti` in `Dashboardservice.cs:90`. Finché il campo non ha un solo
  significato, i percorsi "cosa vedo" e "cosa il sistema accetta" continueranno a divergere:
  l'utente può vedere disponibilità e ricevere un errore, o il contrario.
- **Il percorso di verifica e il percorso di prenotazione sono due algoritmi diversi.**
  `DisponibilitaService` ignora `Postazione.Attiva` e `Zona.Attiva`, `PostazioneAssignmentService`
  no. Vanno riportati sulla stessa funzione.
- **L'occupazione del tavolo è all-or-nothing.** Un tavolo da 8 prenotato per 2 persone risulta
  interamente occupato. Il campo `NumeroPosti` sulla tabella ponte esiste esattamente per
  evitarlo, e non viene mai scritto (vedi A).
- **La "zona preferita" è in realtà un filtro rigido.** `PostazioneAssignmentService.cs:26-35`: se
  la zona scelta è piena la prenotazione fallisce, anche con tutto il resto del locale libero. Il
  DTO e la documentazione la chiamano preferenza.
- **Le combinazioni di tavoli non attraversano mai le zone.** Corretto come regola di sala, ma
  significa che due tavoli da 2 in zone diverse non soddisfano mai una richiesta da 4: va detto
  all'utente, non lasciato come rifiuto muto.
- **Timezone misto.** Backend: `Europe/Rome` in `PrenotazioniService.GetNowInRome`,
  `DateTime.UtcNow` in `Dashboardservice.cs:144`, `DateTime.Today` in
  `PrenotazioneCreateDTOValidator.cs:15`. Frontend: `toISOString()` in `DashboardPage.tsx:12`,
  quindi **tra mezzanotte e le 2 la dashboard mostra i dati di ieri**. Serve un solo orologio di
  dominio, ovunque.

### 2.4 Migliorie a cui non hai pensato

In ordine di rapporto valore/sforzo, tutte coerenti con il dominio già modellato:

1. **Turnover del tavolo (durata seduta).** Oggi un tavolo è occupato per l'intera fascia. Con una
   durata media di seduta il locale fa due turni sullo stesso tavolo nella stessa fascia: è la
   singola funzionalità che più avvicina il prodotto a un gestionale reale.
2. **No-show come stato reale**, con contatore per cliente e policy (es. blocco della prenotazione
   online dopo N no-show). Oggi è solo un numero sulla dashboard.
3. **Waitlist su fascia piena.** Quando la capienza è esaurita, invece del rifiuto: lista d'attesa
   e notifica automatica alla prima cancellazione. Riusa tutto il modello esistente.
4. **Email di conferma e reminder** (conferma alla creazione, promemoria il giorno prima). È anche
   il modo più diretto per ridurre i no-show, quindi chiude il punto 2.
5. **Zona come preferenza vera**, con fallback su altra zona e avviso all'utente invece del rifiuto.
6. **Overbooking controllato** configurabile per fascia (es. +10%): pratica standard nella
   ristorazione.
7. **UI sull'audit trail.** La tabella `Logging` esiste e viene popolata, ma è consultabile solo
   dai log Railway: è l'unico item del tuo `AppuntiFix.txt` ed è a costo bassissimo (una pagina
   Admin con filtri su utente/azione/data).
8. **Chiusure straordinarie e orari speciali** — requisito originale; oggi non puoi chiudere il
   locale a Ferragosto se non disattivando le fasce a mano.
9. **Export CSV/PDF** dei report — dichiarato completato, mai fatto, e in colloquio è la cosa che
   viene chiesta più spesso.
10. **Concorrenza ottimistica** (`xmin` come concurrency token su `Prenotazione`) — chiude il
    difetto C in modo idiomatico EF Core e vale come dimostrazione di competenza.
11. **Refresh token** — 60 minuti di sessione secca sono un problema operativo per uno staff che
    lavora durante il servizio.
12. **Multitenancy** (più locali sullo stesso deploy) — direzione naturale del prodotto e unica
    voce di roadmap che cambia davvero la scala del progetto.

---

## 3. Segnalazioni

### Priorità ALTA

**Logica applicativa**
- `check-disponibilita` restituisce sempre disponibilità piena: `NumeroPosti` mai valorizzato — `DisponibilitaService.cs:35` vs `PrenotazioniService.cs:95-101`, `:166-172`
- Admin/Staff non possono modificare prenotazioni altrui: controllo ownership incondizionato — `PrenotazioniService.cs:144-145`
- Race condition sulla doppia prenotazione: nessun indice unique dopo `20260826133929`, nessuna transazione, nessun lock — `PrenotazioniService.cs:73-105`, `GestoraContext.cs:139`
- Vincolo "una prenotazione al giorno" ridotto a TOCTOU dopo la rimozione dell'indice — `PrenotazioniService.cs:387-398`
- `MaxPrenotazioni` con tre semantiche diverse — `PrenotazioniService.cs:427`, `DisponibilitaService.cs:49`, `Dashboardservice.cs:90`
- `UpdateAsync` prenotazione senza audit log: unica mutazione che non logga — `PrenotazioniService.cs:131-175`

**Backend**
- `POST seed-admin` pubblico in produzione: su DB vuoto il primo che arriva diventa Admin — `AuthenticationUserController.cs:93`
- Nessun lockout Identity e nessun rate limiting su `/login`: brute force senza freni — `AuthenticationExtensions.cs:18-20`, `Program.cs`
- `exception.Message` restituito al client anche sulle 500: leak di messaggi Npgsql — `ExceptionMiddleware.cs:67`
- Secondo costruttore pubblico con 8 campi `object1..object7` mai assegnati: se il DI lo seleziona il service parte con tutte le dipendenze null — `PrenotazioniService.cs:31-38`, `:61-71`
- Deploy non versionato: nessun Dockerfile, nessun `railway.json`, configurazione solo nella UI Railway
- Nessun `Database.Migrate()` allo startup e `/health` senza check sul DB: un deploy con schema disallineato risulta "Healthy" — `Program.cs:142`, `:213`
- Password policy aggirabile: `AdminResetPasswordDTO` senza validator accetta 6 caratteri alfanumerici — `AuthenticationUserController.cs:268-276`

**Frontend**
- Schermata bianca irreversibile con token malformato in localStorage: `JSON.parse(atob(...))` senza try/catch nell'initializer — `AuthContext.tsx:13`
- Select "Zona" non collegato al form (`setValue` senza `register`/`value`): dopo `reset()` il DOM mostra la vecchia zona mentre il form invia `null` — `PrenotazioneModal.tsx:108-116`
- Dashboard con la data sbagliata tra mezzanotte e le 2 (`toISOString()` in UTC) — `DashboardPage.tsx:7-8`, `:12`
- Nessun fail-fast su `VITE_API_URL` mancante: `baseURL: undefined` manda tutte le chiamate sull'origin Vercel — `lib/axios.ts:4`

### Priorità MEDIA

**Backend**
- Cache `FascePerGiorno` non invalidata su delete: fascia cancellata servita per 30 minuti — `FasciaOrariaService.cs:135`
- `Page` non validato in `PrenotazioniQueryParams`: `?page=0` produce `Skip(-20)` — `PrenotazioniQueryParams.cs:9`
- Ordinamento paginazione non deterministico (solo `DataPrenotazione`): righe duplicate o mancanti tra pagine — `PrenotazioniService.cs:278`
- N+1 su `GetUsers` (`GetRolesAsync` per utente, senza paginazione) — `AuthenticationUserController.cs:175-188`
- N+1 nei due job Quartz: una query e una `SaveChanges` per riga — `PrenotazioniService.cs:325-327`, `:345`
- `Include(PrenotazioniPostazioni)` senza filtro nel percorso caldo dell'assegnazione, dato mai usato — `PostazioneRepository.cs:33`
- `DisponibilitaService` ignora `Postazione.Attiva` e `Zona.Attiva` — `DisponibilitaService.cs:25`
- `UnauthorizedAccessException` mappata su 401 invece di 403: fa scattare il logout automatico del frontend — `ExceptionMiddleware.cs:43`
- `InvalidOperationException → 409` troppo ampia: cattura anche errori interni di EF Core — `ExceptionMiddleware.cs:47`
- `CheckDisponibilitaDTO` senza validator sull'unico endpoint pubblico
- Quartz persistente ma non in cluster mode: con più repliche ogni job gira due volte — `Program.cs:100-110`
- Manca `UseForwardedHeaders`: l'audit trail registra l'IP del proxy, non del client — `PrenotazioniService.cs:400`
- 5 `NotImplementedException` per soddisfare `IRepository<T>`/`IService<T>` — `ZonaRepository.cs:90`, `FasciaOrariaService.cs:259`, `PostazioneService.cs:94`, `:99`
- 404 su collezione vuota ancora presente (FIX-007 dichiarato chiuso) — `PrenotazioniService.cs:253-254`, `PrenotazioneController.cs:162-163`
- Audit log fuori transazione: due `SaveChanges` separati, se il log fallisce la scrittura resta — `PrenotazioniService.cs:103-104`
- `NomeCliente` scrivibile anche dal Cliente, mentre è documentato come campo Staff/Admin — `PrenotazioniService.cs:90`
- Un Cliente non può leggere il dettaglio della propria prenotazione (`get-prenotazione` è Admin/Staff) — `PrenotazioneController.cs:43`, `:75`
- Migration `20260311090312_StatoAsEnum` con `Up()`/`Down()` vuoti, committata in produzione
- `Microsoft.EntityFrameworkCore.Tools 10.0.0` con stack EF 9.0.9; `Serilog.Sinks.Seq` referenziato e mai configurato — `GestoraWebApi.csproj:31`, `:43`
- `Logging` senza indici né `MaxLength`: tabella di audit interrogabile solo in full scan — `GestoraContext.cs:13`
- `DeleteBehavior.Cascade` su utente→prenotazioni: eliminare un utente cancella lo storico e falsa le statistiche — `GestoraContext.cs:116-119`
- `PostazioniOccupate` in dashboard non distingue le fasce: un tavolo usato a pranzo risulta occupato tutto il giorno — `Dashboardservice.cs:55-61`, `:108`

**Frontend**
- `queryClient.clear()` mancante al logout: i dati del primo utente restano in cache dopo il cambio account — `AuthContext.tsx:27-30`
- Blocco `onError` duplicato 21 volte in 5 hook (~150 LOC eliminabili) — `useZone.ts`, `usePostazioni.ts`, `useFasceOrarie.ts`, `usePrenotazioni.ts`, `useAdminUtenti.ts`
- Toast doppi su Postazioni: notifica sia nell'hook sia nel modal — `usePostazioni.ts:22`, `:29` vs `PostazioneModal.tsx:57-82`
- `pageSize: 100` hardcoded senza paginazione UI: oltre 100 prenotazioni i dati spariscono in silenzio — `PrenotazionePage.tsx:34`
- Bottoni di azione non disabilitati durante la mutation: doppio click uguale doppia chiamata — `PrenotazionePage.tsx:116-129`
- `ConfirmDialog` non chiudibile con ESC/overlay (`open` senza `onOpenChange`) e con titolo "Conferma eliminazione" hardcoded, riusato per annullare una prenotazione — `ConfirmDialog.tsx:21`, `:24`, `:33`
- `tsconfig.app.json` senza `"strict": true`
- Zero test frontend, nessun runner configurato
- `EditUserModal` e `ResetPasswordModal` senza resolver zod, a differenza di `CreateUserModal` — `EditUserModal.tsx:18`, `ResetPasswordModal.tsx:17`
- `QueryClient` senza `defaultOptions`: nessun `staleTime`, retry anche sui 403 — `main.tsx:11`
- Decodifica JWT duplicata in 3 punti, `atob` senza gestione base64url/UTF-8, nessun controllo di `exp` — `AuthContext.tsx:13`, `:21`, `LoginPage.tsx:32`

**Testing**
- Zero test su `AddAsync`/`UpdateAsync`: l'intero flusso di prenotazione non è coperto
- I 4 test di assegnazione coprono `TrovaCombinazioniDisponibili`, non `AssegnaPostazioneDisponibileAsync` che decide le assegnazioni reali — `PostazioneAssignmentServiceTests.cs`
- Zero test su `DisponibilitaService`, `DashboardService`, controller, auth/JWT, RBAC, validator, middleware, job Quartz
- Nessun test di integrazione (`WebApplicationFactory`), nessun test di concorrenza, nessuna soglia di coverage in CI

### Priorità BASSA

**Backend**
- Cartelle morte `Services/FasciaOraria/` e `Repositories/FasciaOraria/` (vuote)
- Quattro forme di naming per lo stesso concetto: `FasciaOrarie` (namespace), `FasciaOrariaService` (classe), `FasceOrarieController` (route), `FasceOrarie` (tabella)
- `Services/PrenotazioniPostazioni/` contiene solo DTO, nessun service: nome ingannevole
- Algoritmo greedy duplicato verbatim — `PostazioneAssignmentService.cs:63-80` e `:108-125`
- `GetAuthenticatedUserId()`/`GetIpAddress()` copiati in 4 service invece che centralizzati
- Proiezione `FasciaOraria → DTO` ripetuta 4 volte identica — `FasciaOrariaService.cs:153-275`
- Costanti duplicate: `{2,4,8}` in 3 punti, cache 30 min in 3 punti, retention 6 mesi hardcoded
- `_mapper` iniettato e mai usato — `FasciaOrariaService.cs:19`; `_itCulture` mai usato — `Dashboardservice.cs:14`
- `FasciaOrariaMappingProfile` ignora `OrarioInizio`/`OrarioFine`: profilo di fatto inutilizzabile
- `using` auto-importati e mai ripuliti — `PrenotazioniService.cs:16`, `FasciaOrariaRepository.cs:6`, `PostazioneService.cs:13`
- `GuardCutoffAsync` è sincrono col suffisso `Async`; `GetAllQueryableAsync` non è async
- `virtual` sulle navigazioni senza lazy loading abilitato: non fa nulla
- File di test con nome troncato `FasciaOrariaServiceTe.cs`
- Email di contatto con typo nel dominio in Swagger — `SwaggerSetup.cs:27`
- Messaggio 401 generico parla sempre di prenotazioni anche su Dashboard/Zone/Utenti — `AuthenticationExtensions.cs:63`
- PII: email loggata a ogni tentativo di login, anche fallito — `AuthenticationUserController.cs:65-72`

**Frontend**
- Responsive assente: zero breakpoint nelle pagine, sidebar `w-64` fissa senza menu mobile, 7 tabelle senza `overflow-x-auto`. Su smartphone l'app non è usabile
- Accessibilità: nessun `htmlFor`/`id` nei form, checkbox con `<span>` al posto di `<label>`, `PrenotazioneModal` è un overlay artigianale senza focus trap né ESC mentre tutti gli altri usano Radix
- Empty state assenti su Zone, Postazioni, Fasce, Prenotazioni, Utenti: tabella con la sola intestazione. In `/postazioni`, prima di scegliere la zona, sembra un errore
- Loading a pagina intera in 6 pagine: a ogni refetch la UI sparisce e riappare
- Stile incoerente: metà app usa `<Button>` shadcn, metà `<button className="bg-blue-500">`, colore fuori dalla palette del tema
- React Query Devtools importati staticamente: finiscono nel bundle di produzione — `main.tsx:5`, `:18`
- Codice morto: `App.tsx`, `App.css`, `src/assets/*`, `useUpdateStatoZona`, `useDeletePrenotazione`, `useFasceOrarie`; costante `GIORNI` duplicata
- Prettier configurato ma non applicato (indentazione a 4 spazi contro `tabWidth: 2` in 3 pagine); nessun hook pre-commit
- `index.html` con `lang="en"` su app in italiano e title `gestora-frontend`; nessuna favicon né meta description
- `/unauthorized` è un vicolo cieco: nessun link di ritorno
- Header senza username/ruolo, sidebar senza stato attivo: non si capisce in che pagina si è
- URL degli endpoint scritte inline negli hook: nessun punto unico per i path

**Gestione / processo**
- `BACKEND_FIX_TODO.md` dichiara zero fix aperte mentre FIX-007 è ancora presente in 2 punti
- Il tracker dà "Completato" a "Reportistica + export CSV/PDF" che non esiste
- `Guida_Test_Swagger.txt` fermo al 20/03/2026: non copre cutoff 2h, `NomeCliente`, `get-mie-prenotazioni`, dashboard, pannello utenti. È l'unico surrogato di criteri di accettazione ed è disallineato
- Nessuna matrice requisito→implementazione: i casi d'uso numerati non sono mai richiamati in tracker, fix o commit
- Nessuna Definition of Done: gli stati sono normati per colore, non per semantica
- Conteggio test incoerente tra i documenti: 18 / 28 / 31
- "Clean Architecture + DDD" dichiarato in due documenti e in `PIANO_RILASCIO.md`, ma la struttura reale è layered in un unico progetto
- Contraddizione mai risolta: "creazione dinamica delle postazioni con i coperti richiesti" contro capienza vincolata a {2,4,8}
- Modello dati senza diagramma ER (la sezione "Diagramma UML" è un titolo vuoto); `NomeCliente` mai aggiunto al foglio *Modelli*; nessun registro delle migration
- Concorrenza, fusi orari, prenotazioni a cavallo di mezzanotte, modifica di fasce/postazioni con prenotazioni future: mai trattati a livello di analisi
- Stato del progetto distribuito su 5 fonti con protocollo di aggiornamento su 6 fogli: è qui che si generano le incoerenze
- Percorso errato `02_Personali\Gestora` in `CLAUDE.md` e nelle istruzioni globali (reale: `Personali\Gestora`)
- `CLAUDE.md` §Stack dice ancora "FRONTEND (completato — deploy pendente)" mentre il deploy Vercel è fatto
- Grafo `graphify-out/` fermo al 14/08: non riflette le Fasi 5-7
- `JobsController.cs` non committato, presente solo nel working tree
