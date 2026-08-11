namespace BeatWatch_BackEnd.Dtos.Login
{
    public class LoginMovilResponseDto
    {
        public string TokenJwt { get; set; } = string.Empty;
        public string UsuarioId { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;

        public string? IdLicencia { get; set; }

        public bool PerfilCompletado { get; set; }
        public bool DiagnosticoCompletado { get; set; }
        public bool DispositivoVinculado { get; set; }
        public bool RegistroPacienteCompletado { get; set; }
        public string? PacienteId { get; set; }

    }
}
