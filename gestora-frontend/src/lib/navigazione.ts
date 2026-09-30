/**
 * Dove va un utente "a casa" in base ai ruoli. Era scritto tre volte (LoginPage,
 * RedirectSeAutenticato, UnauthorizedPage) con tre esiti diversi per lo stesso caso limite:
 * l'utente registrato a cui l'Admin ha tolto tutti i ruoli. Due lo mandavano a /dashboard, la
 * terza a /prenotazioni, e da li' ProtectedRoute lo rimandava a /unauthorized: un giro senza uscita.
 */
export function paginaDiCasa(roles: readonly string[]): string {
  if (roles.length === 0) return '/unauthorized'
  if (roles.includes('Admin') || roles.includes('Staff')) return '/dashboard'
  return '/prenotazioni'
}
