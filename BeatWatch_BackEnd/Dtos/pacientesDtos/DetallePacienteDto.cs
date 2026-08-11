namespace BeatWatch_BackEnd.Dtos.pacientesDtos
{
    public class CuidadorInfoDto
    {
        //public string Id { get; set; } = null!;
        public string Nombre { get; set; } = null!;
        //public string Correo { get; set; } = null!;
        //public string Telefono { get; set; } = null!;
        //public string Rol { get; set; } = null!;
    }

    public class DetallePacienteResponseDto
    {
        public string PacienteId { get; set; } = null!;
        public string UsuarioId { get; set; } = null!;
        public string NombreCompleto { get; set; } = null!;
        public string Correo { get; set; } = null!;
        public string Telefono { get; set; } = null!;
        public string CURP { get; set; } = null!;
        public int Edad { get; set; }
        public string Sexo { get; set; } = null!;
        public double Peso { get; set; }
        public double Estatura { get; set; }
        public DateTime FechaNacimiento { get; set; }
        public string Direccion { get; set; } = null!;
        public string TipoSangre { get; set; } = null!;
        public string? IdLicencia { get; set; }
        public byte[]? Fotografia { get; set; }

        // 🟢 Asegúrate de que esta propiedad NO esté comentada
        public List<CuidadorInfoDto> Cuidadores { get; set; } = new();

        public List<object> CondicionesArritmias { get; set; } = new();
    }
}