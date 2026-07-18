using System.Net;
using System.Net.Mail;
using BeatWatch_BackEnd.Configuration;
using BeatWatch_BackEnd.infrescture;
using Microsoft.Extensions.Options;

namespace BeatWatch_BackEnd.Services;

public sealed class SmtpEmailService : IEmailService
{
    private readonly EmailSettings _settings;

    public SmtpEmailService(IOptions<EmailSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task SendPasswordResetAsync(string recipient, string resetUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.SmtpHost)
            || string.IsNullOrWhiteSpace(_settings.FromAddress)
            || string.IsNullOrWhiteSpace(_settings.PasswordResetUrl))
        {
            throw new InvalidOperationException("La configuracion de correo para recuperacion no esta completa.");
        }

        using var message = new MailMessage
        {
            From = new MailAddress(_settings.FromAddress, _settings.FromName),
            Subject = "Restablece tu contrasena de BeatWatch",
            Body = $"Solicitaste restablecer tu contrasena. Usa este enlace dentro de una hora: {resetUrl}",
            IsBodyHtml = false
        };
        message.To.Add(recipient);

        using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(_settings.SmtpUsername, _settings.SmtpPassword)
        };
        await client.SendMailAsync(message, cancellationToken);
    }
}
