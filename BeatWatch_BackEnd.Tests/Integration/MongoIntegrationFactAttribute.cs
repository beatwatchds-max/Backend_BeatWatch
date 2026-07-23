using Xunit;

namespace BeatWatch_BackEnd.Tests.Integration;

[AttributeUsage(AttributeTargets.Method)]
public sealed class MongoIntegrationFactAttribute : FactAttribute
{
    public MongoIntegrationFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TEST_MONGODB_CONNECTION_STRING")))
        {
            Skip = "Define TEST_MONGODB_CONNECTION_STRING to run MongoDB integration tests.";
        }
    }
}
