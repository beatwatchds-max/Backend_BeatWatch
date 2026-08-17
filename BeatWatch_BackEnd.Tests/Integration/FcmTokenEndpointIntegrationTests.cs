using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using BeatWatch_BackEnd.Models;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using MongoDB.Driver;

namespace BeatWatch_BackEnd.Tests.Integration;

public sealed class FcmTokenEndpointIntegrationTests : IClassFixture<BeatWatchApiFactory>
{
    private readonly BeatWatchApiFactory _factory;
    private readonly HttpClient _client;

    public FcmTokenEndpointIntegrationTests(BeatWatchApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
    }

    [MongoIntegrationFact]
    public async Task RegistrarToken_SolicitudesSimultaneas_ConservaUnaSolaAsociacion()
    {
        await _client.GetAsync("/health"); // Inicia el host y ejecuta el inicializador de índices.
        var usuarios = new MongoClient(Environment.GetEnvironmentVariable("TEST_MONGODB_CONNECTION_STRING"))
            .GetDatabase(_factory.DatabaseName)
            .GetCollection<Usuario>("Usuarios");
        var primerUsuarioId = ObjectId.GenerateNewId().ToString();
        var segundoUsuarioId = ObjectId.GenerateNewId().ToString();
        await usuarios.InsertManyAsync([
            CrearUsuario(primerUsuarioId, "uno"),
            CrearUsuario(segundoUsuarioId, "dos")
        ]);

        const string token = "fcm-token-concurrente";
        var respuestas = await Task.WhenAll(
            RegistrarTokenAsync(primerUsuarioId, token),
            RegistrarTokenAsync(segundoUsuarioId, token));

        Assert.All(respuestas, respuesta => Assert.True(respuesta.StatusCode is HttpStatusCode.NoContent or HttpStatusCode.Conflict));
        Assert.Equal(1, await usuarios.CountDocumentsAsync(u => u.FcmToken == token));
    }

    private Task<HttpResponseMessage> RegistrarTokenAsync(string usuarioId, string token)
    {
        var solicitud = new HttpRequestMessage(HttpMethod.Put, "/api/Notificaciones/token")
        {
            Content = JsonContent.Create(new { token, deviceId = "android-id", deviceType = "android" })
        };
        solicitud.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CrearJwt(usuarioId));
        return _client.SendAsync(solicitud);
    }

    private static Usuario CrearUsuario(string id, string sufijo) => new()
    {
        Id = id,
        Nombre = $"Usuario {sufijo}",
        Correo = $"{sufijo}-{id}@test.local",
        Telefono = "5550000000",
        Contrasena = "hash"
    };

    private static string CrearJwt(string usuarioId)
    {
        var token = new JwtSecurityToken(
            issuer: "https://tests.beatwatch.local",
            audience: "beatwatch-tests",
            claims: [new Claim(ClaimTypes.NameIdentifier, usuarioId)],
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes("integration-tests-signing-key-must-be-32-bytes")),
                SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
