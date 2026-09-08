# Gestora — procedure operative

Come si fanno le operazioni del progetto, con i comandi veri e **cosa va storto se si sbaglia**.
Ogni trappola scritta qui è già costata tempo almeno una volta.

Tutti i comandi sono per **PowerShell su Windows**, su una riga sola.

---

## Indice

1. [Avviare il progetto in locale](#1-avviare-il-progetto-in-locale)
2. [Verificare che sia tutto a posto](#2-verificare-che-sia-tutto-a-posto)
3. [Reset dei database](#3-reset-dei-database)
4. [Applicare una modifica al database in produzione](#4-applicare-una-modifica-al-database-in-produzione)
5. [Pubblicare un rilascio](#5-pubblicare-un-rilascio)
6. [Accedere al database di produzione e fare un backup](#6-accedere-al-database-di-produzione-e-fare-un-backup)
7. [User Secrets: le credenziali locali](#7-user-secrets-le-credenziali-locali)
8. [Trappole da conoscere](#8-trappole-da-conoscere)

---

## 1. Avviare il progetto in locale

Servono due terminali, più PostgreSQL già attivo sulla macchina.

**Backend** (porta 5099, fissata in `Properties/launchSettings.json`):
```powershell
cd "C:\Users\Carlo Taranto\Progetti_Tech\Personali\Gestora\GestoraWebApi"; dotnet run
```

**Frontend** (porta 5173):
```powershell
cd "C:\Users\Carlo Taranto\Progetti_Tech\Personali\Gestora\gestora-frontend"; npm run dev
```

Il frontend locale legge `.env.local`, che deve contenere `VITE_API_URL=http://localhost:5099/api`.
Quel file **non è versionato**: su un computer nuovo va ricreato.

> Aprendo `localhost:5173` si vedono sempre i dati del **database locale**, mai quelli di
> produzione. È voluto.

---

## 2. Verificare che sia tutto a posto

Da fare prima di chiudere una fase. Tutti e quattro devono essere puliti.

```powershell
cd "...\GestoraWebApi"; dotnet test
cd "...\gestora-frontend"; npm test
cd "...\gestora-frontend"; npm run build
cd "...\gestora-frontend"; npm run lint
```

Valori attesi oggi: **239** test backend, **26** frontend, build senza errori, lint senza errori.

> ⚠️ `npm test` **non controlla i tipi**, `npm run build` sì. Un test verde non sostituisce una
> build pulita: in Fase 8 la build ha trovato un campo scritto male che i test non vedevano.

> ⚠️ Se un test contraddice quello che vedi succedere davvero, **sospetta la build prima del
> codice**: un file ripristinato da un backup ha un timestamp vecchio e il compilatore non lo
> ricompila. Si risolve con `dotnet build -t:Rebuild`.

Esiste anche la scorciatoia `/verifica-gestora`, che lancia questi controlli e riporta l'esito.

---

## 3. Reset dei database

Cancellare tutto e ripartire da zero. **Da fare solo a implementazioni concluse** (voce `OPS-001`
in `BACKLOG.md`).

### ⚠️ Le tre cose che si dimenticano

1. **Le tabelle `QRTZ_*` non stanno nelle migration di Entity Framework.** Dopo aver ricreato il
   database va rieseguito a mano `Scripts/quartz_postgres.sql`, **altrimenti l'applicazione non
   parte**.
2. **Sparisce anche il registro delle attività** (tabella `Logging`), non solo i dati di prova.
   È una conseguenza voluta della scelta, non un effetto collaterale da scoprire dopo.
3. **Il primo amministratore si ricrea da solo**: `POST /api/Setup/admin` si riapre
   automaticamente quando non esiste nessun Admin. Nessuna variabile da toccare su Railway o
   Vercel.

### Locale

**Il backend va spento prima**, altrimenti la connessione aperta blocca la cancellazione.

```powershell
cd "...\GestoraWebApi"; dotnet ef database drop
```
Senza `--force`: così stampa quale database sta per cancellare e chiede conferma. È la rete di
sicurezza, non toglierla.

```powershell
cd "...\GestoraWebApi"; dotnet ef database update
psql -U postgres -d gestora_db -f Scripts\quartz_postgres.sql
```

Poi si apre l'applicazione, che porterà alla schermata di primo avvio per ricreare l'Admin.

### Produzione

Stessa sequenza, ma il database è su Railway e **la esegue Fabio**. Si entra con
`railway connect Postgres` (vedi punto 6) e si esegue lo script delle tabelle Quartz da lì.

Prima di iniziare: **backup**, anche se si sta cancellando apposta. Se qualcosa va storto a metà
si resta con un database mezzo vuoto e nessun modo di tornare indietro.

---

## 4. Applicare una modifica al database in produzione

Le modifiche allo schema (migration) **si applicano a mano**: è la decisione di prodotto 2.
Claude prepara la migration, Fabio la applica.

**Perché serve una sequenza stretta.** Railway aggiorna l'applicazione gradualmente: fra
"modifica applicata" e "nuova versione online" c'è un intervallo in cui la versione vecchia
interroga uno schema che non corrisponde più, e risponde errore 500 su tutto ciò che tocca quella
tabella.

Per il traffico di Gestora la soluzione giusta è fare i passaggi **in fila stretta**, non a
distanza di ore:

1. **Backup** — non negoziabile, anche per una modifica banale
2. Applicare la modifica
3. Pubblicare la nuova versione dell'applicazione
4. Verificare: `GET /health` deve rispondere `Healthy`, più una chiamata vera sulla parte toccata

**Lo script si genera così:**
```powershell
cd "...\GestoraWebApi"; dotnet ef migrations script --idempotent -o Scripts\nome_script.sql
```

> ⚠️ **Togliere il BOM dallo script prima di usarlo.** Il file generato ha un carattere invisibile
> in testa che psql attacca alla prima istruzione: `START TRANSACTION` fallisce e **tutto lo
> script gira senza transazione**, quindi senza possibilità di annullare se qualcosa va male.
> È già successo in produzione il 02/09/2026.

> ⚠️ **Se una modifica si applica a mano, usare sempre lo script generato**, non i comandi SQL
> scritti a mano: lo script include anche la riga che registra la modifica come applicata. Senza
> quella riga lo schema è giusto ma Entity Framework continua a considerarla da fare, e un futuro
> aggiornamento proverà a rieseguirla. Successo il 31/08, sistemato il 04/09.

---

## 5. Pubblicare un rilascio

**Il commit, il push, il merge e il tag li fa Fabio.** Claude fornisce solo il messaggio.

1. Commit su `dev`
2. Merge `dev` → `main`
3. Tag sulla punta di `main`, poi push del tag
4. Verifica in produzione

**Cosa si ripubblica da solo:**
- push su `main` → **Vercel** ricostruisce il frontend
- push su `main` → **Railway** ricostruisce il backend
- il **tag non fa niente**: è solo un segnalibro sulla storia

Quindi: se la modifica è solo di backend, Vercel ricostruisce ma non cambia niente, e viceversa.

**Numerazione:** si alza l'ultimo numero (`v1.0.5` → `v1.0.6`) per fix e correzioni. Una fase di
solo riordino interno può restare **senza tag**, come la Fase 9.

**Verifica dopo il deploy:**
```powershell
curl https://gestora-project-production.up.railway.app/health
```
Deve rispondere `Healthy` — copre anche la raggiungibilità del database, non solo il processo.

Poi login dal frontend con i tre ruoli.

> ⚠️ **Prima di confermare un commit, contare i file inclusi.** `git status` breve raggruppa le
> cartelle nuove e ne mostra meno di quanti ce ne sono davvero; Visual Studio fa lo stesso, e i
> file nuovi vanno spuntati a mano. Per l'elenco reale:
> ```powershell
> git status --untracked-files=all
> ```
> In Fase 7 un commit ha lasciato fuori 12 file nuovi, che sono stati persi.

---

## 6. Accedere al database di produzione e fare un backup

```powershell
railway connect Postgres
```

> Il servizio Postgres di Railway **non ha un indirizzo pubblico**, quindi `pg_dump` da casa non
> funziona: `postgres.railway.internal` non è raggiungibile da fuori. Il backup si fa **da dentro
> quella sessione**, tabella per tabella:
> ```
> \copy "FasceOrarie" TO 'backup_FasceOrarie.csv' CSV HEADER
> ```

> ⚠️ I file di backup **non vanno mai committati**. Il `.gitignore` di radice li esclude già
> (`backup_*.csv`, `backup_*.dump`, `*.dump`), ma controllare prima del commit. Il 02/09/2026 tre
> file di backup sono finiti in un repository pubblico e la storia di Git ha dovuto essere
> riscritta.

### Rotazione della password del database

Si cambia dal **tab Config del servizio Postgres** su Railway ("regenerate"), che aggiorna insieme
il database e le variabili. Cambiarla con `ALTER USER` a mano aggiorna solo il database e lascia
le variabili indietro, senza modo di riallinearle dal pannello.

> ⚠️ **L'autocomplete di Railway mangia il carattere che precede il riferimento.** Scegliendo un
> valore dalla lista, gli `=` di `Port=`, `Database=`, `Username=`, `Password=` spariscono, e la
> stringa di connessione diventa `Port5432;Databaserailway;...`. Il database risponde
> `password authentication failed`, che porta completamente fuori strada perché la password è
> giusta. **Dopo ogni modifica contare i cinque `=`**:
> ```powershell
> railway variables --service "Gestora-Project"
> ```

I riferimenti tipo `${{Postgres.PGHOST}}` vanno **digitati** nel campo, non incollati da fuori:
incollati restano stringhe morte.

---

## 7. User Secrets: le credenziali locali

Le credenziali di sviluppo vivono **fuori dal repository**, in un file sulla macchina:

```
C:\Users\Carlo Taranto\AppData\Roaming\Microsoft\UserSecrets\aa9f6e84-1217-48e1-9783-b07f152f7874\secrets.json
```

Comandi, da eseguire dentro `GestoraWebApi\`:
```powershell
dotnet user-secrets list
dotnet user-secrets set "JwtSettings:Secret" "nuovo-valore"
dotnet user-secrets remove "JwtSettings:Secret"
```

Su un computer nuovo vanno ricreati da zero (`dotnet user-secrets init` e poi `set`).

> ⚠️ **Per digitare una password usare sempre:**
> ```powershell
> $sec = Read-Host "Password" -AsSecureString
> ```
> `Read-Host` da solo **mostra la password a schermo**. Il 07/09/2026 una password di produzione
> è finita così in chat ed è stata sostituita.

---

## 8. Trappole da conoscere

### psql chiamato da PowerShell mangia le virgolette

PowerShell 5.1 toglie i doppi apici dagli argomenti passati a un programma esterno. Quindi
`-c 'SELECT * FROM "Utenti";'` arriva a psql **senza** virgolette, e PostgreSQL cerca la tabella
`utenti` in minuscolo: "relazione non esiste".

Vanno protette con il backslash, dentro gli apici singoli:
```powershell
psql -U postgres -d gestora_db -c 'SELECT * FROM \"Utenti\";'
```

> **Nomi delle tabelle degli utenti**: in questo progetto si chiamano `Utenti` e `Ruoli`,
> **non** `AspNetUsers` / `AspNetRoles`.

### Comandi PowerShell lunghi

Vanno dati **su una riga sola**: i blocchi su più righe si concatenano male nel terminale.
Se un comando è davvero lungo, si mette in un file `.ps1` salvato in **UTF-8 con BOM**, altrimenti
PowerShell 5.1 lo legge con la codifica sbagliata e gli accenti si rompono.

### Excel COM da PowerShell (per il tracker)

- I fogli si indirizzano per **numero**, non per nome: i nomi contengono emoji e falliscono
- I valori si scrivono sempre convertiti a testo: `[string]"..."`
- Le date vanno scritte come date vere (`[datetime]::new(2026,9,8)`), non come stringhe:
  scritte come testo vengono lette all'americana e il giorno diventa il mese
- Per copiare la formattazione di una riga: `Rows.Item(x).Copy(Rows.Item(y))`, mai `PasteSpecial`
- Il colore di una cella si legge come numero, non come testo: per riusare un colore esistente
  si legge da una cella che ce l'ha già, invece di scriverlo a mano

### Railway: un progetto = un'applicazione

Database e applicazione devono stare **nello stesso progetto Railway**, altrimenti i riferimenti
fra variabili (`${{Postgres.PGHOST}}`) non funzionano e il servizio non compare nemmeno
nell'autocomplete. Non è un problema di sintassi o di permessi: è che sono in due posti diversi.

### Gli indirizzi degli endpoint

La rotta base è `/api/[nome del controller senza "Controller"]`.
Quindi `AuthenticationUserController` risponde su `/api/AuthenticationUser/...` — **non**
`/api/AuthenticationUserController/...` e **non** `/api/Auth/...`, come dice documentazione
vecchia.

Eccezione da ricordare: le fasce orarie rispondono su `/api/FasceOrarie/...` anche se la classe
si chiama `FasciaOrariaService`. È il naming incoerente lasciato apposta (vedi `BACKLOG.md`,
sezione *Deciso di NON fare*).
