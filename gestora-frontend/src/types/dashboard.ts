// Risposta panoramica giornaliera
export type CopertiFasciaDTO = {
  fasciaOrariaId: number // long → number
  oraInizio: string
  oraFine: string
  maxCoperti: number
  copertiPrenotati: number
  copertiDisponibili: number
  numeroPrenotazioni: number
}

export type DashboardGiornalieraDTO = {
  data: string // DateOnly → "YYYY-MM-DD"
  totalePrenotazioni: number
  prenotazioniAttive: number
  prenotazioniInCorso: number
  prenotazioniCompletate: number
  prenotazioniAnnullate: number
  totaleCopertiPrenotati: number
  totalePostazioniAttive: number
  postazioniOccupate: number
  postazioniLibere: number
  copertiPerFascia: CopertiFasciaDTO[]
}

// Risposta panoramica settimanale
export type GiornoSettimanaleDTO = {
  data: string // DateOnly → "YYYY-MM-DD"
  giornoNome: string
  numeroPrenotazioni: number
  numeroCoperti: number
  annullate: number
}

export type DashboardSettimanaleDTO = {
  dataInizio: string // DateOnly → "YYYY-MM-DD"
  dataFine: string
  totalePrenotazioni: number
  totaleCoperti: number
  /** Percentuale prenotazioni annullate sul totale del periodo. */
  tassoAnnullamento: number
  /**
   * Percentuale no-show: prenotazioni rimaste nello stato Attiva
   * oltre la data di prenotazione (il cliente non si è presentato
   * e lo staff non ha né confermato né annullato).
   */
  tassoNoShow: number
  giorni: GiornoSettimanaleDTO[]
}