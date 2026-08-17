using BeatWatch_BackEnd.Configuration;
using BeatWatch_BackEnd.Data;
using BeatWatch_BackEnd.Dtos;
using BeatWatch_BackEnd.infrescture;
using BeatWatch_BackEnd.Models;
using BeatWatch_BackEnd.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace BeatWatch_BackEnd.Tests.Integration;

public sealed class AlertaFcmIntegrationTests : IClassFixture<BeatWatchApiFactory>
{
    private readonly BeatWatchApiFactory _factory;

    public AlertaFcmIntegrationTests(BeatWatchApiFactory factory)
    {
        _factory = factory;
    }

    [MongoIntegrationFact]
    public async Task RegistrarAlerta_TokenNoRegistrado_LimpiaSoloCamposFcmYConservaAlerta()
    {
        var context = CrearContexto();
        var datos = await PrepararDatosAsync(context);
        var fcm = new FcmSimulado { Error = new FcmTokenInvalidoException(new InvalidOperationException()) };
        var servicio = new AlertaService(context, NullLogger<AlertaService>.Instance, fcm);

        await servicio.RegistrarAlertaAsync(datos.DispositivoId, CrearAlertaDto());

        var usuario = await context.Usuarios.Find(u => u.Id == datos.UsuarioId).FirstOrDefaultAsync();
        Assert.Null(usuario!.FcmToken);
        Assert.Null(usuario.FcmDeviceId);
        Assert.Null(usuario.FcmTokenActualizadoEn);
        Assert.True(await context.AlertasDispositivos.Find(a => a.IdPaciente == datos.PacienteId).AnyAsync());
    }

    [MongoIntegrationFact]
    public async Task RegistrarAlerta_ErrorNoTerminal_ConservaTokenYEnviaPayloadCompleto()
    {
        var context = CrearContexto();
        var datos = await PrepararDatosAsync(context);
        var fcm = new FcmSimulado { Error = new InvalidOperationException("Firebase no disponible") };
        var servicio = new AlertaService(context, NullLogger<AlertaService>.Instance, fcm);

        await servicio.RegistrarAlertaAsync(datos.DispositivoId, CrearAlertaDto());

        var usuario = await context.Usuarios.Find(u => u.Id == datos.UsuarioId).FirstOrDefaultAsync();
        Assert.Equal("fcm-token", usuario!.FcmToken);
        Assert.Equal("device-id", usuario.FcmDeviceId);
        Assert.NotNull(fcm.Datos);
        Assert.Equal("FRECUENCIA_ALTA", fcm.Datos!["tipo"]);
        Assert.Equal("120", fcm.Datos["valorDetectado"]);
        Assert.Equal(datos.PacienteId, fcm.Datos["pacienteId"]);
        Assert.False(string.IsNullOrWhiteSpace(fcm.Datos["alertId"]));
        Assert.False(string.IsNullOrWhiteSpace(fcm.Datos["timestamp"]));
    }

    private MongoDbContext CrearContexto()
    {
        return new MongoDbContext(
            Options.Create(new MongoDbSettings
            {
                ConnectionString = Environment.GetEnvironmentVariable("TEST_MONGODB_CONNECTION_STRING")!,
                DatabaseName = _factory.DatabaseName
            }),
            NullLogger<MongoDbContext>.Instance);
    }

    private static async Task<(string UsuarioId, string PacienteId, string DispositivoId)> PrepararDatosAsync(MongoDbContext context)
    {
        var usuarioId = ObjectId.GenerateNewId().ToString();
        var pacienteId = ObjectId.GenerateNewId().ToString();
        var dispositivoId = ObjectId.GenerateNewId().ToString();
        await context.Usuarios.InsertOneAsync(new Usuario
        {
            Id = usuarioId,
            Nombre = "Paciente",
            Correo = $"{usuarioId}@test.local",
            Telefono = "5550000000",
            Contrasena = "hash",
            FcmToken = "fcm-token",
            FcmDeviceId = "device-id",
            FcmTokenActualizadoEn = DateTime.UtcNow
        });
        await context.Pacientes.InsertOneAsync(new Paciente
        {
            Id = pacienteId,
            UsuarioId = usuarioId,
            CURP = $"CURP{pacienteId[..14]}",
            Edad = 30,
            Sexo = "X",
            Peso = 70,
            Estatura = 170,
            TipoSangre = "O+",
            FechaNacimiento = DateTime.UtcNow.AddYears(-30),
            Direccion = "Dirección"
        });
        await context.Dispositivos.InsertOneAsync(new Dispositivo
        {
            Id = dispositivoId,
            IdPaciente = pacienteId,
            CodigoDispositivo = $"DEV-{dispositivoId}",
            NumeroSerie = $"SER-{dispositivoId}"
        });
        return (usuarioId, pacienteId, dispositivoId);
    }

    private static CrearAlertaDto CrearAlertaDto() => new()
    {
        Tipo = "frecuencia_alta",
        ValorDetectado = 120,
        Mensaje = "Frecuencia elevada",
        Timestamp = new DateTime(2026, 8, 16, 18, 30, 0, DateTimeKind.Utc)
    };

    private sealed class FcmSimulado : IFcmNotificationService
    {
        public Exception? Error { get; init; }
        public IReadOnlyDictionary<string, string>? Datos { get; private set; }

        public Task<string> EnviarAsync(string token, string titulo, string cuerpo, IReadOnlyDictionary<string, string>? datos = null, CancellationToken cancellationToken = default)
        {
            Datos = datos;
            return Error is null ? Task.FromResult("mensaje-id") : Task.FromException<string>(Error);
        }
    }
}
