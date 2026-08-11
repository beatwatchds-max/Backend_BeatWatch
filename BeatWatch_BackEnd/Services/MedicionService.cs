using BeatWatch_BackEnd.Data;
using BeatWatch_BackEnd.Dtos.pacientesDtos;
using BeatWatch_BackEnd.infrescture;
using BeatWatch_BackEnd.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace BeatWatch_BackEnd.Services
{
    public class MedicionService : IMedicionService
    {
        private readonly MongoDbContext _context;

        public MedicionService(MongoDbContext context)
        {
            _context = context;
        }

        public async Task<string> RegistrarMedicionAsync(string idDispositivoIdentificador, RegistrarMedicionDto dto)
        {
            // 1. Buscar dispositivo soportando si envían el Mongo _id o el CodigoDispositivo (ej. "watch-82d51034")
            var isObjectId = ObjectId.TryParse(idDispositivoIdentificador, out _);

            var filtroDispositivo = isObjectId
                ? Builders<Dispositivo>.Filter.Eq(d => d.Id, idDispositivoIdentificador)
                : Builders<Dispositivo>.Filter.Eq(d => d.CodigoDispositivo, idDispositivoIdentificador);

            var dispositivo = await _context.Dispositivos.Find(filtroDispositivo).FirstOrDefaultAsync();

            if (dispositivo == null)
            {
                throw new KeyNotFoundException($"El dispositivo '{idDispositivoIdentificador}' no existe.");
            }

            if (string.IsNullOrWhiteSpace(dispositivo.IdPaciente))
            {
                throw new InvalidOperationException("El dispositivo no está vinculado a ningún paciente.");
            }

            // 2. Crear documento de medición
            var nuevaMedicion = new MedicionFrecuenciaCardiaca
            {
                IdDispositivo = dispositivo.Id!,
                CodigoDispositivo = dispositivo.CodigoDispositivo,
                IdPaciente = dispositivo.IdPaciente,
                FrecuenciaCardiacaBpm = dto.FrecuenciaCardiacaBpm,
                SaturacionOxigenoSpO2 = dto.SaturacionOxigenoSpO2,
                Timestamp = DateTime.SpecifyKind(dto.Timestamp, DateTimeKind.Utc),
                FechaRegistro = DateTime.UtcNow
            };

            await _context.MedicionesFrecuenciaCardiaca.InsertOneAsync(nuevaMedicion);

            // 3. Opcional: Actualizar el snapshot "UltimaSincronizacion" y estado del dispositivo
            var updateDispositivo = Builders<Dispositivo>.Update
                .Set(d => d.UltimaSincronizacion, DateTime.UtcNow)
                .Set(d => d.EstadoConexion, "Online");

            await _context.Dispositivos.UpdateOneAsync(filtroDispositivo, updateDispositivo);

            return nuevaMedicion.Id!;
        }

        public async Task<List<MedicionResponseDto>> ObtenerHistorialPacienteAsync(string idPaciente, DateTime? desde, DateTime? hasta, int limite)
        {
            if (!ObjectId.TryParse(idPaciente, out _))
            {
                throw new ArgumentException("El ID del paciente no tiene un formato válido.");
            }

            var builder = Builders<MedicionFrecuenciaCardiaca>.Filter;
            var filtro = builder.Eq(m => m.IdPaciente, idPaciente);

            if (desde.HasValue)
            {
                filtro &= builder.Gte(m => m.Timestamp, DateTime.SpecifyKind(desde.Value, DateTimeKind.Utc));
            }

            if (hasta.HasValue)
            {
                filtro &= builder.Lte(m => m.Timestamp, DateTime.SpecifyKind(hasta.Value, DateTimeKind.Utc));
            }

            var limiteFinal = limite <= 0 ? 100 : Math.Min(limite, 500);

            var mediciones = await _context.MedicionesFrecuenciaCardiaca
                .Find(filtro)
                .SortByDescending(m => m.Timestamp)
                .Limit(limiteFinal)
                .ToListAsync();

            return mediciones.Select(m => new MedicionResponseDto
            {
                IdMedicion = m.Id!,
                FrecuenciaCardiacaBpm = m.FrecuenciaCardiacaBpm,
                SaturacionOxigenoSpO2 = m.SaturacionOxigenoSpO2,
                Timestamp = m.Timestamp
            }).ToList();
        }
    }
}