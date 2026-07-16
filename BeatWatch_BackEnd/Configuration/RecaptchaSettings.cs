using System.ComponentModel.DataAnnotations;

namespace BeatWatch_BackEnd.Configuration;

public sealed class RecaptchaSettings
{
    public bool Enabled { get; init; } = true;

    // La clave de sitio es publica y solo la consume el cliente web.
    public string? SiteKey { get; init; }

    [Required]
    public string SecretKey { get; init; } = null!;

    [Range(0, 1)]
    public decimal MinimumScore { get; init; } = 0.5m;

    [StringLength(100)]
    public string? ExpectedAction { get; init; }
}
