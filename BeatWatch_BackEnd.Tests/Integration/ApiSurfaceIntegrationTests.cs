using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace BeatWatch_BackEnd.Tests.Integration;

public sealed class ApiSurfaceIntegrationTests : IClassFixture<BeatWatchApiFactory>
{
    private readonly HttpClient _client;

    public ApiSurfaceIntegrationTests(BeatWatchApiFactory factory)
    {
        _client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
    }

    [MongoIntegrationFact]
    public async Task PublicReadEndpoints_ReturnSuccess()
    {
        var usuarios = await _client.GetAsync("/api/usuarios?page=0&pageSize=101");
        var weather = await _client.GetAsync("/WeatherForecast");

        Assert.Equal(HttpStatusCode.OK, usuarios.StatusCode);
        Assert.Equal(HttpStatusCode.OK, weather.StatusCode);
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
    public async Task ArritmiaEndpoint_RechazaPayloadsInvalidos()
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

        Assert.Equal(HttpStatusCode.BadRequest, missingNestedObjects.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalidValues.StatusCode);
    }

    [MongoIntegrationFact]
    public async Task PaymentAndReportErrors_ReturnExpectedClientStatus()
    {
        var payment = await _client.PostAsJsonAsync("/api/licencias/procesar-pago", new
        {
            usuarioId = "65f1a2b3c4d5e6f7a8b9c0d1",
            tipoLicencia = "invalid",
            metodoPago = "OXXO"
        });
        var report = await _client.GetAsync("/api/reportes/descargar/recibo/65f1a2b3c4d5e6f7a8b9c0d1");

        Assert.Equal(HttpStatusCode.BadRequest, payment.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, report.StatusCode);
    }

    [MongoIntegrationFact]
    public async Task PatientRegistrationAndInvalidMobileLogin_ReturnExpectedStatuses()
    {
        var patient = await _client.PostAsJsonAsync("/api/pacientes/registrar", new
        {
            nombreCompleto = "Patient Integration",
            correo = $"patient-{Guid.NewGuid():N}@beatwatch.test",
            telefono = "5551234567"
        });
        var mobileLogin = await _client.PostAsJsonAsync("/api/autenticacion/iniciar-sesion-movil", new { token = "invalid" });

        Assert.Equal(HttpStatusCode.OK, patient.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, mobileLogin.StatusCode);
    }

    [MongoIntegrationFact]
    public async Task OxxoPayment_CreatesLicenseAndGeneratesPdfReceipt()
    {
        var payment = await _client.PostAsJsonAsync("/api/licencias/procesar-pago", new
        {
            usuarioId = "65f1a2b3c4d5e6f7a8b9c0d1",
            tipoLicencia = "Individual",
            metodoPago = "OXXO",
            correoElectronico = "payment@beatwatch.test"
        });
        var paymentBody = await payment.Content.ReadFromJsonAsync<JsonElement>();
        var licenseId = paymentBody.GetProperty("licencia").GetProperty("id").GetString();
        var receipt = await _client.GetAsync($"/api/reportes/descargar/recibo/{licenseId}");
        var bytes = await receipt.Content.ReadAsByteArrayAsync();

        Assert.Equal(HttpStatusCode.OK, payment.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(licenseId));
        Assert.Equal(HttpStatusCode.OK, receipt.StatusCode);
        Assert.Equal("application/pdf", receipt.Content.Headers.ContentType?.MediaType);
        Assert.True(bytes.Length > 4);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(bytes, 0, 4));
    }
}
