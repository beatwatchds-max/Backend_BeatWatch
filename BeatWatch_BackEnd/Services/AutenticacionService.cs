using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using BeatWatch_BackEnd.Models;
using BeatWatch_BackEnd.Data; // Asegúrate de tener este using para tu MongoDbContext
using Microsoft.Extensions.Configuration;

namespace BeatWatch_BackEnd.Services
{
    public class AutenticacionService
    {
        private readonly MongoDbContext _context; // Usamos tu contexto
        private readonly IConfiguration _config;

        // Inyectamos el MongoDbContext igual que en los otros servicios
        public AutenticacionService(MongoDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        public async Task<string?> ValidarTokenYGenerarJwtAsync(string tokenMovil)
        {
            // 1. Buscar al paciente activo que tenga este token usando tu DbContext
            var paciente = await _context.Usuarios
                .Find(u => u.TokenMovil == tokenMovil && u.Activo == true)
                .FirstOrDefaultAsync();

            if (paciente == null)
            {
                return null; // Token no encontrado o usuario inactivo
            }

            // 2. Preparar la clave secreta para firmar el JWT
            var jwtKey = _config["JwtSettings:SigningKey"]; // Ajustado a tu appsettings real
            var keyBytes = Encoding.UTF8.GetBytes(jwtKey!);
            var creds = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256);

            // 3. Crear los claims (la info que viaja dentro del token)
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, paciente.Id!),
                new Claim(ClaimTypes.Name, paciente.Nombre),
                new Claim(ClaimTypes.Role, paciente.Rol),
                new Claim("TokenMovil", paciente.TokenMovil!)
            };

            // 4. Construir el JWT
            var token = new JwtSecurityToken(
                issuer: _config["JwtSettings:Issuer"],
                audience: _config["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(30), // Sesión persistente por 30 días para móvil
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}