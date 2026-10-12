using GestoraWebApi.Auth;

namespace GestoraWebApi.Repositories.Utenti
{
    public interface IUtentiRepository
    {
        /// <summary>
        /// V2-015: utenze mai usate, create prima di <paramref name="creatiPrimaDi"/> (UTC), dalla
        /// più vecchia, al massimo <paramref name="massimo"/>. Regola completa sull'implementazione.
        /// </summary>
        Task<List<ApplicationUser>> GetUtenzeMaiUsateAsync(DateTime creatiPrimaDi, int massimo);
    }
}
