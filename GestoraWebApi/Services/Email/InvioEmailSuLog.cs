namespace GestoraWebApi.Services.Email
{
    /// <summary>
    /// V2-003: solo sviluppo, quando Email:Host è vuoto. Non invia nulla: scrive nei log il
    /// contenuto della email, così il link di conferma si copia dalla console.
    /// </summary>
    public class InvioEmailSuLog : IInvioEmail
    {
        private readonly ILogger<InvioEmailSuLog> _logger;

        public InvioEmailSuLog(ILogger<InvioEmailSuLog> logger)
        {
            _logger = logger;
        }

        public Task InviaAsync(string destinatario, string oggetto, string testo, CancellationToken ct = default)
        {
            _logger.LogInformation("[Email di sviluppo, non inviata] A: {Destinatario} | Oggetto: {Oggetto}{NewLine}{Testo}",
                destinatario, oggetto, Environment.NewLine, testo);
            return Task.CompletedTask;
        }
    }
}
