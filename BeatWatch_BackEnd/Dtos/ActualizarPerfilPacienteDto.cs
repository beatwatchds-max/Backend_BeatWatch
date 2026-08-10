using System.ComponentModel.DataAnnotations;

namespace BeatWatch_BackEnd.DTOs
{
    public class ActualizarPerfilPacienteDto
    {
        public string? Curp { get; set; }

        [Range(1, 120)]
        public int? Edad { get; set; }

        public string? Sexo { get; set; }

        [Range(1.0, 300.0)]
        public double? Peso { get; set; }

        [Range(30.0, 250.0)]
        public double? Estatura { get; set; }

        public DateTime? FechaNacimiento { get; set; }

        public string? Direccion { get; set; }

        public string? TipoSangre { get; set; }

        public string? Fotografia { get; set; }
    }
}
