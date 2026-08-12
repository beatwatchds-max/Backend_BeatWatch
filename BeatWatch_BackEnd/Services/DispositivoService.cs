using BeatWatch_BackEnd.Data;
using BeatWatch_BackEnd.Dtos.dispositivos;
using BeatWatch_BackEnd.infrescture;
using BeatWatch_BackEnd.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using Microsoft.AspNetCore.DataProtection;
using System.Security.Cryptography;

namespace BeatWatch_BackEnd.Services
{
    public class DispositivoService : IDispositivoService
    {
        private readonly MongoDbContext _context;
        private readonly IDataProtector _tokenProtector;

        public DispositivoService(MongoDbContext context, IDataProtectionProvider dataProtectionProvider)
        {
            _context = context;
            _tokenProtector = dataProtectionProvider.CreateProtector("BeatWatch.DispositivoTokens.v1");
        }

        public DispositivoService(MongoDbContext context)
            : this(context, DataProtectionProvider.Create("BeatWatch"))
        {
        }
        #region Emparejamiento y auxiliares 
        public async Task<SesionEmparejamientoResponseDto> CrearSesionEmparejamientoAsync(CrearSesionEmparejamientoDto dto)
        {
            var idSesion = Guid.NewGuid().ToString("D");
            var tokenEmparejamiento = Guid.NewGuid().ToString("N");
            var watchSecret = Guid.NewGuid().ToString("N");
            var expiraEn = DateTime.UtcNow.AddMinutes(2); // Expira en 2 minutos

            var nuevaSesion = new SesionEmparejamiento
            {
                IdSesion = idSesion,
                TokenEmparejamiento = tokenEmparejamiento,
                WatchSecretHash = BCrypt.Net.BCrypt.HashPassword(watchSecret),
                Estado = "PENDIENTE",
                NumeroSerie = dto.NumeroSerie.Trim().ToUpperInvariant(),
                Alias = dto.Alias?.Trim() ?? "Galaxy Watch",
                TipoDispositivo = dto.TipoDispositivo,
                CodigoModelo = dto.CodigoModelo,
                CodigoDispositivo = dto.CodigoDispositivo,
                SistemaOperativo = dto.SistemaOperativo,
                VersionAplicacion = dto.VersionAplicacion,
                FechaCreacion = DateTime.UtcNow,
                FechaExpiracion = expiraEn
            };

            await _context.SesionesEmparejamiento.InsertOneAsync(nuevaSesion);

            return new SesionEmparejamientoResponseDto
            {
                IdSesion = idSesion,
                TokenEmparejamiento = tokenEmparejamiento,
                WatchSecret = watchSecret,
                ExpiraEn = expiraEn
            };
        }

