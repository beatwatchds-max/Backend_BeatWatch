namespace BeatWatch_BackEnd.Dtos
{
    public class CrearPacienteDto
    {
        public string NombreCompleto { get; set; } = null!;
        public string Correo { get; set; } = null!;
        public string? Telefono { get; set; } // Opcional según la maqueta
    }
}