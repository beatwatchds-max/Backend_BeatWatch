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
        // Verificar que el correo no exista usando FindAsync y MoveNext para que sea mockeable en pruebas
        var cursor = await _context.Usuarios.FindAsync(u => u.Correo == request.Correo);
        Usuario existente = null;
        if (cursor.MoveNext())
        {
            existente = cursor.Current.FirstOrDefault();
        }
        if (existente != null)
        {
            throw new InvalidOperationException("El correo ya está registrado.");
        }

        // Generar hash de la contraseña
        var hash = BCrypt.Net.BCrypt.HashPassword(request.Contrasena);

        var nuevoUsuario = new Usuario
        {
            Nombre = request.Nombre,
            Correo = request.Correo,
            Telefono = request.Telefono,
            Contrasena = hash,
            Activo = true,
            Cuidadores = new List<string>()
        };

        await _context.Usuarios.InsertOneAsync(nuevoUsuario);
        return nuevoUsuario;
    }
}
