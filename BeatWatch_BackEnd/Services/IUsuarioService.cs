using BeatWatch_BackEnd.Models;

namespace BeatWatch_BackEnd.Services
{

public interface IUsuarioService
{
    Task<Usuario> RegistrarAsync(RegistroRequest request);
    Task<Usuario?> AutenticarAsync(string correo, string contrasena);
}
}
