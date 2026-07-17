using BeatWatch_BackEnd.Models;

namespace BeatWatch_BackEnd.Services
{

public interface IUsuarioService
{
    Task<Usuario> RegistrarAsync(RegistroRequest request);
    Task<Usuario?> AutenticarAsync(string correo, string contrasena);
    Task<string?> CrearTokenRestablecimientoAsync(string correo, CancellationToken cancellationToken = default);
    Task<bool> RestablecerContrasenaAsync(string token, string contrasena, CancellationToken cancellationToken = default);
}
}
