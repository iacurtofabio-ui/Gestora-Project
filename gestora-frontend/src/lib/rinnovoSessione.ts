/**
 * V2-010 — quando rinnovare il token.
 *
 * Il token vale 60 minuti. Prima nessuno lo rinnovava: alla scadenza lo Staff veniva buttato fuori
 * a meta' servizio e perdeva il form che stava compilando. Ora AuthProvider chiede un token nuovo
 * poco prima della scadenza, finche' la pagina e' aperta; il backend non lo concede oltre 12 ore
 * dal login.
 *
 * Il margine e' ampio apposta: un timer in una scheda in secondo piano o su un portatile in
 * pausa parte in ritardo, e un rinnovo arrivato dopo la scadenza non e' piu' possibile.
 */
export const MARGINE_RINNOVO_MS = 10 * 60 * 1000

/** Dopo un errore di rete si riprova fra un minuto, finche' il token vale. */
export const RIPROVA_RINNOVO_MS = 60 * 1000

/**
 * Millisecondi da aspettare prima del rinnovo: zero se si e' gia' dentro il margine. Null se il
 * token non dichiara una scadenza (`exp`, in secondi): non c'e' niente da rinnovare.
 */
export function ritardoRinnovo(exp: number | undefined, adesso: number = Date.now()): number | null {
  if (typeof exp !== 'number') return null
  return Math.max(0, exp * 1000 - MARGINE_RINNOVO_MS - adesso)
}
