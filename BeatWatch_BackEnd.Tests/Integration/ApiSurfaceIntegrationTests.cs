using System.Net;
using System.Net.Http.Json;

namespace BeatWatch_BackEnd.Tests.Integration;

public sealed class ApiSurfaceIntegrationTests : IClassFixture<BeatWatchApiFactory>
{
    private readonly HttpClient _client;

    public ApiSurfaceIntegrationTests(BeatWatchApiFactory factory)
    {
        _client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
    }

    [MongoIntegrationFact]
    public async Task PublicAndProtectedReadEndpoints_ReturnExpectedStatuses()
    {
        var usuarios = await _client.GetAsync("/api/usuarios?page=0&pageSize=101");
        var health = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.Unauthorized, usuarios.StatusCode);
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
    }

    [MongoIntegrationFact]
    public async Task ProtectedUserEndpoints_RejectAnonymousRequests()
    {
        var profile = await _client.PostAsJsonAsync("/api/pacientes/perfil", new { });
        var deactivate = await _client.DeleteAsync("/api/usuarios/65f1a2b3c4d5e6f7a8b9c0d1/borrado-logico");
        var caregivers = await _client.PutAsJsonAsync("/api/usuarios/65f1a2b3c4d5e6f7a8b9c0d1/cuidadores", new { cuidadores = Array.Empty<string>() });
        var unlink = await _client.DeleteAsync("/api/usuarios/65f1a2b3c4d5e6f7a8b9c0d1/cuidadores/65f1a2b3c4d5e6f7a8b9c0d2");

        Assert.Equal(HttpStatusCode.Unauthorized, profile.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, deactivate.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, caregivers.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unlink.StatusCode);
    }

    [MongoIntegrationFact]
    public async Task NewProtectedEndpoints_RejectAnonymousRequests()
    {
        const string id = "65f1a2b3c4d5e6f7a8b9c0d1";
        var pair = await _client.PostAsJsonAsync("/api/dispositivos/emparejar", new { });
        var devices = await _client.GetAsync("/api/dispositivos");
        var update = await _client.PutAsJsonAsync($"/api/dispositivos/{id}", new { alias = "Reloj" });
        var delete = await _client.DeleteAsync($"/api/dispositivos/{id}");
        var profile = await _client.PostAsJsonAsync("/api/pacientes/perfil", new { });
        var history = await _client.GetAsync($"/api/historial?idPaciente={id}");

        Assert.Equal(HttpStatusCode.Unauthorized, pair.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, devices.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, update.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, delete.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, profile.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, history.StatusCode);
    }

    [MongoIntegrationFact]
    public async Task ArritmiaEndpoint_RechazaSolicitudesAnonimas()
    {
        var missingNestedObjects = await _client.PostAsJsonAsync("/api/salud/arritmia", new
        {
            tipo = "Taquicardia",
            frecuenciaCardiaca = 120,
            duracionEpisodioSeconds = 10,
            idPaciente = "65f1a2b3c4d5e6f7a8b9c0d1"
        });
        var invalidValues = await _client.PostAsJsonAsync("/api/salud/arritmia", new
        {
            tipo = "",
            frecuenciaCardiaca = 301,
            duracionEpisodioSeconds = -1,
            idPaciente = "invalido",
            sintomas = new { },
            factoresRiesgo = new { }
        });

        Assert.Equal(HttpStatusCode.Unauthorized, missingNestedObjects.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, invalidValues.StatusCode);
    }

    [MongoIntegrationFact]
    public async Task LicenseActivationAndReportEndpoints_RejectAnonymousRequests()
    {
        var activation = await _client.PostAsync("/api/licencias/activar-gratuita", null);
        var report = await _client.GetAsync("/api/reportes/descargar/recibo/65f1a2b3c4d5e6f7a8b9c0d1");

        Assert.Equal(HttpStatusCode.Unauthorized, activation.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, report.StatusCode);
    }

    [MongoIntegrationFact]
    public async Task PatientRegistrationRequiresAuthentication_AndMobileLoginValidatesPayload()
    {
        var patient = await _client.PostAsJsonAsync("/api/pacientes/registrar", new
        {
            nombreCompleto = "Patient Integration",
            correo = $"patient-{Guid.NewGuid():N}@beatwatch.test",
            telefono = "5551234567"
        });
        var mobileLogin = await _client.PostAsJsonAsync("/api/autenticacion/iniciar-sesion-movil", new { token = "invalid" });

        Assert.Equal(HttpStatusCode.Unauthorized, patient.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, mobileLogin.StatusCode);
    }

}
