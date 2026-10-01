import { z } from 'zod'

/**
 * REV-048 — regole di validazione condivise dai form che toccano le credenziali.
 *
 * La password aveva quattro definizioni diverse nel frontend e solo una era quella giusta:
 * registrazione e creazione utente da Admin chiedevano 6 caratteri qualsiasi, il reset password
 * non validava affatto, mentre il primo avvio applicava le regole vere. Le regole vere sono quelle
 * di RegisterDTOValidator e AdminResetPasswordDTOValidator (FluentValidation), non la policy di
 * ASP.NET Identity in AuthenticationExtensions: i validator girano prima e sono piu' stretti, ed e'
 * esattamente il senso di REV-013, che aveva chiuso la scorciatoia del reset da Admin.
 *
 * Effetto pratico del disallineamento: il form accettava "abc123", la chiamata partiva e l'utente
 * scopriva le regole solo dal messaggio del server. Qui la regola sta scritta una volta sola.
 */
export const passwordSchema = z
  .string()
  .min(8, 'Almeno 8 caratteri')
  .regex(/[A-Z]/, 'Serve almeno una lettera maiuscola')
  .regex(/[0-9]/, 'Serve almeno un numero')
  .regex(/[^a-zA-Z0-9]/, 'Serve almeno un carattere speciale')

/**
 * Come RegisterDTOValidator + AllowedUserNameCharacters del backend: fra 3 e 50 caratteri,
 * spazi ammessi ("Fabio Iacurto") ma non doppi. Gli spazi in testa e in coda si tolgono da soli.
 */
export const usernameSchema = z
  .string()
  .trim()
  .min(3, 'Almeno 3 caratteri')
  .max(50, 'Massimo 50 caratteri')
  .regex(/^[A-Za-z0-9 àèéìòùÀÈÉÌÒÙ'.\-_@+]+$/, "Usa lettere, numeri, spazi e i simboli . - _ ' @ +")
  .refine((u) => !u.includes('  '), 'Non usare due spazi di seguito')

export const emailSchema = z.string().email('Email non valida')

/**
 * Campo numerico intero obbligatorio (AUD-M11). Con valueAsNumber un campo vuoto vale NaN e Zod v4
 * lo scarta come tipo sbagliato, con il suo messaggio in inglese ("Invalid input: expected number,
 * received NaN"): il messaggio di .min() non compariva mai. Qui lo stesso testo vale per entrambi.
 */
export function interoObbligatorio(messaggio: string, minimo = 1) {
  return z
    .number({ error: messaggio })
    .int('Inserisci un numero intero')
    .min(minimo, messaggio)
}
