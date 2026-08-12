using BeatWatch_BackEnd.Dtos.cuidadores;
using BeatWatch_BackEnd.Models;
using BeatWatch_BackEnd.Models.Registro;
using BeatWatch_BackEnd.Models.Usuarios;

namespace BeatWatch_BackEnd.infrescture
{

public interface IUsuarioService
{
    Task<Usuario> RegistrarAsync(RegistroRequest request);
    Task<Usuario?> AutenticarAsync(string correo, string contrasena);
    Task<string?> CrearTokenRestablecimientoAsync(string correo, CancellationToken cancellationToken = default);
    Task<bool> RestablecerContrasenaAsync(string token, string contrasena, CancellationToken cancellationToken = default);
    Task<ResultadoPaginado<Usuario>> ObtenerUsuariosPaginadosAsync(int page, int pageSize, string? searchName, string? searchEmail, string? idLicencia);
    Task<bool> DesactivarAsync(string id, string idLicencia, CancellationToken cancellationToken = default);
    Task<bool> ActualizarCuidadoresAsync(string id, IReadOnlyCollection<string> cuidadores, string idLicencia, CancellationToken cancellationToken = default);
    Task<bool> DesvincularCuidadorAsync(string id, string cuidadorId, string idLicencia, CancellationToken cancellationToken = default);

        Task<Usuario> RegistrarCuidadorDesdeSesionAsync(RegistrarCuidadorDto request, string adminId);
        Task<List<CuidadorOpcionDto>> ObtenerCuidadoresYAdminsPorLicenciaAsync(string idLicencia);
    }
}
