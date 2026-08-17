using System.ComponentModel.DataAnnotations;

namespace BeatWatch_BackEnd.Dtos.notificaciones;

public class TokenFcmDto
{
    [Required]
    [StringLength(4096, MinimumLength = 1)]
    public string Token { get; set; } = string.Empty;

    [Required]
    [StringLength(256, MinimumLength = 1)]
    public string DeviceId { get; set; } = string.Empty;

    [Required]
    [StringLength(64, MinimumLength = 1)]
    public string DeviceType { get; set; } = string.Empty;
}
