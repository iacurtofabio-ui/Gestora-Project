export type PostazioneAssegnataDTO = {
    numero: number
    nomeZona: string | null
}

export type PrenotazioneDTO = {
    id: number
    dataPrenotazione: string
    numeroCoperti: number
    note: string | null
    stato: string | null
    nomeUtente: string | null
    oraInizio: string | null
    oraFine: string | null
    postazioni: PostazioneAssegnataDTO[]
}

export type PrenotazioneCreateDTO = {
    dataPrenotazione: string
    numeroCoperti: number
    note: string | null
    fasciaOrariaId: number
    zonaId: number | null
}

export const STATI_PRENOTAZIONE = {
    ATTIVA: 'Attiva',
    IN_CORSO: 'InCorso',
    COMPLETATA: 'Completata',
    ANNULLATA: 'Annullata',
} as const