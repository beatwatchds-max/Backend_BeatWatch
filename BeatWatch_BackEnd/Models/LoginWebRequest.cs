using System.ComponentModel.DataAnnotations;

namespace BeatWatch_BackEnd.Models;

public class LoginWebRequest
{
    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato valido.")]
    public string Correo { get; set; } = null!;

    [Required(ErrorMessage = "La contrasena es obligatoria.")]
    public string Contrasena { get; set; } = null!;

    [Required(ErrorMessage = "El token de reCAPTCHA es obligatorio.")]
    public string RecaptchaToken { get; set; } = null!;
}
