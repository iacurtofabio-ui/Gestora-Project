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
    nomeCliente: string | null
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
    nomeCliente: string | null
}

export const STATI_PRENOTAZIONE = {
    ATTIVA: 'Attiva',
    IN_CORSO: 'InCorso',
    COMPLETATA: 'Completata',
    ANNULLATA: 'Annullata',
} as const

// Etichette leggibili: "InCorso" indica una prenotazione confermata, non necessariamente
// nella fascia oraria in corso ora - il nome nel DB/enum resta invariato per non toccare il backend.
export const STATO_LABELS: Record<string, string> = {
    [STATI_PRENOTAZIONE.ATTIVA]: 'Attiva',
    [STATI_PRENOTAZIONE.IN_CORSO]: 'Confermata',
    [STATI_PRENOTAZIONE.COMPLETATA]: 'Completata',
    [STATI_PRENOTAZIONE.ANNULLATA]: 'Annullata',
}