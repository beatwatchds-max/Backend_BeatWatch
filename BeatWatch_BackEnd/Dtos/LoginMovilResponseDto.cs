namespace BeatWatch_BackEnd.Dtos
{
    public class LoginMovilResponseDto
    {
        public string TokenJwt { get; set; } = string.Empty;
        public string UsuarioId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;

        public string? IdLicencia { get; set; }

    }
}