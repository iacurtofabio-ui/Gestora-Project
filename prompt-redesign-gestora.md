## Ruolo

Agisci come design lead di uno studio noto per dare a ogni cliente un'identità visiva
riconoscibile e non confondibile con quella di nessun altro. Il cliente ha già scartato
una proposta perché sembrava templated e generica, e ora paga per un punto di vista
estetico preciso. Fai scelte deliberate e opinionate su palette, tipografia, layout e
motion, specifiche per QUESTO prodotto.

## Contesto del prodotto

Gestora non è un SaaS generico: è uno strumento che si usa in sala, spesso di fretta, spesso
durante il servizio. La materia prima visiva sta lì — tempo, coperti, sale, fasce orarie,
turni, sold out. Da questo vocabolario devono nascere le scelte distintive, non da un
kit di card arrotondate applicabile a qualsiasi prodotto.

## Problema da risolvere

Funzionalità e struttura vanno bene e NON vanno toccate. L'aspetto visivo no:

1. Componenti troppo standard, sembrano gli esempi della documentazione di shadcn
   lasciati com'erano.
2. Palette cupa e senza carattere.
3. Il tema scuro tende al marrone invece che a un nero neutro. Causa probabile: la scala
   neutra calda (stone/warm) dei CSS variables di shadcn in `index.css` o `globals.css`.
   Verifica e correggi alla radice, sui token.
4. Gestione visiva degli errori datata (alert nativi, dialog grezzi, messaggi vaghi).
5. Manca gerarchia: tutto ha lo stesso peso, lo stesso border-radius, la stessa ombra.

## Vincoli non negoziabili

- Nessuna modifica a logica di business, chiamate API, routing, schemi Zod, gestione
  dello stato o contratti dei dati. Solo presentazione.
- Resta su shadcn/ui + Tailwind. Non introdurre altre librerie UI. Motion: puoi usare
  quello che il progetto ha già; se serve una libreria di animazione, chiedi prima.
- Tutti i colori passano dai design token nei CSS variables. Zero hex hardcodati nei
  componenti.
- Quality floor, senza annunciarlo: responsive fino a mobile, focus da tastiera visibile,
  `prefers-reduced-motion` rispettato, contrasto AA sul testo.

## FASE 1 — Piano di design (nessun codice)

Prima leggi il codice reale: `index.css`/`globals.css`, la config Tailwind, i componenti
condivisi e almeno 3 schermate rappresentative. Riporta cosa hai trovato.

Poi proponi **due direzioni alternative e nettamente diverse tra loro**. Per ciascuna:

- **Color**: 4–6 valori esadecimali nominati, scala light e dark. Per il dark, dichiara
  esplicitamente il nero di base e la strategia di elevazione (superfici stratificate, non
  un unico grigio piatto).
- **Type**: uno o due caratteri, con ruoli precisi. Se due, devono essere chiaramente
  distinti. Scala tipografica dichiarata con pesi, spaziature e line-height. Niente
  famiglie di default scelte per inerzia.
- **Layout**: concetto in una frase più wireframe ASCII della schermata principale.
  Indica l'allineamento.
- **Motion**: un solo momento orchestrato memorabile, più le transizioni che rispondono
  a un'azione dell'utente (apertura, conferma, aggiornamento di una prenotazione).
- **Principio guida**: cosa rende questa direzione specifica di Gestora e non riusabile
  su un altro prodotto.

Dai un nome a ogni direzione e spiega in due righe quale utente e quale momento d'uso
serve meglio.

### Cliché da evitare

Prima di consegnare il piano, verifica che non stia scivolando in uno di questi default.
Se ci somiglia, cambialo e dichiara cosa hai cambiato e perché:

- Sfondo crema caldo (~#F4F1EA) con serif ad alto contrasto e accento terracotta (~#D97757).
- Nero quasi puro con un unico accento verde acido o vermiglio.
- Layout da quotidiano: filetti sottili, border-radius zero, colonne dense.
- Kit SaaS-card: contenuto spezzato in card identiche, stesso raggio ovunque a prescindere
  dalla gerarchia, stessa ombra grigia sotto tutto, gradienti come decorazione.
- Chrome da template: eyebrow ALL-CAPS spaziato sopra ogni titolo, stringhe unite da
  punti mediani (`A · B · C`), etichette tipo `PAROLA — frammento`, near-black tinto
  (#0B0B0B, #111) al posto del nero, monospace per le label piccole, `→` in coda ai
  pulsanti.
- Una sola parola del titolo evidenziata in colore o corsivo.
- Entrata fade-and-slide-up su ogni sezione e hover transition su ogni card.
- Marker numerati 01 / 02 / 03 dove il contenuto non è realmente una sequenza.

**Fermati qui e aspetta che io scelga una direzione. Non scrivere codice in questa fase.**

## FASE 2 — Pilota su due schermate

Dopo la mia scelta, implementa solo:

1. Il sistema di token completo (colori light/dark, tipografia, spaziature, raggi,
   elevazioni) nei CSS variables e nella config Tailwind.
2. Due schermate rappresentative: quella con più densità di dati e una form/dettaglio.

Poi mostrami il risultato e fermati per una revisione. Se hai a disposizione uno strumento
per aprire il browser, fai uno screenshot e critica il tuo stesso lavoro prima di
consegnarmelo.

## FASE 3 — Rollout

Solo dopo la mia approvazione, estendi il sistema al resto dell'app, includendo:

- **Stati di errore**: sostituisci alert nativi e dialog grezzi. L'errore spiega cosa è
  successo e come rimediare, con la voce dell'interfaccia. Non si scusa e non è mai vago.
  Errori di validazione inline sul campo, errori di sistema in toast o inline, dialog
  riservato alle sole azioni distruttive che richiedono conferma.
- **Stati vuoti**: un invito ad agire, non un'illustrazione con una frase generica.
- **Stati di caricamento**: skeleton coerenti con il layout finale, non spinner centrati.
- **Microcopy**: voce attiva, sentence case, il pulsante dice cosa succede ("Salva
  modifiche", non "Invia") e la conferma usa lo stesso verbo. Niente riempitivi.

## Criteri di accettazione

- Il tema scuro è neutro e privo di dominante calda, con gerarchia leggibile tra le
  superfici.
- Esiste una gerarchia visiva chiara: non tutti gli elementi hanno lo stesso peso.
- La schermata principale ha un elemento memorabile; tutto il resto attorno è disciplinato
  e silenzioso. L'audacia si spende in un punto solo.
- Nessuna regressione funzionale: build pulita, zero errori TypeScript, tutte le
  funzionalità esistenti operative.
- Nessun colore hardcodato fuori dai token.

## Cosa non fare

- Non riscrivere componenti funzionanti solo per rimaneggiarli.
- Non aggiungere dipendenze senza chiedere.
- Non toccare il backend.
- Non saltare le fasi né anticipare codice durante la Fase 1.
