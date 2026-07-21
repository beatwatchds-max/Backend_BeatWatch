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
        var profile = await _client.PostAsJsonAsync("/api/usuarios/perfil", new { });
        var deactivate = await _client.DeleteAsync("/api/usuarios/65f1a2b3c4d5e6f7a8b9c0d1/borrado-logico");
        var caregivers = await _client.PutAsJsonAsync("/api/usuarios/65f1a2b3c4d5e6f7a8b9c0d1/cuidadores", new { cuidadores = Array.Empty<string>() });
        var unlink = await _client.DeleteAsync("/api/usuarios/65f1a2b3c4d5e6f7a8b9c0d1/cuidadores/65f1a2b3c4d5e6f7a8b9c0d2");

        Assert.Equal(HttpStatusCode.Unauthorized, profile.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, deactivate.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, caregivers.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unlink.StatusCode);
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
