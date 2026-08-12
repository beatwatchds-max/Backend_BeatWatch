using BeatWatch_BackEnd.Data;
using BeatWatch_BackEnd.Dtos;
using BeatWatch_BackEnd.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace BeatWatch_BackEnd.Services
{
    public class AlertaService : IAlertaService
    {
        private readonly MongoDbContext _context;
        private readonly ILogger<AlertaService> _logger;

        public AlertaService(MongoDbContext context, ILogger<AlertaService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<AlertaResponseDto> RegistrarAlertaAsync(string idDispositivoIdentificador, CrearAlertaDto dto)
        {
            // 1. Resolver el dispositivo por Mongo _id o por CodigoDispositivo
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

            // 2. Mapear y guardar el documento de alerta en MongoDB
            var nuevaAlerta = new AlertaDispositivo
            {
                IdDispositivo = dispositivo.Id!,
                CodigoDispositivo = dispositivo.CodigoDispositivo,
                IdPaciente = dispositivo.IdPaciente,
                Tipo = dto.Tipo.Trim().ToUpperInvariant(),
                ValorDetectado = dto.ValorDetectado,
                Mensaje = dto.Mensaje,
                Timestamp = DateTime.SpecifyKind(dto.Timestamp, DateTimeKind.Utc),
                FechaRegistro = DateTime.UtcNow
            };

            await _context.AlertasDispositivos.InsertOneAsync(nuevaAlerta);

            // 3. Disparar Notificación Push (FCM / Firebase) en segundo plano
            _ = EnviarNotificacionPushAsync(dispositivo.IdPaciente, dto.Tipo, dto.Mensaje);

            return new AlertaResponseDto
            {
                IdAlerta = nuevaAlerta.Id,
                Tipo = nuevaAlerta.Tipo,
                ValorDetectado = nuevaAlerta.ValorDetectado,
                Mensaje = nuevaAlerta.Mensaje,
                Timestamp = nuevaAlerta.Timestamp
            };
        }

        private async Task EnviarNotificacionPushAsync(string idPaciente, string tipo, string mensaje)
        {
            try
            {
                // Aquí va la llamada a Firebase Messaging (FCM) hacia los tokens registrados de los cuidadores/paciente
                _logger.LogInformation("Notificación Push [FCM] enviada para el paciente {IdPaciente}: [{Tipo}] {Mensaje}", idPaciente, tipo, mensaje);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar la notificación Push para la alerta del paciente {IdPaciente}", idPaciente);
            }
        }
    }
}