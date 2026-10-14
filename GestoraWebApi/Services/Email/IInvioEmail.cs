namespace GestoraWebApi.Services.Email
{
    /// <summary>V2-003: invio di una email di testo. Lancia un'eccezione se l'invio non riesce.</summary>
    public interface IInvioEmail
    {
        Task InviaAsync(string destinatario, string oggetto, string testo, CancellationToken ct = default);
    }
}
