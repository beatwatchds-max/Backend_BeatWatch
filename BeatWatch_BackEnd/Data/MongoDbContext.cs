using BeatWatch_BackEnd.Configuration;
using BeatWatch_BackEnd.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace BeatWatch_BackEnd.Data
{
    public class MongoDbContext
    {
        private readonly IMongoDatabase _database = null!;

    
        public MongoDbContext()
        {
        }

        public MongoDbContext(IOptions<MongoDbSettings> settings)
        {
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
        public virtual IMongoCollection<EstadisticaDiaria> EstadisticasDiarias => _database.GetCollection<EstadisticaDiaria>("EstadisticaDiaria");
        public virtual IMongoCollection<MedicionFrecuenciaCardiaca> MedicionesFrecuenciaCardiaca => _database.GetCollection<MedicionFrecuenciaCardiaca>("MedicionesFrecuenciaCardiaca");

        private void CrearIndices()
        {
            try
            {
         
                var indexKeys = Builders<EstadisticaDiaria>.IndexKeys
                    .Ascending(e => e.IdPaciente)
                    .Ascending(e => e.Fecha);

                var indexOptions = new CreateIndexOptions
                {
                    Unique = true,
                    Name = "ux_IdPaciente_Fecha"
                };

                var indexModel = new CreateIndexModel<EstadisticaDiaria>(indexKeys, indexOptions);

            
                EstadisticasDiarias.Indexes.CreateOne(indexModel);
            }
            catch (MongoCommandException ex)
            {
                // Loggear advertencia (por ejemplo con ILogger o Console)
                Console.WriteLine($"Nota de Índice: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error general al crear índices: {ex.Message}");
            }
        }
    }
}