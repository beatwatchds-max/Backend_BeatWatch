namespace BeatWatch_BackEnd.infrescture;

public interface ICaptchaVerifier
{
    Task<bool> IsValidAsync(string token, string? remoteIpAddress, CancellationToken cancellationToken = default);
}
