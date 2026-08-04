using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using BeatWatch_BackEnd.Models;
using BeatWatch_BackEnd.Data;
using BeatWatch_BackEnd.Dtos;

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

        public async Task<LoginMovilResponseDto?> ValidarTokenYGenerarJwtAsync(string tokenMovil)
        {
            var usuario = await _context.Usuarios
                .Find(u => u.TokenMovil == tokenMovil && u.Activo == true)
                .FirstOrDefaultAsync();

            if (usuario?.Id is not string usuarioId) return null;

            // Búsqueda de la licencia asociada
            var licencia = await _context.Licencias
                .Find(l => l.Activa == true && (l.UsuarioId == usuarioId || l.UsuariosAsociados.Contains(usuarioId)))
                .FirstOrDefaultAsync();

            string idLicenciaEncontrada = !string.IsNullOrEmpty(usuario.IdLicencia)
                ? usuario.IdLicencia
                : (licencia?.Id ?? string.Empty);

            // Evaluamos el rol
            bool esPaciente = usuario.Rol.Equals("Paciente", StringComparison.OrdinalIgnoreCase);

            bool perfilCompletado = true;
            bool diagnosticoCompletado = true;
            bool dispositivoVinculado = true;
            string? pacienteId = null;

            if (esPaciente)
            {
                var paciente = await _context.Pacientes
                    .Find(p => p.UsuarioId == usuarioId)
                    .FirstOrDefaultAsync();

                perfilCompletado = paciente != null;
                pacienteId = paciente?.Id;

                if (perfilCompletado)
                {
                    diagnosticoCompletado = await _context.Arritmias
                        .Find(a => a.IdPaciente == paciente!.Id)
                        .AnyAsync();

                    dispositivoVinculado = await _context.Dispositivos
                        .Find(d => d.IdPaciente == paciente!.Id)
                        .AnyAsync();
                }
                else
                {
                    diagnosticoCompletado = false;
                    dispositivoVinculado = false;
                }
            }

            // Generación del Token JWT
            var jwtKey = _config["JwtSettings:SigningKey"];
            var keyBytes = Encoding.UTF8.GetBytes(jwtKey!);
            var creds = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256);

            // 🟢 AGREGAMOS idLicencia A LOS CLAIMS
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, usuarioId),
                new Claim(ClaimTypes.Name, usuario.Nombre),
                new Claim(ClaimTypes.Role, usuario.Rol),
                new Claim("TokenMovil", usuario.TokenMovil!),
                new Claim("idLicencia", idLicenciaEncontrada) // 👈 AHORA EL TOKEN SÍ LLEVA LA LICENCIA
            };

            var tokenObject = new JwtSecurityToken(
                issuer: _config["JwtSettings:Issuer"],
                audience: _config["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(30),
                signingCredentials: creds
            );

            var tokenJwtString = new JwtSecurityTokenHandler().WriteToken(tokenObject);

            return new LoginMovilResponseDto
            {
                TokenJwt = tokenJwtString,
                UsuarioId = usuarioId,
                Rol = usuario.Rol,
                Nombre = usuario.Nombre,
                Correo = usuario.Correo ?? string.Empty,
                Telefono = usuario.Telefono ?? string.Empty,
                IdLicencia = idLicenciaEncontrada,
                PerfilCompletado = perfilCompletado,
                DiagnosticoCompletado = diagnosticoCompletado,
                DispositivoVinculado = dispositivoVinculado,
                PacienteId = pacienteId
            };
        }
    }
}