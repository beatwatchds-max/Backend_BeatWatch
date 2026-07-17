using System.ComponentModel.DataAnnotations;

namespace BeatWatch_BackEnd.Configuration;

public sealed class EmailSettings
{
    [EmailAddress]
    public string? FromAddress { get; init; }

    public string? FromName { get; init; }

    public string? SmtpHost { get; init; }

    [Range(1, 65535)]
    public int SmtpPort { get; init; } = 587;

    public string? SmtpUsername { get; init; }

    public string? SmtpPassword { get; init; }

    [Url]
    public string? PasswordResetUrl { get; init; }
}
