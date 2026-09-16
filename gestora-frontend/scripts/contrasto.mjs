/**
 * Controllo del contrasto della tavolozza (Fase 13).
 *
 * Legge i colori da src/index.css e verifica che ogni coppia testo/fondo sia leggibile.
 * La soglia e' quella della regola WCAG AA: 4.5:1 per il testo normale, 3:1 per il testo grande
 * e per i bordi. Serve perche' una tavolozza scelta a occhio produce facilmente combinazioni
 * belle e illeggibili — ambra su crema e' l'esempio classico.
 *
 * Si esegue con:  node scripts/contrasto.mjs
 * Da rieseguire ogni volta che si tocca un colore in index.css.
 */

import { readFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import { dirname, join } from 'node:path'

const cartella = dirname(fileURLToPath(import.meta.url))
const css = readFileSync(join(cartella, '..', 'src', 'index.css'), 'utf8')

/** Estrae i token di un blocco (:root oppure .dark) come { nome: 'oklch(...)' }. */
function leggiBlocco(selettore) {
  const inizio = css.indexOf(selettore + ' {')
  if (inizio === -1) throw new Error('Blocco non trovato: ' + selettore)
  const fine = css.indexOf('\n}', inizio)
  const corpo = css.slice(inizio, fine)
  const token = {}
  for (const riga of corpo.split('\n')) {
    const m = riga.match(/^\s*--([\w-]+):\s*(.+?);/)
    if (m) token[m[1]] = m[2].trim()
  }
  return token
}

/** oklch(L C H) -> componenti sRGB lineari, tagliate dentro il gamut. */
function oklchAsRgbLineare(valore) {
  // Un colore con trasparenza (es. oklch(1 0 0 / 12%)) non si puo' valutare da solo: il colore
  // che si vede dipende da cosa c'e' sotto. Va saltato, non letto ignorando la trasparenza —
  // farlo darebbe un risultato rassicurante e falso (un bordo al 12% letto come bianco pieno).
  if (valore.includes('/')) return null
  const m = valore.match(/oklch\(\s*([\d.]+)\s+([\d.]+)\s+([\d.]+)/)
  if (!m) return null
  const [, L, C, H] = m.map(Number)

  const a = C * Math.cos((H * Math.PI) / 180)
  const b = C * Math.sin((H * Math.PI) / 180)

  const l = (L + 0.3963377774 * a + 0.2158037573 * b) ** 3
  const mm = (L - 0.1055613458 * a - 0.0638541728 * b) ** 3
  const s = (L - 0.0894841775 * a - 1.291485548 * b) ** 3

  const taglia = (x) => Math.min(1, Math.max(0, x))
  return [
    taglia(4.0767416621 * l - 3.3077115913 * mm + 0.2309699292 * s),
    taglia(-1.2684380046 * l + 2.6097574011 * mm - 0.3413193965 * s),
    taglia(-0.0041960863 * l - 0.7034186147 * mm + 1.707614701 * s),
  ]
}

/** Luminanza relativa secondo WCAG (i valori sono gia' lineari). */
function luminanza(valore) {
  const rgb = oklchAsRgbLineare(valore)
  if (!rgb) return null
  return 0.2126 * rgb[0] + 0.7152 * rgb[1] + 0.0722 * rgb[2]
}

function contrasto(a, b) {
  const la = luminanza(a)
  const lb = luminanza(b)
  if (la === null || lb === null) return null
  const [chiaro, scuro] = la > lb ? [la, lb] : [lb, la]
  return (chiaro + 0.05) / (scuro + 0.05)
}

/** Coppie testo/fondo che l'app usa davvero. */
const coppie = [
  ['foreground', 'background', 4.5, 'testo normale sulla pagina'],
  ['muted-foreground', 'background', 4.5, 'testo secondario sulla pagina'],
  ['card-foreground', 'card', 4.5, 'testo dentro una card'],
  ['muted-foreground', 'card', 4.5, 'testo secondario dentro una card'],
  ['popover-foreground', 'popover', 4.5, 'testo dentro una finestra'],
  ['primary-foreground', 'primary', 4.5, 'testo sul pulsante principale'],
  ['secondary-foreground', 'secondary', 4.5, 'testo sul pulsante secondario'],
  ['accent-foreground', 'accent', 4.5, 'testo sulla voce di menu attiva'],
  ['destructive-foreground', 'destructive', 4.5, 'testo sul pulsante di eliminazione'],
  ['success-foreground', 'success', 4.5, 'testo su sfondo "disponibile"'],
  ['warning-foreground', 'warning', 4.5, 'testo su sfondo "quasi pieno"'],
  ['sidebar-foreground', 'sidebar', 4.5, 'testo nella barra laterale'],
  ['sidebar-accent-foreground', 'sidebar-accent', 4.5, 'voce attiva nella barra laterale'],
  ['sidebar-primary-foreground', 'sidebar-primary', 4.5, 'testo sul pulsante della barra'],
  // Colori usati come TESTO su fondo pagina: e' il caso di text-destructive negli errori dei form
  // e di text-success nel semaforo di disponibilita'.
  ['destructive', 'background', 4.5, 'messaggio di errore in un form'],
  ['destructive', 'card', 4.5, 'messaggio di errore dentro una card'],
  ['success', 'background', 4.5, 'scritta "disponibile"'],
  ['success', 'card', 4.5, 'scritta "disponibile" dentro una card'],
  ['warning', 'background', 4.5, 'scritta "quasi pieno"'],
  ['warning', 'card', 4.5, 'scritta "quasi pieno" dentro una card'],
  ['primary', 'background', 4.5, 'link e testo in evidenza'],
  ['primary', 'card', 4.5, 'link dentro una card'],
  // Bordi decorativi: separano due zone, non trasportano informazione. Soglia bassa, basta che
  // si vedano.
  ['border', 'background', 1.3, 'bordo decorativo su fondo pagina'],
  ['border', 'card', 1.3, 'bordo decorativo su card'],
  // Contorno dei campi di un form: qui la soglia e' 3:1 e non e' un dettaglio estetico. E' il
  // bordo che dice dove si puo' scrivere: se non si distingue dal fondo, il campo non si vede
  // (regola WCAG 1.4.11, componenti dell'interfaccia).
  ['input', 'background', 3, 'bordo di un campo da compilare'],
  ['input', 'card', 3, 'bordo di un campo dentro una card'],
  // Aggiunta con la direzione «Turno»: i form stanno quasi tutti dentro un dialogo, che e'
  // una superficie a se' (--popover). Mancando questa coppia, il bordo dei campi del modal
  // Prenotazione risultava a norma nel controllo e sotto soglia a schermo.
  ['input', 'popover', 3, 'bordo di un campo dentro un dialogo'],
  ['ring', 'background', 3, 'contorno del campo attivo'],
  ['ring', 'card', 3, 'contorno del campo attivo dentro una card'],
  ['ring', 'popover', 3, 'contorno del campo attivo dentro un dialogo'],
  ['ring', 'sidebar', 3, 'contorno della voce attiva nella barra laterale'],
  // Voci di menu (Select e DropdownMenu). L'evidenziazione e' un gradino di elevazione sopra la
  // superficie del menu, non una campitura: il testo resta quello normale, quindi la coppia da
  // verificare e' popover-foreground sul fondo evidenziato. La seconda riga controlla che il
  // gradino si veda davvero: sul tema scuro, quando l'evidenziazione era --accent, il gradino
  // valeva 0,016 di chiarezza sopra il popover, cioe' niente.
  ['popover-foreground', 'evidenza', 4.5, 'testo della voce evidenziata in un menu'],
  ['evidenza', 'popover', 1.25, 'evidenziazione della voce rispetto al menu'],
  ['destructive', 'popover', 4.5, 'voce "elimina" in un menu'],
  ['primary', 'popover', 4.5, 'testo in evidenza dentro un dialogo'],
  // Il contorno dell'azione di riga a riposo. Soglia 3:1 come per il bordo dei campi: e' un
  // comando, e la regola WCAG 1.4.11 chiede che il suo contorno si distingua dal fondo. Senza
  // questo controllo il bordo poteva restare cosi' tenue da non dare nessuna affordance, che e'
  // il difetto che aveva la prima versione della variante.
  ['azione-bordo', 'card', 3, "contorno dell’azione di riga"],
  ['azione-bordo', 'background', 3, "contorno dell’azione di riga sul fondo pagina"],
  ['azione-bordo', 'popover', 3, "contorno dell’azione di riga dentro un dialogo"],
  ['primary', 'background', 4.5, "testo dell’azione di riga a riposo"],
]

/**
 * Coppie testo/campitura che devono raccontare lo STESSO stato.
 *
 * --warning e --destructive nascono per essere leggibili come testo, quindi sono scuri;
 * --banda-attenzione e --banda-pieno sono le campiture delle bande, che non portano testo e
 * possono essere piu' accese. Il rischio e' che lo stesso stato finisca con due tinte
 * percettivamente diverse: un giallo di qua e un arancio di la'. Qui si verifica che la TINTA
 * (la terza cifra di oklch) resti la stessa. Possono divergere per chiarezza e intensita',
 * non per colore.
 */
const coerenzaTinta = [
  ['warning', 'banda-attenzione', 'quasi pieno'],
  ['destructive', 'banda-pieno', 'pieno / eliminazione'],
]

const SCARTO_TINTA_MASSIMO = 5

function leggiTinta(valore) {
  const m = valore.match(/oklch\(\s*[\d.]+\s+([\d.]+)\s+([\d.]+)/)
  return m ? Number(m[2]) : null
}

let problemi = 0

for (const [nomeTema, selettore] of [
  ['TEMA CHIARO', ':root'],
  ['TEMA SCURO', '.dark'],
]) {
  const token = leggiBlocco(selettore)
  console.log('\n' + nomeTema)
  console.log('-'.repeat(78))

  for (const [davanti, dietro, soglia, descrizione] of coppie) {
    const c = contrasto(token[davanti], token[dietro])
    if (c === null) {
      console.log(`  saltata   ${descrizione} (${davanti} o ${dietro} usa la trasparenza)`)
      continue
    }
    const ok = c >= soglia
    if (!ok) problemi++
    const rapporto = c.toFixed(2).padStart(5)
    console.log(
      `  ${ok ? 'ok      ' : 'DA FARE '} ${rapporto}:1  (minimo ${soglia})  ${descrizione}`
    )
  }
}

for (const [nomeTema, selettore] of [
  ['TEMA CHIARO', ':root'],
  ['TEMA SCURO', '.dark'],
]) {
  const token = leggiBlocco(selettore)
  console.log('\n' + nomeTema + ' - coerenza di tinta fra testo e campitura')
  console.log('-'.repeat(78))

  for (const [testo, campitura, descrizione] of coerenzaTinta) {
    const a = leggiTinta(token[testo])
    const b = leggiTinta(token[campitura])
    if (a === null || b === null) {
      console.log('  saltata   ' + descrizione)
      continue
    }
    // La ruota dei colori e' circolare: 359 e 1 distano 2 gradi, non 358.
    const diretto = Math.abs(a - b)
    const scarto = Math.min(diretto, 360 - diretto)
    const ok = scarto <= SCARTO_TINTA_MASSIMO
    if (!ok) problemi++
    console.log(
      `  ${ok ? 'ok      ' : 'DA FARE '} ${scarto.toFixed(1).padStart(5)} gradi  (massimo ${SCARTO_TINTA_MASSIMO})  ${descrizione}: ${testo} vs ${campitura}`
    )
  }
}

console.log('\n' + '='.repeat(78))
if (problemi === 0) {
  console.log('Tutte le coppie superano la soglia.')
} else {
  console.log(`${problemi} coppie sotto la soglia: vanno corrette in src/index.css.`)
  process.exitCode = 1
}
