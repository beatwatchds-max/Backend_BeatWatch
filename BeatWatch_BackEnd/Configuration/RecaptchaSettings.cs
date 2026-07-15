using System.ComponentModel.DataAnnotations;

namespace BeatWatch_BackEnd.Configuration;

public sealed class RecaptchaSettings
{
    [Required]
    public string SecretKey { get; init; } = null!;

    [Range(0, 1)]
    public decimal MinimumScore { get; init; } = 0.5m;

    [StringLength(100)]
    public string? ExpectedAction { get; init; }
}
