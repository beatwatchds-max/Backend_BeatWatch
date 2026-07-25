using BeatWatch_BackEnd.Data;
using BeatWatch_BackEnd.Dtos;
using BeatWatch_BackEnd.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace BeatWatch_BackEnd.Services
{
    public class DispositivoService
    {
        private readonly MongoDbContext _context;

        public DispositivoService(MongoDbContext context)
        {
            _context = context;
        }

        public async Task<Dispositivo> EmparejarDispositivoAsync(EmparejarDispositivoDto dto)
        {
            var numeroSerieNormalizado = dto.NumeroSerie.Trim().ToUpperInvariant();

            if (!ObjectId.TryParse(dto.IdPaciente, out _))
            {
                throw new ArgumentException("El identificador del paciente no tiene un formato válido.");
            }

            var existe = await _context.Dispositivos
                .Find(d => d.NumeroSerie == numeroSerieNormalizado)
                .AnyAsync();

            if (existe)
            {
                throw new InvalidOperationException("El número de serie ya está registrado por otro usuario.");
            }

            var nuevoDispositivo = new Dispositivo
            {
                NumeroSerie = numeroSerieNormalizado,
                Alias = dto.Alias.Trim(),
                TipoDispositivo = dto.TipoDispositivo,
                CodigoModelo = dto.CodigoModelo,
                CodigoDispositivo = dto.CodigoDispositivo,
                SistemaOperativo = dto.SistemaOperativo,
                IdPaciente = dto.IdPaciente,
                FechaRegistro = DateTime.UtcNow,
                UltimaSincronizacion = DateTime.UtcNow,
                Activo = true
            };

            // Inicializamos las métricas dummy de acuerdo al tipo
            if (dto.TipoDispositivo.Equals("Smartphone", StringComparison.OrdinalIgnoreCase))
            {
                nuevoDispositivo.MetricasSmartphone = new MetricasSmartphoneDto
                {
                    VersionApp = "BITWATCH v2.2",
                    EstadoNotificaciones = "Activas",
                    EstadoGps = "Activo"
                };
            }
            else
            {
                nuevoDispositivo.MetricasWearable = new MetricasWearableDto
                {
                    FrecuenciaCardiacaBpm = 72,
                    SaturacionOxigenoSpO2 = 98,
                    Pasos = 4230
                };
            }

            await _context.Dispositivos.InsertOneAsync(nuevoDispositivo);
            return nuevoDispositivo;
        }

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

            return await _context.Dispositivos
                .Find(filter)
                .ToListAsync();
        }

        public async Task<bool> ActualizarAliasAsync(string id, string nuevoAlias)
        {
            // Validar que el ID de MongoDB tenga un formato correcto
            if (!ObjectId.TryParse(id, out _))
            {
                throw new ArgumentException("El identificador del dispositivo no tiene un formato válido.");
            }

            var filter = Builders<Dispositivo>.Filter.Eq(d => d.Id, id);
            var update = Builders<Dispositivo>.Update.Set(d => d.Alias, nuevoAlias.Trim());

            var result = await _context.Dispositivos.UpdateOneAsync(filter, update);

            // Retorna true si encontró el documento y lo actualizó
            return result.MatchedCount > 0;
        }

        public async Task<bool> EliminarDispositivoAsync(string id)
        {
            // Validar formato del ObjectId de MongoDB
            if (!ObjectId.TryParse(id, out _))
            {
                throw new ArgumentException("El identificador del dispositivo no tiene un formato válido.");
            }

            var filter = Builders<Dispositivo>.Filter.Eq(d => d.Id, id);
            var result = await _context.Dispositivos.DeleteOneAsync(filter);

            // Retorna true si encontró y eliminó el documento
            return result.DeletedCount > 0;
        }
    }
}