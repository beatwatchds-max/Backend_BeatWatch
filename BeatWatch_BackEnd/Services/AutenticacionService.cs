using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using BeatWatch_BackEnd.Models;
using BeatWatch_BackEnd.Data;
using BeatWatch_BackEnd.Dtos.Login;

using Microsoft.Extensions.Options;
using BeatWatch_BackEnd.Configuration;

namespace BeatWatch_BackEnd.Services
{
    public class AutenticacionService
    {
        private readonly MongoDbContext _context;
        private readonly JwtSettings _settings;

        public AutenticacionService(MongoDbContext context, IOptions<JwtSettings> settings)
        {
            _context = context;
            _settings = settings.Value;
        }


        public async Task<LoginMovilResponseDto?> ValidarTokenYGenerarJwtAsync(string tokenMovil)
        {
            // 1. Buscar usuario por TokenMovil
            var usuario = await _context.Usuarios
                .Find(u => u.TokenMovil == tokenMovil && u.Activo == true)
                .FirstOrDefaultAsync();

            if (usuario?.Id is not string usuarioId) return null;

            // 🟢 2. Bloquear si ya hay una sesión activa en otro dispositivo
            if (usuario.SesionActiva)
            {
                throw new InvalidOperationException("Ya existe una sesión activa en otro dispositivo. Cierra sesión en el dispositivo actual para ingresar.");
            }

            // 3. Generar identificador de sesión único
            string nuevaSesionId = Guid.NewGuid().ToString();

            // 🟢 4. Mantener el TokenMovil en la base de datos pero marcar la Sesión como Activa
            var updateUsuario = Builders<Usuario>.Update
                .Set(u => u.SesionActiva, true)
                .Set(u => u.UltimaSesionId, nuevaSesionId)
                .Set(u => u.UltimoAcceso, DateTime.UtcNow);

            await _context.Usuarios.UpdateOneAsync(u => u.Id == usuarioId, updateUsuario);

            // Búsqueda de la licencia asociada
            var licencia = await _context.Licencias
                .Find(l => l.Activa == true && (l.UsuarioId == usuarioId || l.UsuariosAsociados.Contains(usuarioId)))
                .FirstOrDefaultAsync();

            string idLicenciaEncontrada = !string.IsNullOrEmpty(usuario.IdLicencia)
                ? usuario.IdLicencia
                : (licencia?.Id ?? string.Empty);

            bool esPaciente = usuario.Rol.Equals("Paciente", StringComparison.OrdinalIgnoreCase);

            bool perfilCompletado = true;
            bool diagnosticoCompletado = true;
            bool dispositivoVinculado = true;
            bool registroPacienteCompletado = true;
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
            else // Lógica para Administrador / Cuidador
            {
                if (!string.IsNullOrEmpty(idLicenciaEncontrada))
                {
                    var pacienteAsociado = await _context.Pacientes
                        .Find(p => p.IdLicencia == idLicenciaEncontrada)
                        .FirstOrDefaultAsync();

                    registroPacienteCompletado = pacienteAsociado != null;
                    pacienteId = pacienteAsociado?.Id;

                    if (registroPacienteCompletado)
                    {
                        diagnosticoCompletado = await _context.Arritmias
                            .Find(a => a.IdPaciente == pacienteAsociado!.Id)
                            .AnyAsync();

                        dispositivoVinculado = await _context.Dispositivos
                            .Find(d => d.IdPaciente == pacienteAsociado!.Id)
                            .AnyAsync();
                    }
                    else
                    {
                        diagnosticoCompletado = false;
                        dispositivoVinculado = false;
                    }
                }
                else
                {
                    registroPacienteCompletado = false;
                    diagnosticoCompletado = false;
                    dispositivoVinculado = false;
                }
            }

            // Generación del Token JWT
            var keyBytes = Encoding.UTF8.GetBytes(_settings.SigningKey);
            var creds = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
        new Claim(ClaimTypes.NameIdentifier, usuarioId),
        new Claim(ClaimTypes.Name, usuario.Nombre),
        new Claim(ClaimTypes.Role, usuario.Rol),
        new Claim("idLicencia", idLicenciaEncontrada),
        new Claim("tipoSesion", "movil"),
        new Claim(JwtRegisteredClaimNames.Jti, nuevaSesionId)
    };

            var tokenObject = new JwtSecurityToken(
                issuer: _settings.Issuer,
                audience: _settings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_settings.ExpirationMinutes),
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
                RegistroPacienteCompletado = registroPacienteCompletado,
                PacienteId = pacienteId
            };
        }


        public async Task<bool> CerrarSesionMovilAsync(string usuarioId)
        {
            var updateUsuario = Builders<Usuario>.Update
                .Set(u => u.SesionActiva, false)
                .Set(u => u.UltimaSesionId, null)
                .Set(u => u.TokensFcm, new List<string>());

            var result = await _context.Usuarios.UpdateOneAsync(u => u.Id == usuarioId, updateUsuario);
            return result.ModifiedCount > 0;
        }
    }
}
