/**
 * Metriche del carattere di ripiego (direzione «Turno»).
 *
 * PERCHE' ESISTE
 * Archivo e' self-hosted: il browser lo scarica, e finche' non e' arrivato scrive il testo con
 * un carattere di sistema. Se i due caratteri hanno metriche diverse, in quell'istante il testo
 * occupa uno spazio diverso e la pagina si sposta sotto gli occhi (e' il CLS, Cumulative Layout
 * Shift). Il rimedio e' una @font-face di ripiego che NON scarica niente: usa un carattere gia'
 * installato e lo ridimensiona con `size-adjust`, `ascent-override` e `descent-override` perche'
 * occupi esattamente lo spazio che occupera' Archivo.
 *
 * Questo script legge quei tre numeri dal file del font vero, invece di stimarli.
 *
 * COME LEGGE IL FONT
 * Il .woff2 e' un font compresso con Brotli. Node sa decomprimere Brotli da solo
 * (zlib.brotliDecompressSync), quindi non serve nessuna libreria: si legge l'indice delle
 * tabelle del woff2, si decomprime il blocco dati e si vanno a prendere `head` (unita' per em),
 * `hhea` (ascendente/discendente) e `OS/2` (larghezza media dei caratteri).
 *
 * Si esegue con:  node scripts/metriche-ripiego.mjs
 * Da rieseguire solo se si aggiorna il pacchetto del font o si cambia carattere.
 */

import { readFileSync } from 'node:fs'
import { brotliDecompressSync } from 'node:zlib'
import { fileURLToPath } from 'node:url'
import { dirname, join } from 'node:path'

const cartella = dirname(fileURLToPath(import.meta.url))
const percorsoFont = join(
  cartella,
  '..',
  'node_modules',
  '@fontsource-variable',
  'archivo',
  'files',
  'archivo-latin-wght-normal.woff2'
)

/**
 * Metriche di Arial, il carattere su cui ricade il ripiego.
 *
 * Non si possono leggere da un file: Arial e' installato nel sistema, non nel progetto.
 *
 * ⚠️ `xAvgCharWidth` e' VERIFICATO NEL BROWSER, non copiato da una tabella. Il valore che gira
 * nelle tabelle pubblicate (904) e' di una versione vecchia del font e qui produceva un ripiego
 * del 21% piu' largo di Archivo — cioe' peggio che non mettere nessun ripiego. Misurando la
 * stessa riga di testo nei due caratteri, Arial risulta l'1,6% piu' largo di Archivo, che
 * corrisponde a 1098 unita' su 2048.
 *
 * Come rifare la misura, se un giorno il numero non tornasse (in una console del browser, sulla
 * pagina dell'app):
 *
 *   const w = (f) => { const s = document.createElement('span')
 *     s.style.cssText = 'position:absolute;left:-9999px;white-space:nowrap;font-size:100px'
 *     s.style.fontFamily = f; s.textContent = 'AaBbGg 19:30 24 coperti'
 *     document.body.appendChild(s); const x = s.getBoundingClientRect().width; s.remove(); return x }
 *   w('Arial') / w("'Archivo Variable'")
 */
const ARIAL = {
  unitsPerEm: 2048,
  ascender: 1854,
  descender: -434,
  lineGap: 67,
  xAvgCharWidth: 1098,
}

/** I 63 tag di tabella che il woff2 sa indicare con un numero invece che con 4 lettere. */
const TAG_NOTI = [
  'cmap', 'head', 'hhea', 'hmtx', 'maxp', 'name', 'OS/2', 'post',
  'cvt ', 'fpgm', 'glyf', 'loca', 'prep', 'CFF ', 'VORG', 'EBDT',
  'EBLC', 'gasp', 'hdmx', 'kern', 'LTSH', 'PCLT', 'VDMX', 'vhea',
  'vmtx', 'BASE', 'GDEF', 'GPOS', 'GSUB', 'EBSC', 'JSTF', 'MATH',
  'CBDT', 'CBLC', 'COLR', 'CPAL', 'SVG ', 'sbix', 'acnt', 'avar',
  'bdat', 'bloc', 'bsln', 'cvar', 'fdsc', 'feat', 'fmtx', 'fvar',
  'gvar', 'hsty', 'just', 'lcar', 'mort', 'morx', 'opbd', 'prop',
  'trak', 'Zapf', 'Silf', 'Glat', 'Gloc', 'Feat', 'Sill',
]

