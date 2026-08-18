using BeatWatch_BackEnd.Data;
using BeatWatch_BackEnd.Dtos;
using BeatWatch_BackEnd.infrescture;
using BeatWatch_BackEnd.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Globalization;

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
            await EnviarNotificacionPushAsync(nuevaAlerta);

            return new AlertaResponseDto
            {
                IdAlerta = nuevaAlerta.Id,
                Tipo = nuevaAlerta.Tipo,
                ValorDetectado = nuevaAlerta.ValorDetectado,
                Mensaje = nuevaAlerta.Mensaje,
                Timestamp = nuevaAlerta.Timestamp
            };
        }

        private async Task EnviarNotificacionPushAsync(AlertaDispositivo alerta)
        {
            try
            {
                // 1. Obtener al paciente y su usuario
                var paciente = await _context.Pacientes.Find(p => p.Id == alerta.IdPaciente).FirstOrDefaultAsync();
                if (paciente is null) return;

                var usuarioPaciente = await _context.Usuarios.Find(u => u.Id == paciente.UsuarioId).FirstOrDefaultAsync();

                // 2. Recolectar tokens de destino
                var tokensDestino = new List<string>();

                // Token del paciente (si tiene)
                if (!string.IsNullOrWhiteSpace(usuarioPaciente?.FcmToken))
                {
                    tokensDestino.Add(usuarioPaciente.FcmToken);
                }

                // Tokens de los Cuidadores (si el paciente tiene cuidadores asignados)
                if (usuarioPaciente != null && usuarioPaciente.Cuidadores.Any())
                {
                    var cuidadores = await _context.Usuarios
                        .Find(u => usuarioPaciente.Cuidadores.Contains(u.Id))
                        .ToListAsync();

                    var tokensCuidadores = cuidadores
                        .Where(c => !string.IsNullOrWhiteSpace(c.FcmToken))
                        .Select(c => c.FcmToken!);

                    tokensDestino.AddRange(tokensCuidadores);
                }

                // Si nadie tiene un dispositivo móvil vinculado, salimos
                if (!tokensDestino.Any()) return;

                var titulo = $"Alerta {alerta.Tipo}";
                var datos = CrearDatosFcm(alerta, titulo);

                // 3. Enviar la notificación a todos los dispositivos en paralelo
                foreach (var token in tokensDestino.Distinct())
                {
                    try
                    {
                        var idMensaje = await _fcmNotificationService.EnviarAsync(token, titulo, alerta.Mensaje, datos);
                        _logger.LogInformation("Notificación Push [FCM] enviada exitosamente. IdMensaje: {IdMensaje}", idMensaje);
                    }
                    catch (FcmTokenInvalidoException)
                    {
                        // Limpiar token inválido
                        var limpiarToken = Builders<Usuario>.Update
                            .Set(u => u.FcmToken, null)
                            .Set(u => u.FcmDeviceId, null)
                            .Set(u => u.FcmTokenActualizadoEn, null);

                        await _context.Usuarios.UpdateOneAsync(u => u.FcmToken == token, limpiarToken);
                        _logger.LogWarning("Firebase rechazó un token FCM; registro eliminado de MongoDB.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error técnico al procesar el envío de notificaciones Push para alerta.");
            }
        }

        internal static IReadOnlyDictionary<string, string> CrearDatosFcm(AlertaDispositivo alerta, string titulo) => new Dictionary<string, string>
        {
            ["title"] = titulo,
            ["body"] = alerta.Mensaje,
            ["alertId"] = alerta.Id ?? string.Empty,
            ["tipo"] = alerta.Tipo,
            ["valorDetectado"] = alerta.ValorDetectado.ToString(CultureInfo.InvariantCulture),
            ["pacienteId"] = alerta.IdPaciente,
            ["timestamp"] = alerta.Timestamp.ToString("O", CultureInfo.InvariantCulture)
        };
    }
}
