using BeatWatch_BackEnd.Configuration;
using BeatWatch_BackEnd.Data;
using BeatWatch_BackEnd.Models;
using BeatWatch_BackEnd.Services;
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

        await context.Usuarios.InsertOneAsync(new Usuario { Nombre = "Uno", Correo = "uno@test.local", Telefono = "1", Contrasena = "hash", FcmToken = "fcm-token-unico", FcmTokenActualizadoEn = DateTime.UtcNow.AddMinutes(-1) });
        await context.Usuarios.InsertOneAsync(new Usuario { Nombre = "Actual", Correo = "actual@test.local", Telefono = "3", Contrasena = "hash", FcmToken = "fcm-token-unico", FcmTokenActualizadoEn = DateTime.UtcNow });
        await context.Usuarios.InsertOneAsync(new Usuario { Nombre = "Sin token", Correo = "sin-token@test.local", Telefono = "2", Contrasena = "hash" });
        await context.Usuarios.InsertOneAsync(new Usuario { Nombre = "Token vacío uno", Correo = "vacio-uno@test.local", Telefono = "4", Contrasena = "hash", FcmToken = string.Empty });
        await context.Usuarios.InsertOneAsync(new Usuario { Nombre = "Token vacío dos", Correo = "vacio-dos@test.local", Telefono = "5", Contrasena = "hash", FcmToken = string.Empty });

        var initializer = new MongoDbInitializer(context, NullLogger<MongoDbInitializer>.Instance);
        await initializer.StartAsync(CancellationToken.None);

        var usuariosConToken = await context.Usuarios.Find(u => u.FcmToken == "fcm-token-unico").ToListAsync();
        Assert.Single(usuariosConToken);
        Assert.Equal("actual@test.local", usuariosConToken[0].Correo);

        await Assert.ThrowsAsync<MongoWriteException>(() => context.Usuarios.InsertOneAsync(
            new Usuario { Nombre = "Dos", Correo = "dos@test.local", Telefono = "6", Contrasena = "hash", FcmToken = "fcm-token-unico" }));
    }
}
