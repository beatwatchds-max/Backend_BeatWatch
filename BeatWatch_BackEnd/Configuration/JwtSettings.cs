using System.ComponentModel.DataAnnotations;

namespace BeatWatch_BackEnd.Configuration;

public sealed class JwtSettings
{
    [Required]
    public string Issuer { get; init; } = null!;

    [Required]
    public string Audience { get; init; } = null!;

    [Required]
    [MinLength(32)]
    public string SigningKey { get; init; } = null!;

    [Range(1, 60)]
    public int ExpirationMinutes { get; init; } = 15;
}
