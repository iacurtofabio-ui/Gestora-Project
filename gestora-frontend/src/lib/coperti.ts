/**
 * I tre livelli di riempimento di una fascia oraria, in un punto solo.
 *
 * Sta qui e non dentro `BandaCoperti.tsx` perche' lo usano sia la banda (per il colore del
 * riempimento) sia il numero che le sta accanto (per il colore del testo): devono dire la stessa
 * cosa, e se le soglie fossero scritte due volte prima o poi direbbero cose diverse.
 *
 * Le soglie: sotto l'85% la fascia e' normale e non deve attirare l'occhio; dall'85% in su manca
 * poco al tetto ed e' il momento in cui lo staff decide se accettare ancora; al 100% e' chiusa.
 */
const QUASI_PIENO = 85

export function quotaCoperti(prenotati: number, capienza: number): number {
  // Capienza a zero significa fascia non configurata: meglio zero che una divisione per zero.
  return capienza > 0 ? (prenotati / capienza) * 100 : 0
}

/** Classe del riempimento della banda. Vedi i token --banda-* in index.css. */
export function coloreBanda(quota: number): string {
  if (quota >= 100) return 'bg-banda-pieno'
  if (quota >= QUASI_PIENO) return 'bg-banda-attenzione'
  return 'bg-primary'
}

/** Classe del numero che accompagna la banda. */
export function coloreQuota(prenotati: number, capienza: number): string {
  const quota = quotaCoperti(prenotati, capienza)
  if (quota >= 100) return 'text-destructive'
  if (quota >= QUASI_PIENO) return 'text-warning'
  return 'text-foreground'
}
