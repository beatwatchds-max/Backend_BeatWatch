using BeatWatch_BackEnd.Models;
using BeatWatch_BackEnd.Services;
using FirebaseAdmin.Messaging;

namespace BeatWatch_BackEnd.Tests.Services;

public class FcmNotificationServiceTests
{
    [Fact]
    public void EsTokenInvalido_SoloAceptaTokenNoRegistrado()
    {
        Assert.True(FcmNotificationService.EsTokenInvalido(MessagingErrorCode.Unregistered));
        Assert.False(FcmNotificationService.EsTokenInvalido(MessagingErrorCode.InvalidArgument));
        Assert.False(FcmNotificationService.EsTokenInvalido(MessagingErrorCode.Unavailable));
    }

    [Fact]
    public void CrearMensaje_ConservaNotificacionYDatos()
    {
        var mensaje = FcmNotificationService.CrearMensaje("token", "Titulo", "Cuerpo", new Dictionary<string, string> { ["alertId"] = "alerta" });

        Assert.Equal("token", mensaje.Token);
        Assert.Equal("Titulo", mensaje.Notification.Title);
        Assert.Equal("Cuerpo", mensaje.Notification.Body);
        Assert.Equal("alerta", mensaje.Data["alertId"]);
    }

    [Fact]
    public void CrearDatosFcm_IncluyeContratoCompletoDeAlerta()
    {
        var alerta = new AlertaDispositivo
        {
            Id = "alerta-1",
            IdPaciente = "paciente-1",
            Tipo = "FRECUENCIA_ALTA",
            ValorDetectado = 120.5,
            Mensaje = "Frecuencia elevada",
            Timestamp = new DateTime(2026, 8, 16, 18, 30, 0, DateTimeKind.Utc)
        };

        var datos = AlertaService.CrearDatosFcm(alerta, "Alerta de frecuencia cardiaca");

        Assert.Equal("Alerta de frecuencia cardiaca", datos["title"]);
        Assert.Equal("alerta-1", datos["alertId"]);
        Assert.Equal("FRECUENCIA_ALTA", datos["tipo"]);
        Assert.Equal("120.5", datos["valorDetectado"]);
        Assert.Equal("2026-08-16T18:30:00.0000000Z", datos["timestamp"]);
        Assert.Equal("paciente-1", datos["pacienteId"]);
        Assert.Equal("Frecuencia elevada", datos["body"]);
    }
}
