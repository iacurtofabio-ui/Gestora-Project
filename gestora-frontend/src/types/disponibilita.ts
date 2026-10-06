/** Rispecchia FasciaDisponibilitaDTO del backend (Services/PrenotazioniPostazioni). */
export type FasciaDisponibilitaDTO = {
  fasciaOrariaId: number
  orarioInizio: string
  orarioFine: string
  maxCoperti: number
  postiResiduiFascia: number
  totalePostiDisponibili: number
  totaleCapienza: number
  disponibilePerRichiesta: boolean
  messaggio: string | null
}

/** Rispecchia DisponibilitaResponseDTO del backend. */
export type DisponibilitaResponseDTO = {
  fasce: FasciaDisponibilitaDTO[]
}

/** Rispecchia LimitiPrenotazioneDTO del backend (GET Prenotazione/limiti-prenotazione, pubblico). */
export type LimitiPrenotazioneDTO = {
  maxCopertiPerPrenotazione: number
}
