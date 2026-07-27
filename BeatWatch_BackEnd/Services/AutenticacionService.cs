using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using BeatWatch_BackEnd.Models;
using BeatWatch_BackEnd.Data;
using BeatWatch_BackEnd.Dtos; // Importante para usar LoginMovilResponseDto

namespace BeatWatch_BackEnd.Services
{
    public class AutenticacionService
    {
        private readonly MongoDbContext _context;
        private readonly IConfiguration _config;

        public AutenticacionService(MongoDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // CAMBIO 1: El tipo de retorno ahora es Task<LoginMovilResponseDto?>
        public async Task<LoginMovilResponseDto?> ValidarTokenYGenerarJwtAsync(string tokenMovil)
        {
            var usuario = await _context.Usuarios
                .Find(u => u.TokenMovil == tokenMovil && u.Activo == true)
                .FirstOrDefaultAsync();

            if (usuario == null) return null;

            // --- BÚSQUEDA DINÁMICA DE LA LICENCIA ---
            // Buscamos si existe una licencia donde el UsuarioId coincida con este usuario
            // (o si guardas el ID en la licencia de alguna forma)
            var licencia = await _context.Licencias
                .Find(l => l.UsuarioId == usuario.Id && l.Activa == true)
                .FirstOrDefaultAsync();

            string idLicenciaEncontrada = licencia?.Id ?? string.Empty;
            // ----------------------------------------

            // Generar JWT
            var jwtKey = _config["JwtSettings:SigningKey"];
            var keyBytes = Encoding.UTF8.GetBytes(jwtKey!);
            var creds = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
        new Claim(ClaimTypes.NameIdentifier, usuario.Id!),
        new Claim(ClaimTypes.Name, usuario.Nombre),
        new Claim(ClaimTypes.Role, usuario.Rol),
        new Claim("TokenMovil", usuario.TokenMovil!)
    };

            var tokenObject = new JwtSecurityToken(
                issuer: _config["JwtSettings:Issuer"],
                audience: _config["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(30),
                signingCredentials: creds
            );

            var tokenJwtString = new JwtSecurityTokenHandler().WriteToken(tokenObject);

            bool esPaciente = usuario.Rol.Equals("Paciente", StringComparison.OrdinalIgnoreCase);

            return new LoginMovilResponseDto
            {
                TokenJwt = tokenJwtString,
                UsuarioId = usuario.Id,
                Rol = usuario.Rol,
                Nombre = esPaciente ? usuario.Nombre : string.Empty,
                Correo = esPaciente ? usuario.Correo : string.Empty,
                Telefono = esPaciente ? usuario.Telefono : string.Empty,

                // Asignamos el ID obtenido de la colección Licencias (o usuario.IdLicencia si viniera en el usuario)
                IdLicencia = !string.IsNullOrEmpty(usuario.IdLicencia)
                    ? usuario.IdLicencia
                    : idLicenciaEncontrada
            };
        
         }
    }
}