/** Numero a lunghezza variabile usato dal woff2: 7 bit per byte, l'ottavo dice "continua". */
function leggiUIntBase128(buf, posizione) {
  let valore = 0
  for (let i = 0; i < 5; i++) {
    const byte = buf[posizione + i]
    valore = (valore << 7) | (byte & 0x7f)
    if ((byte & 0x80) === 0) return { valore, letti: i + 1 }
  }
  throw new Error('Numero UIntBase128 malformato')
}

function leggiTabelleWoff2(buf) {
  if (buf.toString('ascii', 0, 4) !== 'wOF2') throw new Error('Non e’ un file woff2')

  const numeroTabelle = buf.readUInt16BE(12)
  let p = 48
  const tabelle = []

  for (let i = 0; i < numeroTabelle; i++) {
    const flags = buf[p]
    p += 1

    const indiceTag = flags & 0x3f
    let tag
    if (indiceTag === 0x3f) {
      tag = buf.toString('ascii', p, p + 4)
      p += 4
    } else {
      tag = TAG_NOTI[indiceTag]
    }

    const versioneTrasformazione = (flags >> 6) & 0x03

    const orig = leggiUIntBase128(buf, p)
    p += orig.letti
    let lunghezzaNelFlusso = orig.valore

    // La lunghezza trasformata c'e' solo quando la tabella e' davvero trasformata. Per glyf e
    // loca la "nessuna trasformazione" e' la versione 3; per tutte le altre e' la versione 0.
    const trasformata =
      tag === 'glyf' || tag === 'loca'
        ? versioneTrasformazione !== 3
        : versioneTrasformazione !== 0

    if (trasformata) {
      const trasf = leggiUIntBase128(buf, p)
      p += trasf.letti
      lunghezzaNelFlusso = trasf.valore
    }

    tabelle.push({ tag, lunghezza: lunghezzaNelFlusso })
  }

  // Da qui in poi c'e' un unico blocco compresso con dentro tutte le tabelle, una dopo l'altra
  // nell'ordine dell'indice appena letto.
  const dati = brotliDecompressSync(buf.subarray(p))

  const posizioni = {}
  let offset = 0
  for (const t of tabelle) {
    posizioni[t.tag] = offset
    offset += t.lunghezza
  }
  return { dati, posizioni }
}

const { dati, posizioni } = leggiTabelleWoff2(readFileSync(percorsoFont))

const unitsPerEm = dati.readUInt16BE(posizioni['head'] + 18)
const ascender = dati.readInt16BE(posizioni['hhea'] + 4)
const descender = dati.readInt16BE(posizioni['hhea'] + 6)
const lineGap = dati.readInt16BE(posizioni['hhea'] + 8)
const xAvgCharWidth = dati.readInt16BE(posizioni['OS/2'] + 2)

// `size-adjust` scala il carattere di ripiego finche' non occupa in larghezza quanto Archivo.
// Il confronto si fa sulla larghezza media dei caratteri, normalizzata sull'em di ciascun font.
const larghezzaArchivo = xAvgCharWidth / unitsPerEm
const larghezzaArial = ARIAL.xAvgCharWidth / ARIAL.unitsPerEm
const sizeAdjust = larghezzaArchivo / larghezzaArial

// ascent/descent-override sono espressi in percentuale dell'em GIA' ridimensionato da
// size-adjust: vanno quindi divisi per il fattore di scala.
const ascentOverride = ascender / unitsPerEm / sizeAdjust
const descentOverride = Math.abs(descender) / unitsPerEm / sizeAdjust
const lineGapOverride = lineGap / unitsPerEm / sizeAdjust

const pct = (n) => (n * 100).toFixed(1) + '%'

console.log('\nArchivo — metriche lette dal file')
console.log('-'.repeat(60))
console.log('  unita’ per em     ', unitsPerEm)
console.log('  ascendente        ', ascender)
console.log('  discendente       ', descender)
console.log('  interlinea        ', lineGap)
console.log('  larghezza media   ', xAvgCharWidth)

console.log('\nDa scrivere in src/index.css, dentro @font-face “Archivo Ripiego”')
console.log('-'.repeat(60))
console.log(`  size-adjust: ${pct(sizeAdjust)};`)
console.log(`  ascent-override: ${pct(ascentOverride)};`)
console.log(`  descent-override: ${pct(descentOverride)};`)
console.log(`  line-gap-override: ${pct(lineGapOverride)};`)
console.log('')
