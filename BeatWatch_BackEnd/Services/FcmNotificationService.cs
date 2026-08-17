using BeatWatch_BackEnd.infrescture;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;

namespace BeatWatch_BackEnd.Services;

public class FcmNotificationService : IFcmNotificationService
{
    private const string ServiceAccountJsonVariable = "FIREBASE_SERVICE_ACCOUNT_JSON";
    private const string ServiceAccountFileVariable = "FIREBASE_SERVICE_ACCOUNT_FILE";
    private readonly FirebaseApp? _firebaseApp;

    public FcmNotificationService(ILogger<FcmNotificationService> logger)
    {
        var serviceAccountJson = Environment.GetEnvironmentVariable(ServiceAccountJsonVariable);
        var serviceAccountFile = Environment.GetEnvironmentVariable(ServiceAccountFileVariable);

        if (!string.IsNullOrWhiteSpace(serviceAccountJson))
        {
            _firebaseApp = FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromJson(serviceAccountJson)
            });
        }
        else if (!string.IsNullOrWhiteSpace(serviceAccountFile))
        {
            _firebaseApp = FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromFile(serviceAccountFile)
            });
        }
        else
        {
            logger.LogWarning("FCM no está configurado. Define {JsonVariable} o {FileVariable} como secreto del entorno.", ServiceAccountJsonVariable, ServiceAccountFileVariable);
        }
    }

    public async Task<string> EnviarAsync(string token, string titulo, string cuerpo, IReadOnlyDictionary<string, string>? datos = null, CancellationToken cancellationToken = default)
    {
        if (_firebaseApp is null)
        {
            throw new InvalidOperationException("FCM no está configurado.");
        }

        var mensaje = CrearMensaje(token, titulo, cuerpo, datos);

        try
        {
            return await FirebaseMessaging.GetMessaging(_firebaseApp).SendAsync(mensaje, cancellationToken);
        }
        catch (FirebaseMessagingException ex) when (EsTokenInvalido(ex.MessagingErrorCode))
        {
            throw new FcmTokenInvalidoException(ex);
        }
    }

    internal static bool EsTokenInvalido(MessagingErrorCode? errorCode) => errorCode == MessagingErrorCode.Unregistered;

    internal static Message CrearMensaje(string token, string titulo, string cuerpo, IReadOnlyDictionary<string, string>? datos) => new()
    {
        Token = token,
        Notification = new Notification
        {
            Title = titulo,
            Body = cuerpo
        },
        Data = datos is null ? null : new Dictionary<string, string>(datos)
    };
}

public sealed class FcmTokenInvalidoException : Exception
{
    public FcmTokenInvalidoException(Exception innerException)
        : base("Firebase rechazó el token FCM como inválido.", innerException)
    {
    }
}
