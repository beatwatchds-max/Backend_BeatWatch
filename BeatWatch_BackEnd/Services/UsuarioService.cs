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
        // Forma correcta, limpia y 100% mockeable con MongoDB.Driver v2+
        var cursor = await _context.Usuarios.FindAsync(u => u.Correo == request.Correo);
        var existente = await cursor.FirstOrDefaultAsync(); // Reemplaza el bloque MoveNext()

        if (existente != null)
        {
            throw new InvalidOperationException("El correo ya está registrado.");
        }

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
