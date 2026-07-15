using System.ComponentModel.DataAnnotations;

namespace BeatWatch_BackEnd.Models;

public class LoginWebRequest
{
    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato valido.")]
    [StringLength(254)]
    public string Correo { get; set; } = null!;

    [Required(ErrorMessage = "La contrasena es obligatoria.")]
    [StringLength(128, MinimumLength = 8)]
    public string Contrasena { get; set; } = null!;

    [Required(ErrorMessage = "El token de reCAPTCHA es obligatorio.")]
    [StringLength(4096)]
    public string RecaptchaToken { get; set; } = null!;
}
