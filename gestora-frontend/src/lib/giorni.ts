/**
 * REV-077: nomi dei giorni della settimana, prima duplicati identici in FasciaOrariaModal e
 * FasciaOrariaPage. L'indice corrisponde a DayOfWeek del backend (0 = Domenica).
 */
export const GIORNI_SETTIMANA = [
  'Domenica',
  'Lunedì',
  'Martedì',
  'Mercoledì',
  'Giovedì',
  'Venerdì',
  'Sabato',
] as const

/**
 * FASE 4: stesso indice di GIORNI_SETTIMANA (0 = Domenica, coerente con DayOfWeek del backend),
 * ma nell'ordine in cui si leggono le sette righe di una settimana: da Lunedì. Un elenco di sette
 * righe tutte uguali, ordinato con Domenica in testa, non si distingue a colpo d'occhio.
 */
export const GIORNI_SETTIMANA_DA_LUNEDI = [1, 2, 3, 4, 5, 6, 0].map((indice) => ({
  indice,
  nome: GIORNI_SETTIMANA[indice],
}))
