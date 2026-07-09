using BeatWatch_BackEnd.Data;
using BeatWatch_BackEnd.Models;
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

                _logger.LogInformation("MongoDB collections and unique indexes initialized successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while initializing MongoDB collections and indexes.");
                throw;
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
