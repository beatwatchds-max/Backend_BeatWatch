using System.ComponentModel.DataAnnotations;

namespace BeatWatch_BackEnd.Models;

public sealed class RestablecerContrasenaRequest
{
    [Required]
    public string Token { get; init; } = null!;

    [Required]
    [MinLength(8)]
    public string Contrasena { get; init; } = null!;
}
