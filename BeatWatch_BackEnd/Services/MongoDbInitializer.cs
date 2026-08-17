using BeatWatch_BackEnd.Data;
using BeatWatch_BackEnd.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace BeatWatch_BackEnd.Services
{
    public class MongoDbInitializer : IHostedService
    {
        private readonly MongoDbContext _context;
        private readonly ILogger<MongoDbInitializer> _logger;

        public MongoDbInitializer(MongoDbContext context, ILogger<MongoDbInitializer> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Initializing MongoDB collections and unique indexes...");
            try
            {
                // Unique index for Usuarios (Correo)
                var usuarioIndexKeys = Builders<Usuario>.IndexKeys.Ascending(u => u.Correo);
                var usuarioIndexOptions = new CreateIndexOptions { Unique = true };
                await _context.Usuarios.Indexes.CreateOneAsync(
                    new CreateIndexModel<Usuario>(usuarioIndexKeys, usuarioIndexOptions),
                    cancellationToken: cancellationToken);
                _logger.LogInformation("Unique index created/verified on Usuarios (Correo).");

                // Unique index for Pacientes (CURP)
                var pacienteIndexKeys = Builders<Paciente>.IndexKeys.Ascending(p => p.CURP);
                var pacienteIndexOptions = new CreateIndexOptions { Unique = true };
                await _context.Pacientes.Indexes.CreateOneAsync(
                    new CreateIndexModel<Paciente>(pacienteIndexKeys, pacienteIndexOptions),
                    cancellationToken: cancellationToken);
                _logger.LogInformation("Unique index created/verified on Pacientes (CURP).");

                // Unique index for Licencias (CodigoGrupo)
                var licenciaIndexKeys = Builders<Licencia>.IndexKeys.Ascending(l => l.CodigoGrupo);
                var licenciaIndexOptions = new CreateIndexOptions { Unique = true };
                await _context.Licencias.Indexes.CreateOneAsync(
                    new CreateIndexModel<Licencia>(licenciaIndexKeys, licenciaIndexOptions),
                    cancellationToken: cancellationToken);
                _logger.LogInformation("Unique index created/verified on Licencias (CodigoGrupo).");

                // Unique index for Dispositivos (NumeroSerie)
                var dispositivoIndexKeys = Builders<Dispositivo>.IndexKeys.Ascending(d => d.NumeroSerie);
                var dispositivoIndexOptions = new CreateIndexOptions { Unique = true };
                await _context.Dispositivos.Indexes.CreateOneAsync(
                    new CreateIndexModel<Dispositivo>(dispositivoIndexKeys, dispositivoIndexOptions),
                    cancellationToken: cancellationToken);
                _logger.LogInformation("Unique index created/verified on Dispositivos (NumeroSerie).");

                await ResolverTokensFcmDuplicadosAsync(cancellationToken);
                var fcmTokenKeys = Builders<Usuario>.IndexKeys.Ascending(u => u.FcmToken);
                var fcmTokenOptions = new CreateIndexOptions<Usuario>
                {
                    Unique = true,
                    Name = "ux_FcmToken",
                    PartialFilterExpression = new BsonDocument("FcmToken", new BsonDocument
                    {
                        { "$type", "string" },
                        { "$gt", string.Empty }
                    })
                };
                await _context.Usuarios.Indexes.CreateOneAsync(
                    new CreateIndexModel<Usuario>(fcmTokenKeys, fcmTokenOptions),
                    cancellationToken: cancellationToken);
                await VerificarIndiceFcmAsync(cancellationToken);
                _logger.LogInformation("Unique partial index created/verified on Usuarios (FcmToken).");

                // Pairing sessions are valid for minutes; remove plaintext legacy secrets once expired.
                var sesionesExpiradas = Builders<SesionEmparejamiento>.Filter.Lt(s => s.FechaExpiracion, DateTime.UtcNow);
                var limpiarSecretos = Builders<SesionEmparejamiento>.Update
                    .Unset(s => s.WatchSecret)
                    .Unset(s => s.AccessToken)
                    .Unset(s => s.RefreshToken);
                await _context.SesionesEmparejamiento.UpdateManyAsync(sesionesExpiradas, limpiarSecretos, cancellationToken: cancellationToken);

                _logger.LogInformation("MongoDB collections and unique indexes initialized successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while initializing MongoDB collections and indexes.");
                throw;
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private async Task ResolverTokensFcmDuplicadosAsync(CancellationToken cancellationToken)
        {
            var usuarios = await _context.Usuarios
                .Find(u => u.FcmToken != null && u.FcmToken != string.Empty)
                .ToListAsync(cancellationToken);
            var duplicados = usuarios
                .GroupBy(u => u.FcmToken!, StringComparer.Ordinal)
                .Where(g => g.Count() > 1);

            foreach (var grupo in duplicados)
            {
                var conservar = grupo
                    .OrderByDescending(u => u.FcmTokenActualizadoEn ?? DateTime.MinValue)
                    .ThenByDescending(u => u.Id, StringComparer.Ordinal)
                    .First();
                var limpiar = Builders<Usuario>.Update
                    .Set(u => u.FcmToken, null)
                    .Set(u => u.FcmDeviceId, null)
                    .Set(u => u.FcmTokenActualizadoEn, null);
                await _context.Usuarios.UpdateManyAsync(
                    u => u.FcmToken == grupo.Key && u.Id != conservar.Id,
                    limpiar,
                    cancellationToken: cancellationToken);
                _logger.LogWarning("Se resolvió un token FCM duplicado conservando el registro actualizado más recientemente.");
            }
        }

        private async Task VerificarIndiceFcmAsync(CancellationToken cancellationToken)
        {
            using var cursor = await _context.Usuarios.Indexes.ListAsync(cancellationToken);
            var indices = await cursor.ToListAsync(cancellationToken);
            var indice = indices.FirstOrDefault(i => i.GetValue("name", string.Empty) == "ux_FcmToken");
            var claveCorrecta = indice?["key"]?.AsBsonDocument.TryGetValue("FcmToken", out var orden) == true && orden == 1;
            var filtroToken = indice?["partialFilterExpression"]?.AsBsonDocument
                .GetValue("FcmToken", BsonNull.Value).AsBsonDocument;
            var filtroCorrecto = filtroToken?.GetValue("$type", BsonNull.Value) == "string"
                && filtroToken.GetValue("$gt", BsonNull.Value) == string.Empty;
            if (indice is null || !indice.GetValue("unique", false).ToBoolean() || !claveCorrecta || !filtroCorrecto)
            {
                throw new InvalidOperationException("No se pudo verificar el índice único ux_FcmToken.");
            }
        }
    }
}
