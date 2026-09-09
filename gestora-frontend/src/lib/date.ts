/**
 * REV-016 — le date "di calendario" del dominio sono sempre in ora italiana.
 *
 * Il difetto: `new Date().toISOString().split('T')[0]` produce la data in UTC, non quella del
 * locale. Fra mezzanotte e le due (ora legale) l'Italia e' gia' al giorno dopo mentre UTC e'
 * ancora al giorno prima, quindi la Dashboard chiedeva al backend i dati del giorno sbagliato —
 * proprio il problema che lato server era gia' stato risolto con IClock.TodayInRome.
 *
 * Qui il fuso e' fissato a Europe/Rome e non preso dal browser: il locale del ristorante e' un
 * dato dell'applicazione, non del dispositivo di chi guarda. Un Admin che consulta la dashboard
 * dall'estero deve vedere la giornata del ristorante, non la propria.
 */
const FUSO_ITALIA = 'Europe/Rome'

// 'en-CA' formatta le date come YYYY-MM-DD, cioe' gia' nel formato che l'API si aspetta.
const formattatoreIso = new Intl.DateTimeFormat('en-CA', {
  timeZone: FUSO_ITALIA,
  year: 'numeric',
  month: '2-digit',
  day: '2-digit',
})

/** Data odierna in Italia, come stringa YYYY-MM-DD. */
export function oggiInItalia(): string {
  return formattatoreIso.format(new Date())
}

/**
 * Lunedi' della settimana corrente (in Italia), come stringa YYYY-MM-DD.
 *
 * I conti si fanno in UTC su una data "pura" (mezzanotte Z): la parte di fuso e' gia' stata
 * risolta da oggiInItalia, e lavorare in UTC evita che l'aritmetica sui giorni scivoli quando
 * cambia l'ora legale.
 */
export function lunediSettimanaCorrenteInItalia(): string {
  const oggi = new Date(`${oggiInItalia()}T00:00:00Z`)
  const giorno = oggi.getUTCDay() // 0 = domenica
  oggi.setUTCDate(oggi.getUTCDate() - (giorno === 0 ? 6 : giorno - 1))
  return oggi.toISOString().split('T')[0]
}

/**
 * Una data YYYY-MM-DD scritta per esteso in italiano: "martedi 9 settembre".
 *
 * Serve solo a presentare: il calcolo del giorno resta quello sopra. Il fuso e' fissato come
 * nel resto del file, altrimenti una data "pura" letta a mezzanotte Z scivolerebbe al giorno
 * prima per chi guarda da un fuso indietro rispetto a Roma.
 */
const formattatoreEsteso = new Intl.DateTimeFormat('it-IT', {
  timeZone: FUSO_ITALIA,
  weekday: 'long',
  day: 'numeric',
  month: 'long',
})

export function dataEstesaInItalia(iso: string): string {
  return formattatoreEsteso.format(new Date(`${iso}T12:00:00Z`))
}

/**
 * Una data YYYY-MM-DD in forma breve: "mer 9 set".
 *
 * In tabella la data ISO grezza (2026-09-09) e' precisa e illeggibile: costringe a contare le
 * cifre per capire che giorno e'. La forma breve si legge di colpo e occupa meno spazio.
 */
const formattatoreBreve = new Intl.DateTimeFormat('it-IT', {
  timeZone: FUSO_ITALIA,
  weekday: 'short',
  day: 'numeric',
  month: 'short',
})

export function dataBreveInItalia(iso: string): string {
  return formattatoreBreve.format(new Date(`${iso}T12:00:00Z`)).replace(/\.$/, '')
}
