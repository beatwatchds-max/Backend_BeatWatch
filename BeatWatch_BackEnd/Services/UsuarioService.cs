using BCrypt.Net;
using BeatWatch_BackEnd.Data;
using BeatWatch_BackEnd.Dtos;
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

    // Método privado para generar y garantizar la unicidad del token de 9 dígitos
    private async Task<string> GenerarTokenNumericoUnicoAsync()
    {
        string tokenGenerado;
        bool tokenExiste;

        do
        {
            // Genera un número aleatorio seguro entre 100,000,000 y 999,999,999
            int numeroAleatorio = RandomNumberGenerator.GetInt32(100000000, 999999999);
            tokenGenerado = numeroAleatorio.ToString();

            // Validar contra la base de datos que no exista ya
            var filter = Builders<Usuario>.Filter.Eq(u => u.TokenMovil, tokenGenerado);
            tokenExiste = await _context.Usuarios.Find(filter).AnyAsync();

        } while (tokenExiste);

        return tokenGenerado;
    }

    public async Task<Usuario> RegistrarAsync(RegistroRequest request)
    {
        // 1. Verificación segura de correo existente
        var cursor = await _context.Usuarios.FindAsync(u => u.Correo == request.Correo);
        var existente = await cursor.FirstOrDefaultAsync();

        if (existente != null)
        {
            throw new InvalidOperationException("El correo ya está registrado.");
        }

        // 2. Cifrado de contraseña
        var hash = BCrypt.Net.BCrypt.HashPassword(request.Contrasena);

        // 3. Generar token de 9 dígitos
        string nuevoToken = await GenerarTokenNumericoUnicoAsync();

        // 4. Mapear objeto únicamente con datos de la cuenta
        var nuevoUsuario = new Usuario
        {
            Nombre = request.Nombre,
            Correo = request.Correo,
            Telefono = request.Telefono,
            Contrasena = hash,
            Activo = true,
            Rol = "Administrador",
            TokenMovil = nuevoToken,
            FechaCreacion = DateTime.UtcNow,
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

    // coreccion
    public async Task<ResultadoPaginado<Usuario>> ObtenerUsuariosPaginadosAsync(
      int page,
      int pageSize,
      string? searchName,
      string? searchEmail,
      string? idLicencia)
    {
        var builder = Builders<Usuario>.Filter;
        var filtro = builder.Empty;

        // 1. Filtrar exclusivamente los usuarios ligados a la Licencia
        if (!string.IsNullOrWhiteSpace(idLicencia))
        {
            if (!ObjectId.TryParse(idLicencia, out _))
            {
                throw new ArgumentException("El idLicencia proporcionado no tiene un formato válido.");
            }

            var licencia = await _context.Licencias
                .Find(l => l.Id == idLicencia)
                .FirstOrDefaultAsync();

            if (licencia is null)
            {
                return new ResultadoPaginado<Usuario>
                {
                    TotalRegistros = 0,
                    PaginaActual = page,
                    TotalPaginas = 0,
                    Datos = new List<Usuario>()
                };
            }

            // Obtener IDs de la Licencia (Titular + UsuariosAsociados)
            var miembrosIds = new List<string>();

            if (!string.IsNullOrEmpty(licencia.UsuarioId))
            {
                miembrosIds.Add(licencia.UsuarioId);
            }

            if (licencia.UsuariosAsociados != null && licencia.UsuariosAsociados.Any())
            {
                miembrosIds.AddRange(licencia.UsuariosAsociados);
            }

            // Filtro combinado: Coincide por el campo IdLicencia del Usuario O por su ID estar dentro del array de la Licencia
            var filtroLicenciaDirecta = builder.Eq(u => u.IdLicencia, idLicencia);
            var filtroMiembrosLicencia = builder.In(u => u.Id, miembrosIds);

            filtro &= builder.Or(filtroLicenciaDirecta, filtroMiembrosLicencia);
        }

        // 2. Búsqueda opcional por nombre
        if (!string.IsNullOrWhiteSpace(searchName))
        {
            filtro &= builder.Regex(u => u.Nombre, new BsonRegularExpression(searchName, "i"));
        }

        // 3. Búsqueda opcional por correo
        if (!string.IsNullOrWhiteSpace(searchEmail))
        {
            filtro &= builder.Regex(u => u.Correo, new BsonRegularExpression(searchEmail, "i"));
        }

        // 4. Paginación y ejecución
        var totalRegistros = await _context.Usuarios.CountDocumentsAsync(filtro);
        var saltar = (page - 1) * pageSize;

        var usuarios = await _context.Usuarios.Find(filtro)
                                              .Skip(saltar)
                                              .Limit(pageSize)
                                              .ToListAsync();

        return new ResultadoPaginado<Usuario>
        {
            TotalRegistros = totalRegistros,
            PaginaActual = page,
            TotalPaginas = (int)Math.Ceiling(totalRegistros / (double)pageSize),
            Datos = usuarios
        };
    }


    public async Task<bool> DesactivarAsync(string id, CancellationToken cancellationToken = default)
    {
        var usuarioId = ValidarObjectId(id, nameof(id));
        var update = Builders<Usuario>.Update
            .Set(u => u.Activo, false)
            .Unset(u => u.RestablecimientoContrasenaTokenHash)
            .Unset(u => u.RestablecimientoContrasenaExpiraEn);
        var result = await _context.Usuarios.UpdateOneAsync(
            u => u.Id == usuarioId,
            update,
            cancellationToken: cancellationToken);

        return result.MatchedCount == 1;
    }

    public async Task<bool> ActualizarCuidadoresAsync(
        string id,
        IReadOnlyCollection<string> cuidadores,
        CancellationToken cancellationToken = default)
    {
        var usuarioId = ValidarObjectId(id, nameof(id));
        ArgumentNullException.ThrowIfNull(cuidadores);
        var cuidadoresNormalizados = cuidadores
            .Select(cuidadorId => ValidarObjectId(cuidadorId, "cuidadorId"))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        var update = Builders<Usuario>.Update.Set(u => u.Cuidadores, cuidadoresNormalizados);
        var result = await _context.Usuarios.UpdateOneAsync(
            u => u.Id == usuarioId,
            update,
            cancellationToken: cancellationToken);

        return result.MatchedCount == 1;
    }

    public async Task<bool> DesvincularCuidadorAsync(
        string id,
        string cuidadorId,
        CancellationToken cancellationToken = default)
    {
        var usuarioId = ValidarObjectId(id, nameof(id));
        var cuidadorIdValidado = ValidarObjectId(cuidadorId, nameof(cuidadorId));
        var update = Builders<Usuario>.Update.Pull(u => u.Cuidadores, cuidadorIdValidado);
        var result = await _context.Usuarios.UpdateOneAsync(
            u => u.Id == usuarioId,
            update,
            cancellationToken: cancellationToken);

        return result.MatchedCount == 1;
    }

    private static string ValidarObjectId(string id, string nombreParametro)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            throw new ArgumentException("El identificador no tiene un formato válido.", nombreParametro);
        }

        return id;
    }
    public async Task<Usuario> RegistrarCuidadorDesdeSesionAsync(RegistrarCuidadorDto request, string adminId)
    {
        // 1. Validar correo duplicado
        var cursorUsuario = await _context.Usuarios.FindAsync(u => u.Correo == request.Correo.Trim().ToLowerInvariant());
        if (await cursorUsuario.AnyAsync())
        {
            throw new InvalidOperationException("El correo ya se encuentra registrado.");
        }

        // 2. Buscar al Administrador por el ID de su sesión JWT
        var admin = await _context.Usuarios
            .Find(u => u.Id == adminId && u.Rol == "Administrador")
            .FirstOrDefaultAsync();

        if (admin == null || string.IsNullOrEmpty(admin.IdLicencia))
        {
            throw new InvalidOperationException("El administrador de la sesión no existe o no tiene una licencia asociada.");
        }

        // 3. Generar el TokenMovil de 9 dígitos para el cuidador
        string tokenMovilCuidador = await GenerarTokenNumericoUnicoAsync();
        var hash = BCrypt.Net.BCrypt.HashPassword(request.Contrasena);

        // 4. Crear el Cuidador con la IdLicencia tomada directamente del Admin
        var nuevoCuidador = new Usuario
        {
            Nombre = request.Nombre,
            Correo = request.Correo.Trim().ToLowerInvariant(),
            Telefono = request.Telefono,
            Contrasena = hash,
            Activo = true,
            Rol = "Cuidador",
            TokenMovil = tokenMovilCuidador,
            FechaCreacion = DateTime.UtcNow,
            IdLicencia = admin.IdLicencia // 👈 Copia la Licencia del Administrador autenticado
        };

        await _context.Usuarios.InsertOneAsync(nuevoCuidador);

        // 5. Vincular al array Cuidadores del Admin y a UsuariosAsociados de la Licencia
        var updateAdmin = Builders<Usuario>.Update.Push(u => u.Cuidadores, nuevoCuidador.Id!);
        await _context.Usuarios.UpdateOneAsync(u => u.Id == admin.Id, updateAdmin);

        var filterLicencia = Builders<Licencia>.Filter.Eq(l => l.Id, admin.IdLicencia);
        var updateLicencia = Builders<Licencia>.Update.Push(l => l.UsuariosAsociados, nuevoCuidador.Id!);
        await _context.Licencias.UpdateOneAsync(filterLicencia, updateLicencia);

        return nuevoCuidador;
    }
    public async Task<List<CuidadorOpcionDto>> ObtenerCuidadoresYAdminsPorLicenciaAsync(string idLicencia)
    {
        if (string.IsNullOrWhiteSpace(idLicencia) || !ObjectId.TryParse(idLicencia, out _))
        {
            throw new ArgumentException("El identificador de la licencia no tiene un formato válido.");
        }

        // Filtro: Misma licencia y Rol en ("Administrador", "Cuidador")
        var filterBuilder = Builders<Usuario>.Filter;
        var filtro = filterBuilder.Eq(u => u.IdLicencia, idLicencia) &
                     filterBuilder.In(u => u.Rol, new[] { "Administrador", "Cuidador" }) &
                     filterBuilder.Eq(u => u.Activo, true);

        var usuarios = await _context.Usuarios
            .Find(filtro)
            .Project(u => new CuidadorOpcionDto
            {
                Id = u.Id!,
                Nombre = u.Nombre,
                Correo = u.Correo,
                Rol = u.Rol
            })
            .ToListAsync();

        return usuarios;
    }
}
