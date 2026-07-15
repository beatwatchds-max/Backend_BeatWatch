using System.ComponentModel.DataAnnotations;

namespace BeatWatch_BackEnd.Models
{
    public class RegistroRequest
    {
        [Required]
        public string Nombre { get; set; } = null!;

        [Required]
        [EmailAddress]
        public string Correo { get; set; } = null!;

        [Required]
        [Phone]
        public string Telefono { get; set; } = null!;

        [Required]
        [MinLength(8)]
        public string Contrasena { get; set; } = null!;

        public string? EmpresaOrganizacion { get; set; }
        public string? RFC { get; set; }
        public string? Direccion { get; set; }
        public string? CiudadEstado { get; set; }
    }
}
