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

export type RiepilogoFascia = {
    fasciaOrariaId: number
    giornoSettimana: string
    orarioInizio: string
    orarioFine: string
    maxCoperti: number
    postiTavoli: number
    tettoCoperto: boolean
}

export type RiepilogoSala = {
    tavoliAttivi: number
    postiTotali: number
    fasce: RiepilogoFascia[]
}