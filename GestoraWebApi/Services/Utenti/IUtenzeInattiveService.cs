namespace GestoraWebApi.Services.Utenti
{
    public interface IUtenzeInattiveService
    {
        /// <summary>V2-015: elimina un lotto di utenze mai usate. Restituisce quante ne ha eliminate.</summary>
        Task<int> EliminaUtenzeMaiUsateAsync();
    }
}
