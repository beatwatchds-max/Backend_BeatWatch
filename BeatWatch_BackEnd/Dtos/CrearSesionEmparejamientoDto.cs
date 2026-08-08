using System.ComponentModel.DataAnnotations;

namespace BeatWatch_BackEnd.Dtos
{
    public class CrearSesionEmparejamientoDto
    {
        [Required]
        public string NumeroSerie { get; set; } = string.Empty;

        public string? Alias { get; set; }

        [Required]
        public string TipoDispositivo { get; set; } = string.Empty;

        [Required]
        public string CodigoModelo { get; set; } = string.Empty;

        [Required]
        public string CodigoDispositivo { get; set; } = string.Empty;

        [Required]
        public string SistemaOperativo { get; set; } = string.Empty;

        [Required]
        public string VersionAplicacion { get; set; } = string.Empty;
    }
}