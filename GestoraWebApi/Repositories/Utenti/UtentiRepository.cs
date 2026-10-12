using GestoraWebApi.Auth;
using GestoraWebApi.Context;
using Microsoft.EntityFrameworkCore;

namespace GestoraWebApi.Repositories.Utenti
{
    public class UtentiRepository : IUtentiRepository
    {
        private readonly GestoraContext _context;

        public UtentiRepository(GestoraContext context)
        {
            _context = context;
        }

        /// <summary>
        /// V2-015: un'utenza è "mai usata" quando valgono tutte e quattro le condizioni:
        /// <list type="number">
        /// <item>non ha nessun ruolo diverso da Cliente: Staff e Admin non si toccano mai, nemmeno
        /// se hanno anche Cliente (uno Staff appena creato dall'Admin può non essere ancora entrato);</item>
        /// <item>non ha mai fatto login;</item>
        /// <item>è stata creata prima della soglia;</item>
        /// <item>non ha nessuna prenotazione, in nessuno stato (la FK Restrict lo impedirebbe comunque).</item>
        /// </list>
        /// Le righe sono tracciate: il chiamante le passa a <c>UserManager.DeleteAsync</c>.
        /// </summary>
        public async Task<List<ApplicationUser>> GetUtenzeMaiUsateAsync(DateTime creatiPrimaDi, int massimo)
        {
            var ruoliProtetti = _context.Roles
                .Where(r => r.Name != Roles.Cliente)
                .Select(r => r.Id);

            return await _context.Users
                .Where(u => u.UltimoAccesso == null
                         && u.DataCreazione < creatiPrimaDi
                         && !_context.Prenotazioni.Any(p => p.UserId == u.Id)
                         && !_context.UserRoles.Any(ur => ur.UserId == u.Id && ruoliProtetti.Contains(ur.RoleId)))
                .OrderBy(u => u.DataCreazione)
                .ThenBy(u => u.Id)
                .Take(massimo)
                .ToListAsync();
        }
    }
}
