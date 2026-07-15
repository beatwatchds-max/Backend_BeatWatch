using BeatWatch_BackEnd.Services;
using BeatWatch_BackEnd.Models;
using BeatWatch_BackEnd.Data;
using BCrypt.Net;
using MongoDB.Driver;

namespace BeatWatch_BackEnd.Services;

public class UsuarioService : IUsuarioService
{
    private readonly MongoDbContext _context;

    public UsuarioService(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<Usuario> RegistrarAsync(RegistroRequest request)
    {
        // 1. Verificación mockeable y segura con FirstOrDefaultAsync
        var cursor = await _context.Usuarios.FindAsync(u => u.Correo == request.Correo);
        var existente = await cursor.FirstOrDefaultAsync();

        if (existente != null)
        {
            throw new InvalidOperationException("El correo ya está registrado.");
        }

        // 2. Cifrado de contraseña
        var hash = BCrypt.Net.BCrypt.HashPassword(request.Contrasena);

        // 3. Mapear objeto con los datos del formulario de la maqueta
        var nuevoUsuario = new Usuario
        {
            Nombre = request.Nombre,
            Correo = request.Correo,
            Telefono = request.Telefono,
            Contrasena = hash,
            Activo = true,

            // Mapeo de la nueva sección opcional
            EmpresaOrganizacion = request.EmpresaOrganizacion,
            RFC = request.RFC,
            Direccion = request.Direccion,
            CiudadEstado = request.CiudadEstado,

            Cuidadores = new List<string>()
        };

        await _context.Usuarios.InsertOneAsync(nuevoUsuario);
        return nuevoUsuario;
    }
}
