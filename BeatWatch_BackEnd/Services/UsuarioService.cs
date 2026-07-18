using BCrypt.Net;
using BeatWatch_BackEnd.Data;
using BeatWatch_BackEnd.infrescture;
using BeatWatch_BackEnd.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Security.Cryptography;
using System.Text;

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

    public async Task<Usuario?> AutenticarAsync(string correo, string contrasena)
    {
        var normalizedEmail = correo.Trim().ToLowerInvariant();
        var cursor = await _context.Usuarios.FindAsync(u => u.Correo == normalizedEmail);
        var usuario = await cursor.FirstOrDefaultAsync();

        if (usuario is null || !usuario.Activo || !BCrypt.Net.BCrypt.Verify(contrasena, usuario.Contrasena))
        {
            return null;
        }

        return usuario;
    }

    public async Task<string?> CrearTokenRestablecimientoAsync(string correo, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = correo.Trim().ToLowerInvariant();
        var usuario = await _context.Usuarios.Find(u => u.Correo == normalizedEmail && u.Activo)
            .FirstOrDefaultAsync(cancellationToken);
        if (usuario is null)
        {
            return null;
        }

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        var tokenHash = HashToken(token);
        var expiration = DateTime.UtcNow.AddHours(1);
        var update = Builders<Usuario>.Update
            .Set(u => u.RestablecimientoContrasenaTokenHash, tokenHash)
            .Set(u => u.RestablecimientoContrasenaExpiraEn, expiration);
        await _context.Usuarios.UpdateOneAsync(u => u.Id == usuario.Id, update, cancellationToken: cancellationToken);

        return token;
    }

    public async Task<bool> RestablecerContrasenaAsync(string token, string contrasena, CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(token);
        var filter = Builders<Usuario>.Filter.And(
            Builders<Usuario>.Filter.Eq(u => u.RestablecimientoContrasenaTokenHash, tokenHash),
            Builders<Usuario>.Filter.Gt(u => u.RestablecimientoContrasenaExpiraEn, DateTime.UtcNow));
        var update = Builders<Usuario>.Update
            .Set(u => u.Contrasena, BCrypt.Net.BCrypt.HashPassword(contrasena))
            .Unset(u => u.RestablecimientoContrasenaTokenHash)
            .Unset(u => u.RestablecimientoContrasenaExpiraEn);
        var result = await _context.Usuarios.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);

        return result.ModifiedCount == 1;
    }

    private static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    public async Task<ResultadoPaginado<Usuario>> ObtenerUsuariosPaginadosAsync(int page, int pageSize, string? searchName, string? searchEmail)
    {
        // 1. Inicializar el constructor de filtros
        var builder = Builders<Usuario>.Filter;
        var filtro = builder.Empty; // Por defecto, trae todo

        // 2. Aplicar filtros si el administrador envió parámetros
        if (!string.IsNullOrWhiteSpace(searchName))
        {
            filtro &= builder.Regex(u => u.Nombre, new BsonRegularExpression(searchName, "i"));
        }

        if (!string.IsNullOrWhiteSpace(searchEmail))
        {
            // Corregido: Usamos u.Correo según tu modelo
            filtro &= builder.Regex(u => u.Correo, new BsonRegularExpression(searchEmail, "i"));
        }

        // 3. Contar el total de documentos que coinciden con el filtro
        // Corregido: Usamos _context.Usuarios
        var totalRegistros = await _context.Usuarios.CountDocumentsAsync(filtro);

        // 4. Calcular el salto (Skip)
        var saltar = (page - 1) * pageSize;

        // 5. Ejecutar la consulta con Skip y Limit
        // Corregido: Usamos _context.Usuarios
        var usuarios = await _context.Usuarios.Find(filtro)
                                              .Skip(saltar)
                                              .Limit(pageSize)
                                              .ToListAsync();

        // 6. Retornar el objeto paginado
        return new ResultadoPaginado<Usuario>
        {
            TotalRegistros = totalRegistros,
            PaginaActual = page,
            TotalPaginas = (int)Math.Ceiling(totalRegistros / (double)pageSize),
            Datos = usuarios
        };
    }

}
