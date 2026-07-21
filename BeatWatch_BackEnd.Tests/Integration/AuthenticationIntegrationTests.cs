using System.Net;
using System.Net.Http.Json;
using BeatWatch_BackEnd.Models;

namespace BeatWatch_BackEnd.Tests.Integration;

public sealed class AuthenticationIntegrationTests : IClassFixture<BeatWatchApiFactory>
{
    private readonly HttpClient _client;

    public AuthenticationIntegrationTests(BeatWatchApiFactory factory)
    {
        _client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
    }

    [MongoIntegrationFact]
    public async Task RegistrarThenLogin_ReturnsCreatedAndAccessToken()
    {
        var email = $"test-{Guid.NewGuid():N}@beatwatch.test";
        var registration = new RegistroRequest
        {
            Nombre = "Integration Test",
            Correo = email,
            Telefono = "5551234567",
            Contrasena = "IntegrationPassword123!"
        };

        var created = await _client.PostAsJsonAsync("/api/autenticacion/registrar", registration);
        var login = await _client.PostAsJsonAsync("/api/autenticacion/login", new LoginWebRequest
        {
            Correo = email,
            Contrasena = registration.Contrasena
        });

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [MongoIntegrationFact]
    public async Task Login_ReturnsUnauthorized_ForUnknownCredentials()
    {
        var response = await _client.PostAsJsonAsync("/api/autenticacion/login", new LoginWebRequest
        {
            Correo = "missing@beatwatch.test",
            Contrasena = "IncorrectPassword123!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [MongoIntegrationFact]
    public async Task AuthenticationEndpoints_RejectInvalidPayloads()
    {
        var invalidRegistration = await _client.PostAsJsonAsync("/api/autenticacion/registrar", new { });
        var invalidLogin = await _client.PostAsJsonAsync("/api/autenticacion/login", new { correo = "not-an-email" });
        var invalidReset = await _client.PostAsJsonAsync("/api/autenticacion/restablecer-contrasena", new { token = "", contrasena = "short" });

        Assert.Equal(HttpStatusCode.BadRequest, invalidRegistration.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalidLogin.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalidReset.StatusCode);
    }
}
