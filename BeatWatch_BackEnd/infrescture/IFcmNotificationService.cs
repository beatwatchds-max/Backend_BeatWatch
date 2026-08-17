namespace BeatWatch_BackEnd.infrescture;

public interface IFcmNotificationService
{
    Task<string> EnviarAsync(string token, string titulo, string cuerpo, CancellationToken cancellationToken = default);
}
