import { createContext } from 'react'

/**
 * Tre valori, non due. "sistema" è il predefinito e significa: segui l'impostazione del
 * dispositivo. È diverso da "chiaro": chi non ha mai scelto niente deve vedere l'app come vede
 * tutto il resto sul suo computer o telefono, e cambiare da solo se cambia il sistema.
 */
export type Tema = 'chiaro' | 'scuro' | 'sistema'

export type ThemeContextType = {
  /** Quello che l'utente ha scelto: può essere 'sistema'. */
  tema: Tema
  /** Quello che si vede davvero adesso: 'sistema' è già stato risolto. */
  temaEffettivo: 'chiaro' | 'scuro'
  impostaTema: (tema: Tema) => void
}

export const CHIAVE_TEMA = 'gestora-tema'

export const ThemeContext = createContext<ThemeContextType | null>(null)
