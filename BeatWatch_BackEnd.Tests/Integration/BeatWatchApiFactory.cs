using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace BeatWatch_BackEnd.Tests.Integration;

public sealed class BeatWatchApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _connectionString = Environment.GetEnvironmentVariable("TEST_MONGODB_CONNECTION_STRING")
        ?? throw new InvalidOperationException("TEST_MONGODB_CONNECTION_STRING is required for integration tests.");

    public string DatabaseName { get; } = $"beatwatch_tests_{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("https_port", "443");
        builder.UseSetting("MongoDbSettings:ConnectionString", _connectionString);
        builder.UseSetting("MongoDbSettings:DatabaseName", DatabaseName);
        builder.UseSetting("JwtSettings:Issuer", "https://tests.beatwatch.local");
        builder.UseSetting("JwtSettings:Audience", "beatwatch-tests");
        builder.UseSetting("JwtSettings:SigningKey", "integration-tests-signing-key-must-be-32-bytes");
        builder.UseSetting("JwtSettings:ExpirationMinutes", "15");
        builder.UseSetting("RecaptchaSettings:SecretKey", "test-secret");
        builder.UseSetting("EmailSettings:PasswordResetUrl", "https://tests.beatwatch.local/reset");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.Sources.Clear();
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MongoDbSettings:ConnectionString"] = _connectionString,
                ["MongoDbSettings:DatabaseName"] = DatabaseName,
                ["JwtSettings:Issuer"] = "https://tests.beatwatch.local",
                ["JwtSettings:Audience"] = "beatwatch-tests",
                ["JwtSettings:SigningKey"] = "integration-tests-signing-key-must-be-32-bytes",
                ["JwtSettings:ExpirationMinutes"] = "15",
                ["RecaptchaSettings:SecretKey"] = "test-secret",
                ["EmailSettings:PasswordResetUrl"] = "https://tests.beatwatch.local/reset"
            });
        });
    }

    public override async ValueTask DisposeAsync()
    {
        await new MongoClient(_connectionString).DropDatabaseAsync(DatabaseName);
        await base.DisposeAsync();
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;

    Task IAsyncLifetime.DisposeAsync() => DisposeAsync().AsTask();
}
