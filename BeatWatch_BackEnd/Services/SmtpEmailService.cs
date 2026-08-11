using BeatWatch_BackEnd.Configuration;
using BeatWatch_BackEnd.infrescture;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

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

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
        message.To.Add(MailboxAddress.Parse(recipient));
        message.Subject = "Restablece tu contrasena de BeatWatch";

        message.Body = new TextPart("plain")
        {
            Text = $"Solicitaste restablecer tu contrasena. Usa este enlace dentro de una hora: {resetUrl}"
        };

        using var client = new SmtpClient();
        client.Timeout = 15000;

        // Para puerto 465 usa SslOnConnect
        await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, SecureSocketOptions.StartTls, cancellationToken);
        await client.AuthenticateAsync(_settings.SmtpUsername, _settings.SmtpPassword, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}