using System.ComponentModel.DataAnnotations;

namespace BeatWatch_BackEnd.Dtos
{
    public class EmparejarDispositivoDto
    {
        [Required]
        public string IdSesion { get; set; } = string.Empty;

        [Required]
        public string TokenEmparejamiento { get; set; } = string.Empty;

        [Required]
        public string IdPaciente { get; set; } = string.Empty;

        public string? Alias { get; set; }
    }
}