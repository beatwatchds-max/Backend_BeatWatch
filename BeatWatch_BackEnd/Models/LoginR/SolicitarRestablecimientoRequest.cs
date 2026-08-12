using System.ComponentModel.DataAnnotations;

namespace BeatWatch_BackEnd.Models.LoginR;

public sealed class SolicitarRestablecimientoRequest
{
    [Required]
    [EmailAddress]
    public string Correo { get; init; } = null!;
}
