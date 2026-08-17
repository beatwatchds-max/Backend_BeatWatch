using BeatWatch_BackEnd.Configuration;
using BeatWatch_BackEnd.Data;
using BeatWatch_BackEnd.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace BeatWatch_BackEnd.Tests.Integration;

public sealed class FcmTokenIndexIntegrationTests : IClassFixture<BeatWatchApiFactory>
{
    private readonly BeatWatchApiFactory _factory;

    public FcmTokenIndexIntegrationTests(BeatWatchApiFactory factory)
    {
        _factory = factory;
    }

    [MongoIntegrationFact]
    public async Task FcmTokenIndex_RechazaTokenDuplicadoYPermiteUsuariosSinToken()
    {
        var connectionString = Environment.GetEnvironmentVariable("TEST_MONGODB_CONNECTION_STRING")!;
        var context = new MongoDbContext(
            Options.Create(new MongoDbSettings { ConnectionString = connectionString, DatabaseName = _factory.DatabaseName }),
            NullLogger<MongoDbContext>.Instance);

        await context.Usuarios.InsertOneAsync(new Usuario { Nombre = "Uno", Correo = "uno@test.local", Telefono = "1", Contrasena = "hash", FcmToken = "fcm-token-unico" });
        await context.Usuarios.InsertOneAsync(new Usuario { Nombre = "Sin token", Correo = "sin-token@test.local", Telefono = "2", Contrasena = "hash" });

        await Assert.ThrowsAsync<MongoWriteException>(() => context.Usuarios.InsertOneAsync(
            new Usuario { Nombre = "Dos", Correo = "dos@test.local", Telefono = "3", Contrasena = "hash", FcmToken = "fcm-token-unico" }));
    }
}
