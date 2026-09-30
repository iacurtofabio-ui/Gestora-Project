export type PostazioneAssegnataDTO = {
  numero: number
  nomeZona: string | null
  // NEW-001: serve a precompilare la zona nel modal di modifica. Il backend lo restituiva gia',
  // era il tipo del frontend a non dichiararlo.
  zonaId: number
  // FASE 4 (era REV-001/NEW-001): il dato esiste dal checkpoint 2b, non usciva mai dall API.
  // 0 su un dato vecchio scritto prima di questa fase: non si stampa la parentesi in quel caso.
  numeroPosti: number
}

export type PrenotazioneDTO = {
  id: number
  dataPrenotazione: string
  numeroCoperti: number
  note: string | null
  stato: string | null
  nomeUtente: string | null
  nomeCliente: string | null
  oraInizio: string | null
  oraFine: string | null
  // NEW-001: come sopra, gia' presente nel PrenotazioneDTO lato backend.
  fasciaOrariaId: number
  postazioni: PostazioneAssegnataDTO[]
  // FASE 4: posizione (1-based) della fascia fra le fasce attive dello stesso giorno della
  // settimana. 0 se la fascia non e' piu' fra quelle attive: non si stampa in quel caso.
  numeroTurno: number
}

export type PrenotazioneCreateDTO = {
  dataPrenotazione: string
  numeroCoperti: number
  note: string | null
  fasciaOrariaId: number
  zonaId: number | null
  nomeCliente: string | null
}

export const STATI_PRENOTAZIONE = {
  ATTIVA: 'Attiva',
  IN_CORSO: 'InCorso',
  COMPLETATA: 'Completata',
  ANNULLATA: 'Annullata',
  // FASE 3: prenotazione creata ma mai confermata, con data/fascia ormai passate (no-show).
  NON_PRESENTATA: 'NonPresentata',
} as const

// Etichette leggibili: "InCorso" indica una prenotazione confermata, non necessariamente
// nella fascia oraria in corso ora - il nome nel DB/enum resta invariato per non toccare il backend.
export const STATO_LABELS: Record<string, string> = {
  [STATI_PRENOTAZIONE.ATTIVA]: 'Attiva',
  [STATI_PRENOTAZIONE.IN_CORSO]: 'Confermata',
  [STATI_PRENOTAZIONE.COMPLETATA]: 'Completata',
  [STATI_PRENOTAZIONE.ANNULLATA]: 'Annullata',
  [STATI_PRENOTAZIONE.NON_PRESENTATA]: 'Non presentata',
}
