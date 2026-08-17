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

    public Task<string> EnviarAsync(string token, string titulo, string cuerpo, CancellationToken cancellationToken = default)
    {
        if (_firebaseApp is null)
        {
            throw new InvalidOperationException("FCM no está configurado.");
        }

        var mensaje = new Message
        {
            Token = token,
            Notification = new Notification
            {
                Title = titulo,
                Body = cuerpo
            }
        };

        return FirebaseMessaging.GetMessaging(_firebaseApp).SendAsync(mensaje, cancellationToken);
    }
}
