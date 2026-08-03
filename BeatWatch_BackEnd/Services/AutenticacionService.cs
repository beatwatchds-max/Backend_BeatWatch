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

            // Búsqueda de la licencia asociada
            var licencia = await _context.Licencias
                .Find(l => l.Activa == true && (l.UsuarioId == usuario.Id || l.UsuariosAsociados.Contains(usuario.Id)))
                .FirstOrDefaultAsync();

            string idLicenciaEncontrada = licencia?.Id ?? string.Empty;

            // Evaluamos el rol
            bool esPaciente = usuario.Rol.Equals("Paciente", StringComparison.OrdinalIgnoreCase);

            bool perfilCompletado = true;      // Por defecto true para Admin/Cuidador
            bool diagnosticoCompletado = true; // Por defecto true para Admin/Cuidador
            bool dispositivoVinculado = true;  // Por defecto true para Admin/Cuidador
            string? pacienteId = null;

            // SOLO si es Paciente evaluamos sus colecciones
            if (esPaciente)
            {
                var paciente = await _context.Pacientes
                    .Find(p => p.UsuarioId == usuario.Id)
                    .FirstOrDefaultAsync();

                perfilCompletado = paciente != null;
                pacienteId = paciente?.Id;

                if (perfilCompletado)
                {
                    // Validar si ya completó su cuestionario clínico
                    diagnosticoCompletado = await _context.Arritmias
                        .Find(a => a.IdPaciente == paciente!.Id)
                        .AnyAsync();

                    // Validar si ya vinculó su reloj BeatWatch
                    dispositivoVinculado = await _context.Dispositivos
                        .Find(d => d.IdPaciente == paciente!.Id) // O d.UsuarioId == usuario.Id según tu modelo
                        .AnyAsync();
                }
                else
                {
                    diagnosticoCompletado = false;
                    dispositivoVinculado = false;
                }
            }

            // Generación del Token JWT...
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

            return new LoginMovilResponseDto
            {
                TokenJwt = tokenJwtString,
                UsuarioId = usuario.Id,
                Rol = usuario.Rol,
                Nombre = usuario.Nombre,
                Correo = usuario.Correo ?? string.Empty,
                Telefono = usuario.Telefono ?? string.Empty,
                IdLicencia = !string.IsNullOrEmpty(usuario.IdLicencia) ? usuario.IdLicencia : idLicenciaEncontrada,

                // Banderas dinámicas
                PerfilCompletado = perfilCompletado,
                DiagnosticoCompletado = diagnosticoCompletado,
                DispositivoVinculado = dispositivoVinculado, // <--- Retornamos la nueva bandera
                PacienteId = pacienteId
            };
        }
    }
}