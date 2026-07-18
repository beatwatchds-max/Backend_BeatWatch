using System.Net.Http.Json;
using System.Text.Json.Serialization;
using BeatWatch_BackEnd.Configuration;
using BeatWatch_BackEnd.infrescture;
using Microsoft.Extensions.Options;

namespace BeatWatch_BackEnd.Services;

public sealed class RecaptchaVerifier : ICaptchaVerifier
{
    private readonly HttpClient _httpClient;
    private readonly RecaptchaSettings _settings;

    public RecaptchaVerifier(HttpClient httpClient, IOptions<RecaptchaSettings> settings)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
    }

    public async Task<bool> IsValidAsync(string token, string? remoteIpAddress, CancellationToken cancellationToken = default)
    {
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["secret"] = _settings.SecretKey,
            ["response"] = token,
            ["remoteip"] = remoteIpAddress ?? string.Empty
        });

        using var response = await _httpClient.PostAsync("recaptcha/api/siteverify", content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        var result = await response.Content.ReadFromJsonAsync<RecaptchaResponse>(cancellationToken: cancellationToken);
        return result?.Success == true
            && result.Score >= _settings.MinimumScore
            && (_settings.ExpectedAction is null || result.Action == _settings.ExpectedAction);
    }

    private sealed class RecaptchaResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; init; }

        [JsonPropertyName("score")]
        public decimal Score { get; init; }

        [JsonPropertyName("action")]
        public string? Action { get; init; }
    }
}
