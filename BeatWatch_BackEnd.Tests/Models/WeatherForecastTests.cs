namespace BeatWatch_BackEnd.Tests.Models;

public class WeatherForecastTests
{
    [Theory]
    [InlineData(0, 32)]
    [InlineData(20, 67)]
    [InlineData(-10, 15)]
    public void TemperatureF_ConvertsCelsiusToFahrenheit(int celsius, int expectedFahrenheit)
    {
        var forecast = new WeatherForecast { TemperatureC = celsius };

        Assert.Equal(expectedFahrenheit, forecast.TemperatureF);
    }
}
