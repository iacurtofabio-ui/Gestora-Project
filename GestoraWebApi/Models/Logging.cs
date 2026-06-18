namespace GestoraWebApi.Models
{
    public class Logging : Entita
    {
        public string UserId { get; set; }
        public string Action { get; set; }
        public DateTime Timestamp { get; set; }
        public string? IPAddress { get; set; }
    }
}
