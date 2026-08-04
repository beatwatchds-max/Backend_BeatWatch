namespace BeatWatch_BackEnd.Dtos
{
    public class RegistrarPacienteCompletoDto
    {
        // --- Datos de la cuenta (Usuario) ---
        public string NombreCompleto { get; set; } = null!;
        public string Correo { get; set; } = null!;
        public string? Telefono { get; set; }

        // --- Datos Médicos / Perfil (Paciente) ---
        public string CURP { get; set; } = null!;
        public int Edad { get; set; }
        public string Sexo { get; set; } = null!;
        public double Peso { get; set; }
        public double Estatura { get; set; }
        public DateTime FechaNacimiento { get; set; }
        public string? Direccion { get; set; }
        public string TipoSangre { get; set; } = null!;
        public string? Fotografia { get; set; }
    }
}