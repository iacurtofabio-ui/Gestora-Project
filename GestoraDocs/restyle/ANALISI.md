# Analisi del riferimento — Docker Hub

Misurato con Playwright su `hub.docker.com` (home), `hub.docker.com/_/postgres` (pagina
interna) ed `hub.docker.com/search?type=image` (Explore), a 1440px e 375px. Docker Hub non ha un
tema scuro pubblico raggiungibile senza account, quindi la parte scura del nostro tema resta
un'estensione coerente dei token, non una misura diretta.

## Valori misurati

| Elemento | Valore |
|---|---|
| Sfondo pagina | `#FFFFFF` (bianco puro, non grigio-chiarissimo come ipotizzato) |
| Barra superiore | sfondo blu pieno `rgb(29,99,237)` = `#1D63ED`, testo bianco |
| Bordo di una card | `0.8px solid rgba(0,0,0,0.08)` — sottilissimo, quasi invisibile da solo |
| Raggio di una card | `4px` |
| Raggio di un pulsante | `4px` |
| Ombra su card/pulsante | `none` — **nessuna ombra**, la separazione è tutta nel bordo |
| Tab attivo | testo blu (`#1D63ED`), nessuno sfondo, nessun bordo (la sottolineatura è un elemento a parte non catturato dal selettore) |
| Chip/tag | sfondo grigio trasparente `rgba(0,0,0,0.16)`, raggio `16px` (pillola), testo piccolo (10px), peso 500 |
| Font | **Roboto**, non un font "brandizzato" — grottesca di sistema, pulita |
| Titolo di pagina (h1) | `28px`, peso `500` — non grassissimo, la gerarchia si fa più con la dimensione che col peso |
| Pulsante nell'header | `border-radius: 4px`, padding `6px 16px`, sfondo quasi trasparente su header blu |

## Le cinque scelte che rendono Docker Hub "moderno"

1. **Un solo blu deciso come colore d'azione**, usato ovunque allo stesso modo (barra, link
   attivi, pulsanti): non ci sono colori secondari che competono con lui.
2. **Bordi sottili al posto delle ombre.** Ogni card si stacca dal fondo con una linea quasi
   invisibile (8% di opacità), non con un'ombra pesante. Il risultato è piatto e ordinato, mai
   "gonfio".
3. **Un raggio unico e contenuto** (4px) su card e pulsanti: nessuna gerarchia di raggi diversi,
   tutto ha lo stesso angolo smussato leggero.
4. **Molta aria fra i blocchi**, testo che respira, niente affollamento.
5. **Barra superiore fissa e piena di colore**, non una sidebar: la navigazione principale è
   orizzontale, in vista sempre, e usa lo stesso blu del marchio invece di un grigio neutro.

## Cosa portiamo in Gestora, cosa no

**Portiamo:** l'idea di un solo blu-azione, i bordi sottili al posto delle ombre pesanti, un
raggio più contenuto sui controlli, la barra superiore fissa al posto della sidebar, l'assenza di
un font "di carattere" a favore di una grottesca pulita (nel nostro caso **Inter**, già scelta dal
programma di lavoro).

**Non portiamo:** il bianco puro come sfondo pagina (i nostri token restano su un grigio-azzurro
quasi bianco, più riposante su schermi grandi e già verificato per il contrasto), il blu pieno
come sfondo della barra superiore (in Gestora la barra resta chiara con il blu solo sugli
elementi attivi, per non perdere la leggibilità del tema scuro), il raggio a 4px netto ovunque
(nei nostri token il raggio resta a due livelli — controlli e "cose che galleggiano" — perché
Gestora ha dialog e menu che devono staccarsi di più di una card). Nessun logo, nome o testo di
Docker entra nel progetto: prendiamo solo le proporzioni e la sensazione d'insieme.

## File

Screenshot in `GestoraDocs/restyle/riferimento/`: `home-1440.png`, `postgres-1440.png`,
`explore-375.png`.
