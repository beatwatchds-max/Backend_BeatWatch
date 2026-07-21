namespace BeatWatch_BackEnd.Tests.Integration;

public sealed class HealthIntegrationTests : IClassFixture<BeatWatchApiFactory>
{
    private readonly HttpClient _client;

    public HealthIntegrationTests(BeatWatchApiFactory factory)
    {
        _client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
    }

    [MongoIntegrationFact]
    public async Task Health_Returns200_WhenApplicationStartsWithMongo()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [MongoIntegrationFact]
    public async Task DatabaseStatus_Returns200_WhenIndexesAreInitialized()
    {
        var response = await _client.GetAsync("/api/test/db-status");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }
}
