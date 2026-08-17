using BeatWatch_BackEnd.Data;
using BeatWatch_BackEnd.Dtos;
using BeatWatch_BackEnd.infrescture;
using BeatWatch_BackEnd.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace BeatWatch_BackEnd.Services
{
    public class AlertaService : IAlertaService
    {
        private readonly MongoDbContext _context;
        private readonly ILogger<AlertaService> _logger;
        private readonly IFcmNotificationService _fcmNotificationService;

        public AlertaService(MongoDbContext context, ILogger<AlertaService> logger, IFcmNotificationService fcmNotificationService)
        {
            _context = context;
            _logger = logger;
            _fcmNotificationService = fcmNotificationService;
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

            // 3. La alerta ya es durable; un fallo de push no debe deshacer su registro.
            await EnviarNotificacionPushAsync(dispositivo.IdPaciente, nuevaAlerta.Tipo, nuevaAlerta.Mensaje);

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
                var paciente = await _context.Pacientes.Find(p => p.Id == idPaciente).FirstOrDefaultAsync();
                if (paciente is null) return;

                var usuarioPaciente = await _context.Usuarios.Find(u => u.Id == paciente.UsuarioId).FirstOrDefaultAsync();
                if (usuarioPaciente is null) return;

                var destinatarios = new[] { usuarioPaciente.Id! }.Concat(usuarioPaciente.Cuidadores).Distinct();
                var usuarios = await _context.Usuarios.Find(u => destinatarios.Contains(u.Id!)).ToListAsync();
                var tokens = usuarios.SelectMany(u => u.TokensFcm).Where(t => !string.IsNullOrWhiteSpace(t)).Distinct();

                foreach (var token in tokens)
                {
                    try
                    {
                        var idMensaje = await _fcmNotificationService.EnviarAsync(token, $"Alerta {tipo}", mensaje);
                        _logger.LogInformation("Notificación Push [FCM] confirmada para alerta de dispositivo. IdMensaje: {IdMensaje}", idMensaje);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error al enviar la notificación Push para una alerta de dispositivo.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar la notificación Push para una alerta de dispositivo.");
            }
        }
    }
}
