---

 name: verifica-gestora

 description: Usa prima di dichiarare concluso un task di codice sul progetto Gestora (backend o frontend) — richiede

 evidenza (build/test) prima di affermare che qualcosa funziona o è corretto.

 ---



 # Verifica prima di dichiarare completato — Gestora



 Prima di dire "fatto", "funziona", "risolto" o equivalenti su una modifica di codice in questo

 progetto, esegui la verifica corrispondente all'area toccata. Non affermare il successo sulla

 base della sola lettura del codice: serve l'output di un comando.



 ## Se hai toccato `GestoraWebApi/`



 1. `dotnet build` nella cartella `GestoraWebApi/` — deve completare senza errori (i warning

    pre-esistenti non sono un blocco, ma non introdurne di nuovi senza dirlo)

 2. `dotnet test` nella stessa cartella — confronta il conteggio "Passed" con quello atteso

    (l'ultimo noto è riportato nel blocco stato-sessione del CLAUDE.md di root). Se il numero è

    diverso da quello atteso e non hai aggiunto/rimosso test intenzionalmente, fermati e

    segnalalo — non ignorarlo

 3. Se hai toccato un endpoint: verifica anche manualmente su Swagger (`/swagger`) prima di

    considerarlo concluso, non solo via test unitari



 ## Se hai toccato `gestora-frontend/`



 1. `npm run lint` — zero errori (i warning esistenti vanno segnalati solo se aumentano)

 2. `npm run build` — deve completare senza errori TypeScript

 3. Se hai toccato un componente/pagina visibile: descrivi esplicitamente che la verifica

    manuale in browser NON è stata fatta, se non è stata fatta — non dare per scontato che

    "compila" equivalga a "funziona nell'interfaccia"



 ## Regola di fondo



 Se un comando di verifica fallisce più di 3 volte di fila sullo stesso problema, fermati e

 segui la regola già presente nel CLAUDE.md globale di Carlo: niente altri tentativi, si analizza

 la causa alla radice prima di ritentare.



 Non dichiarare mai "tutti i test passano" o "la build è pulita" senza aver effettivamente

 eseguito il comando in questa sessione — un'affermazione di questo tipo richiede sempre

 l'evidenza del comando corrispondente eseguito poco prima.

