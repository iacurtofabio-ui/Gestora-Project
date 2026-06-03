export type FasciaOrariaDTO = {
    id: number
    orarioInizio: string
    orarioFine: string
    giornoSettimana: number
    maxPrenotazioni: number
    attiva: boolean
}

export type FasciaOrariaFormDTO = {
    orarioInizio: string
    orarioFine: string
    giornoSettimana: number
    maxPrenotazioni: number
    attiva: boolean
}