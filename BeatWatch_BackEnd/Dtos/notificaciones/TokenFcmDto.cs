using System.ComponentModel.DataAnnotations;

namespace BeatWatch_BackEnd.Dtos.notificaciones;

public class TokenFcmDto
{
    [Required]
    [StringLength(4096, MinimumLength = 1)]
    public string Token { get; set; } = string.Empty;
}
