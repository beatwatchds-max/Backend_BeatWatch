using System.ComponentModel.DataAnnotations;

namespace BeatWatch_BackEnd.Dtos
{
    public class CrearPerfilPacienteDto
    {
        [Required]
        [RegularExpression(@"^[A-Za-zÑñ]{4}\d{6}[HhMm][A-Za-zÑñ]{5}[A-Za-z0-9]\d$", ErrorMessage = "La CURP no tiene un formato valido.")]
        public string CURP { get; set; } = null!;

        [Range(0, 130)]
        public int Edad { get; set; }

        [Required]
        [StringLength(20)]
        public string Sexo { get; set; } = null!;

        [Range(0.1, 500)]
        public double Peso { get; set; }

        [Range(0.1, 300)]
        public double Estatura { get; set; }

        [Required]
        [RegularExpression(@"^(A|B|AB|O)[+-]$", ErrorMessage = "El tipo de sangre debe ser A+, A-, B+, B-, AB+, AB-, O+ u O-.")]
        public string TipoSangre { get; set; } = null!;

        [Required]
        [RegularExpression(@"^[a-fA-F0-9]{24}$", ErrorMessage = "IdLicencia debe ser un ObjectId de MongoDB valido.")]
        public string IdLicencia { get; set; } = null!;

        [MaxLength(5 * 1024 * 1024, ErrorMessage = "La fotografia no puede superar 5 MB.")]
        public byte[]? Fotografia { get; set; }
    }
}
