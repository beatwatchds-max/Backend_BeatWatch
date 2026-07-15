namespace BeatWatch_BackEnd.Services;

public interface ICaptchaVerifier
{
    Task<bool> IsValidAsync(string token, string? remoteIpAddress, CancellationToken cancellationToken = default);
}