        // 2. POST /api/Dispositivos/emparejar (Teléfono/Móvil escanea el QR)
        public async Task<Dispositivo> EmparejarDispositivoAsync(EmparejarDispositivoDto dto)
        {
            if (!ObjectId.TryParse(dto.IdPaciente, out _))
            {
                throw new ArgumentException("El identificador del paciente no tiene un formato válido.");
            }

            // Buscar la sesión activa
            var sesion = await _context.SesionesEmparejamiento
                .Find(s => s.IdSesion == dto.IdSesion && s.TokenEmparejamiento == dto.TokenEmparejamiento)
                .FirstOrDefaultAsync();

            if (sesion == null)
            {
                throw new ArgumentException("La sesión o token de emparejamiento no existen.");
            }

            if (sesion.FechaExpiracion < DateTime.UtcNow)
            {
                var updateExpirado = Builders<SesionEmparejamiento>.Update.Set(s => s.Estado, "EXPIRADO");
                await _context.SesionesEmparejamiento.UpdateOneAsync(s => s.Id == sesion.Id, updateExpirado);
                throw new InvalidOperationException("La sesión de emparejamiento ha expirado.");
            }

            // Verificar si el número de serie ya está registrado en dispositivos activos
            var existeDispositivo = await _context.Dispositivos
                .Find(d => d.NumeroSerie == sesion.NumeroSerie && d.Activo)
                .AnyAsync();

            if (existeDispositivo)
            {
                throw new InvalidOperationException("El dispositivo ya se encuentra emparejado.");
            }

            // Generar credenciales para el canal de comandos del wearable.
            string accessToken = $"WATCH_ACCESS_{Guid.NewGuid():N}";
            string refreshToken = $"WATCH_REFRESH_{Guid.NewGuid():N}";
            var accessTokenExpiraEn = DateTime.UtcNow.AddDays(30);

            // Crear el registro oficial del dispositivo
            var nuevoDispositivo = new Dispositivo
            {
                NumeroSerie = sesion.NumeroSerie,
                Alias = !string.IsNullOrWhiteSpace(dto.Alias) ? dto.Alias.Trim() : sesion.Alias,
                TipoDispositivo = sesion.TipoDispositivo,
                CodigoModelo = sesion.CodigoModelo,
                CodigoDispositivo = sesion.CodigoDispositivo,
                SistemaOperativo = sesion.SistemaOperativo,
                IdPaciente = dto.IdPaciente,
                FechaRegistro = DateTime.UtcNow,
                UltimaSincronizacion = DateTime.UtcNow,
                Activo = true,
                WatchAccessToken = _tokenProtector.Protect(accessToken),
                WatchAccessTokenExpiraEn = accessTokenExpiraEn,
                MetricasWearable = new MetricasWearableDto
                {
                    FrecuenciaCardiacaBpm = 0,
                    SaturacionOxigenoSpO2 = 0,
                    Pasos = 0
                }
            };

            await _context.Dispositivos.InsertOneAsync(nuevoDispositivo);

            // Actualizar la sesión a EMPAREJADO para responderle al polling del reloj
            var updateEmparejado = Builders<SesionEmparejamiento>.Update
                .Set(s => s.Estado, "EMPAREJADO")
                .Set(s => s.IdDispositivo, nuevoDispositivo.Id)
                .Set(s => s.AccessToken, _tokenProtector.Protect(accessToken))
                .Set(s => s.RefreshToken, _tokenProtector.Protect(refreshToken))
                .Set(s => s.AccessTokenExpiraEn, accessTokenExpiraEn);

            await _context.SesionesEmparejamiento.UpdateOneAsync(s => s.Id == sesion.Id, updateEmparejado);

            return nuevoDispositivo;
        }

        // 3. GET /api/Dispositivos/emparejamiento/{idSesion}/estado
        public async Task<object> ObtenerEstadoEmparejamientoAsync(string idSesion, string watchSecret)
        {
            var sesion = await _context.SesionesEmparejamiento
                .Find(s => s.IdSesion == idSesion)
                .FirstOrDefaultAsync();

            if (sesion == null || !EsWatchSecretValido(sesion, watchSecret))
            {
                throw new UnauthorizedAccessException("Credenciales de sesión o secreto no válidos.");
            }

            // Comprobar si ya expiró
            if (sesion.Estado == "PENDIENTE" && sesion.FechaExpiracion < DateTime.UtcNow)
            {
                var updateExpirado = Builders<SesionEmparejamiento>.Update.Set(s => s.Estado, "EXPIRADO");
                await _context.SesionesEmparejamiento.UpdateOneAsync(s => s.Id == sesion.Id, updateExpirado);
                sesion.Estado = "EXPIRADO";
            }

            return sesion.Estado switch
            {
                "PENDIENTE" => new
                {
                    success = true,
                    estado = "PENDIENTE",
                    idSesion = sesion.IdSesion,
                    expiraEn = sesion.FechaExpiracion.ToString("yyyy-MM-ddTHH:mm:ssZ")
                },
                "EMPAREJADO" => new
                {
                    success = true,
                    estado = "EMPAREJADO",
                    idDispositivo = sesion.IdDispositivo,
                    codigoDispositivo = sesion.CodigoDispositivo,
                    accessToken = DesprotegerToken(sesion.AccessToken),
                    refreshToken = DesprotegerToken(sesion.RefreshToken),
                    accessTokenExpiraEn = sesion.AccessTokenExpiraEn?.ToString("yyyy-MM-ddTHH:mm:ssZ")
                },
                "EXPIRADO" => new
                {
                    success = false,
                    estado = "EXPIRADO",
                    message = "La sesión de emparejamiento venció"
                },
                _ => new
                {
                    success = false,
                    estado = sesion.Estado,
                    message = "La sesión fue anulada o cancelada"
                }
            };
        }

