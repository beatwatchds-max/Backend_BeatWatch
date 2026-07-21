using BeatWatch_BackEnd.Models;

namespace BeatWatch_BackEnd.infrescture
{

public interface IUsuarioService
{
    Task<Usuario> RegistrarAsync(RegistroRequest request);
    Task<Usuario?> AutenticarAsync(string correo, string contrasena);
    Task<string?> CrearTokenRestablecimientoAsync(string correo, CancellationToken cancellationToken = default);
    Task<bool> RestablecerContrasenaAsync(string token, string contrasena, CancellationToken cancellationToken = default);
    Task<ResultadoPaginado<Usuario>> ObtenerUsuariosPaginadosAsync(int page, int pageSize, string? searchName, string? searchEmail);
    Task<bool> DesactivarAsync(string id, CancellationToken cancellationToken = default);
    Task<bool> ActualizarCuidadoresAsync(string id, IReadOnlyCollection<string> cuidadores, CancellationToken cancellationToken = default);
    Task<bool> DesvincularCuidadorAsync(string id, string cuidadorId, CancellationToken cancellationToken = default);
    }
}
