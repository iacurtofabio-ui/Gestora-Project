/**
 * Rispecchia MotivoDisponibilita del backend (Enums): perché la fascia è o non è prenotabile.
 * Arriva come testo. La pagina pubblica sceglie da qui la propria frase per il cliente.
 */
export type MotivoDisponibilita =
  | 'Libera'
  | 'Terminata'
  | 'TettoEsaurito'
  | 'PostiInsufficienti'
  | 'TavoliInsufficienti'

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
  /** Testo per lo Staff (lo usa PrenotazioneModal). */
  messaggio: string | null
  motivo: MotivoDisponibilita
}

/** Rispecchia DisponibilitaResponseDTO del backend. */
export type DisponibilitaResponseDTO = {
  fasce: FasciaDisponibilitaDTO[]
}

/** Rispecchia LimitiPrenotazioneDTO del backend (GET Prenotazione/limiti-prenotazione, pubblico). */
export type LimitiPrenotazioneDTO = {
  /** Limite tecnico, per tutti i ruoli. */
  maxCopertiPerPrenotazione: number
  /** V2-007: limite per il Cliente e la pagina pubblica; oltre, si contatta il ristorante. */
  maxCopertiPrenotazioneOnline: number
}
