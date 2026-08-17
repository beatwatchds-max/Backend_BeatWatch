using BeatWatch_BackEnd.Configuration;
using BeatWatch_BackEnd.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace BeatWatch_BackEnd.Data
{
    public class MongoDbContext
    {
        private readonly IMongoDatabase _database;
        private readonly ILogger<MongoDbContext> _logger;

        protected MongoDbContext()
        {
            _database = null!;
            _logger = null!;
        }

        public MongoDbContext(IOptions<MongoDbSettings> settings, ILogger<MongoDbContext> logger)
        {
            _logger = logger;
            var client = new MongoClient(settings.Value.ConnectionString);
            _database = client.GetDatabase(settings.Value.DatabaseName);

            CrearIndices();
        }

        public virtual IMongoCollection<Usuario> Usuarios => _database.GetCollection<Usuario>("Usuarios");
        public virtual IMongoCollection<Licencia> Licencias => _database.GetCollection<Licencia>("Licencias");
        public virtual IMongoCollection<Paciente> Pacientes => _database.GetCollection<Paciente>("Pacientes");
        public virtual IMongoCollection<Arritmia> Arritmias => _database.GetCollection<Arritmia>("Arritmias");
        public virtual IMongoCollection<Dispositivo> Dispositivos => _database.GetCollection<Dispositivo>("Dispositivos");
        public virtual IMongoCollection<EpisodioArritmia> EpisodiosArritmia => _database.GetCollection<EpisodioArritmia>("EpisodiosArritmia");
        public virtual IMongoCollection<ActividadDiaria> ActividadesDiarias => _database.GetCollection<ActividadDiaria>("ActividadesDiarias");
        public virtual IMongoCollection<SesionEmparejamiento> SesionesEmparejamiento => _database.GetCollection<SesionEmparejamiento>("SesionesEmparejamiento");
        public virtual IMongoCollection<EstadisticasDiarias> EstadisticasDiarias => _database.GetCollection<EstadisticasDiarias>("EstadisticasDiarias");
        public virtual IMongoCollection<MedicionFrecuenciaCardiaca> MedicionesFrecuenciaCardiaca => _database.GetCollection<MedicionFrecuenciaCardiaca>("MedicionesFrecuenciaCardiaca");
        public virtual IMongoCollection<AlertaDispositivo> AlertasDispositivos =>_database.GetCollection<AlertaDispositivo>("AlertasDispositivos");

        private void CrearIndices()
        {
            try
            {
         
                var indexKeys = Builders<EstadisticasDiarias>.IndexKeys
                    .Ascending(e => e.IdPaciente)
                    .Ascending(e => e.Fecha);

                var indexOptions = new CreateIndexOptions
                {
                    Unique = true,
                    Name = "ux_IdPaciente_Fecha"
                };

                var indexModel = new CreateIndexModel<EstadisticasDiarias>(indexKeys, indexOptions);
                EstadisticasDiarias.Indexes.CreateOne(indexModel);

                var fcmTokenKeys = Builders<Usuario>.IndexKeys.Ascending(u => u.FcmToken);
                var fcmTokenOptions = new CreateIndexOptions<Usuario>
                {
                    Unique = true,
                    Name = "ux_FcmToken",
                    PartialFilterExpression = new BsonDocument("FcmToken", new BsonDocument("$type", "string"))
                };
                Usuarios.Indexes.CreateOne(new CreateIndexModel<Usuario>(fcmTokenKeys, fcmTokenOptions));
            }
            catch (MongoCommandException ex)
            {
                // Loggear advertencia (por ejemplo con ILogger o Console)
                _logger.LogWarning("Nota de Índice: {Message}", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear índices de la base de datos");
            }
        }
    }
}
