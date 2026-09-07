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

/** Come RegisterDTOValidator: obbligatorio, fra 3 e 50 caratteri. */
export const usernameSchema = z
  .string()
  .min(3, 'Almeno 3 caratteri')
  .max(50, 'Massimo 50 caratteri')

export const emailSchema = z.string().email('Email non valida')