        private static bool EsWatchSecretValido(SesionEmparejamiento sesion, string watchSecret) =>
            !string.IsNullOrWhiteSpace(sesion.WatchSecretHash)
                ? BCrypt.Net.BCrypt.Verify(watchSecret, sesion.WatchSecretHash)
                : !string.IsNullOrWhiteSpace(sesion.WatchSecret) && string.Equals(watchSecret, sesion.WatchSecret, StringComparison.Ordinal);

        private string? DesprotegerToken(string? tokenProtegido)
        {
            if (string.IsNullOrWhiteSpace(tokenProtegido)) return null;
            try
            {
                return _tokenProtector.Unprotect(tokenProtegido);
            }
            catch (CryptographicException)
            {
                // Pairing records created before this change remain valid only until they expire.
                return tokenProtegido;
            }
        }
        #endregion

        #region Consultas de dispositivos
        public async Task<List<Dispositivo>> ObtenerDispositivosPorPacienteAsync(string? idPaciente)
        {
            var filterBuilder = Builders<Dispositivo>.Filter;
            var filter = filterBuilder.Empty;

            if (!string.IsNullOrWhiteSpace(idPaciente))
            {
                if (!ObjectId.TryParse(idPaciente, out _))
                {
                    throw new ArgumentException("El identificador del paciente no tiene un formato válido.");
                }

                filter = filterBuilder.Eq(d => d.IdPaciente, idPaciente);
            }

            return await _context.Dispositivos.Find(filter).ToListAsync();
        }

        public async Task<List<Dispositivo>> ObtenerDispositivosPorLicenciaAsync(string idLicencia)
        {
            if (!ObjectId.TryParse(idLicencia, out _))
            {
                throw new ArgumentException("El identificador de la licencia no tiene un formato válido.");
            }

            // A. Obtener los IDs de los Pacientes registrados bajo esta Licencia
            var pacienteIds = await _context.Pacientes
                .Find(p => p.IdLicencia == idLicencia)
                .Project(p => p.Id!)
                .ToListAsync();

            if (!pacienteIds.Any())
            {
                return new List<Dispositivo>();
            }

            // B. Consultar los dispositivos vinculados a esos IDs de paciente
            var filter = Builders<Dispositivo>.Filter.In(d => d.IdPaciente, pacienteIds);
            return await _context.Dispositivos.Find(filter).ToListAsync();
        }

        public async Task<Dispositivo?> ObtenerDispositivoAsync(string idDispositivo)
        {
            if (!ObjectId.TryParse(idDispositivo, out _))
            {
                throw new ArgumentException("El identificador del dispositivo no tiene un formato válido.");
            }

            return await _context.Dispositivos.Find(d => d.Id == idDispositivo && d.Activo).FirstOrDefaultAsync();
        }

        public async Task<bool> ValidarTokenDeDispositivoAsync(string idDispositivo, string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return false;
            var dispositivo = await _context.Dispositivos
                .Find(d => (d.Id == idDispositivo || d.CodigoDispositivo == idDispositivo) && d.Activo)
                .FirstOrDefaultAsync();
            if (dispositivo?.WatchAccessToken is null || dispositivo.WatchAccessTokenExpiraEn <= DateTime.UtcNow) return false;

            return string.Equals(DesprotegerToken(dispositivo.WatchAccessToken), token, StringComparison.Ordinal);
        }
        #endregion

        #region actualizacion y eliminación de dispositivos
        public async Task<bool> ActualizarAliasAsync(string id, string nuevoAlias)
        {
            if (!ObjectId.TryParse(id, out _))
            {
                throw new ArgumentException("El identificador del dispositivo no tiene un formato válido.");
            }

            var filter = Builders<Dispositivo>.Filter.Eq(d => d.Id, id);
            var update = Builders<Dispositivo>.Update.Set(d => d.Alias, nuevoAlias.Trim());

            var result = await _context.Dispositivos.UpdateOneAsync(filter, update);
            return result.MatchedCount > 0;
        }

        public async Task<bool> EliminarDispositivoAsync(string id)
        {
            if (!ObjectId.TryParse(id, out _))
            {
                throw new ArgumentException("El identificador del dispositivo no tiene un formato válido.");
            }

            var filter = Builders<Dispositivo>.Filter.Eq(d => d.Id, id);
            var result = await _context.Dispositivos.DeleteOneAsync(filter);
            return result.DeletedCount > 0;
        }
       
       
        #endregion

    }
}
