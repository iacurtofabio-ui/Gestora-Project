---

 description: Mappa un endpoint del backend a controller, service, repository e test collegati

 argument-hint: <nome-endpoint-o-area, es. "crea-prenotazione" oppure "FasceOrarie">

 ---



 Dato l'endpoint o l'area indicata — $ARGUMENTS — usa graphify (`graphify query`, `graphify

 path`, `graphify explain`) per identificare, senza rileggere l'intero progetto:



 1. Controller e action coinvolti (file + riga)

 2. Service e metodo chiamato dal controller

 3. Repository/e coinvolti

 4. DTO di richiesta/risposta usati

 5. Test esistenti che coprono questo service (se esistono, indica il file in

    `GestoraWebApi.Tests/`; se non esistono, dillo esplicitamente — non presumere che ci siano)



 Rispondi in forma di elenco puntato breve, un punto per livello dell'architettura

 (Controller → Service → Repository), non in prosa.

