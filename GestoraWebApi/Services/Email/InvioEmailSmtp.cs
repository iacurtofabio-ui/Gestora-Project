using GestoraWebApi.Common;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace GestoraWebApi.Services.Email
{
    /// <summary>V2-003: invio reale via SMTP (MailKit). Usato quando Email:Host è valorizzato.</summary>
    public class InvioEmailSmtp : IInvioEmail
    {
        private readonly EmailSettings _settings;

        public InvioEmailSmtp(IOptions<EmailSettings> settings)
        {
            _settings = settings.Value;
        }

        public async Task InviaAsync(string destinatario, string oggetto, string testo, CancellationToken ct = default)
        {
            var messaggio = new MimeMessage();
            messaggio.From.Add(new MailboxAddress(_settings.MittenteNome, _settings.MittenteEmail));
            messaggio.To.Add(MailboxAddress.Parse(destinatario));
            messaggio.Subject = oggetto;
            messaggio.Body = new TextPart("plain") { Text = testo };

            using var client = new SmtpClient();
            await client.ConnectAsync(_settings.Host, _settings.Porta, SecureSocketOptions.Auto, ct);
            if (!string.IsNullOrEmpty(_settings.Utente))
                await client.AuthenticateAsync(_settings.Utente, _settings.Password, ct);
            await client.SendAsync(messaggio, ct);
            await client.DisconnectAsync(true, ct);
        }
    }
}
