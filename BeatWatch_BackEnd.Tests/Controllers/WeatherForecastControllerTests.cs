using BeatWatch_BackEnd.Controllers;

namespace BeatWatch_BackEnd.Tests.Controllers;

public class WeatherForecastControllerTests
{
    [Fact]
    public void Get_ReturnsFiveForecastsWithinExpectedRanges()
    {
        var forecasts = new WeatherForecastController().Get().ToList();

        Assert.Equal(5, forecasts.Count);
        Assert.All(forecasts, forecast =>
        {
            Assert.InRange(forecast.TemperatureC, -20, 54);
            Assert.NotNull(forecast.Summary);
            Assert.InRange(forecast.Date, DateOnly.FromDateTime(DateTime.Today.AddDays(1)), DateOnly.FromDateTime(DateTime.Today.AddDays(6)));
        });
    }
}
