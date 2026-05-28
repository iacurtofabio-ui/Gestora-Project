export type PostazioneDTO = {
    id: number
    numero: number
    capienzaMassima: number
    zonaId: number
    attiva: boolean
    prenotazioneId: number[]
}

export type PostazioneFormDTO = {
    numero: number
    capienzaMassima: number
    zonaId: number
    attiva: boolean
}