namespace BeatWatch_BackEnd.Services;

public interface IEmailService
{
    Task SendPasswordResetAsync(string recipient, string resetUrl, CancellationToken cancellationToken = default);
}
