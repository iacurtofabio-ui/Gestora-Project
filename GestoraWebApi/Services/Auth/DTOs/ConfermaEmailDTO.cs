namespace GestoraWebApi.Services.Auth.DTOs
{
    /// <summary>V2-003: i due valori contenuti nel link di conferma ricevuto per email.</summary>
    public class ConfermaEmailDTO
    {
        public string UserId { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
    }

    /// <summary>V2-003: richiesta di un nuovo link di conferma.</summary>
    public class ReinviaConfermaDTO
    {
        public string Email { get; set; } = string.Empty;
    }
}
