namespace BeatWatch_BackEnd.Dtos.pacientesDtos
{
    public class CrearPacienteDto
    {
        public string NombreCompleto { get; set; } = null!;
        public string Correo { get; set; } = null!;
        public string? Telefono { get; set; }

   
        public List<string> CuidadoresIds { get; set; } = new();
    }
}
