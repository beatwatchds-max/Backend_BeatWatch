namespace BeatWatch_BackEnd.infrescture;

public interface IEmailService
{
    Task SendPasswordResetAsync(string recipient, string resetUrl, CancellationToken cancellationToken = default);
}
