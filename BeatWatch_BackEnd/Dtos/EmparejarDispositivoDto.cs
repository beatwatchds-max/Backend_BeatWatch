using System.ComponentModel.DataAnnotations;

namespace BeatWatch_BackEnd.Dtos
{
    public class EmparejarDispositivoDto
    {
        [Required(ErrorMessage = "El número de serie es obligatorio.")]
        public string NumeroSerie { get; set; } = string.Empty;

        [Required(ErrorMessage = "El alias/nombre es obligatorio.")]
        public string Alias { get; set; } = string.Empty; // Ej: "iPhone 15 Pro"

        [Required(ErrorMessage = "El tipo de dispositivo es obligatorio.")]
        public string TipoDispositivo { get; set; } = "Wearable"; // "Wearable" o "Smartphone"

        public string CodigoModelo { get; set; } = string.Empty; // Ej: "A3290"
        public string CodigoDispositivo { get; set; } = string.Empty; // Ej: "DEV-002"
        public string SistemaOperativo { get; set; } = string.Empty; // Ej: "iOS 18.1.1"

        [Required(ErrorMessage = "El id del paciente es obligatorio.")]
        public string IdPaciente { get; set; } = string.Empty;
    }
}