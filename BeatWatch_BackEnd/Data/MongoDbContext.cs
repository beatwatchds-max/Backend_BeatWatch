using BeatWatch_BackEnd.Configuration;
using BeatWatch_BackEnd.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace BeatWatch_BackEnd.Data
{
    public class MongoDbContext
    {
        private readonly IMongoDatabase _database = null!;

        // Parameterless constructor for mocking purposes
    public MongoDbContext() {
            _database = null;
        }
    public MongoDbContext(IOptions<MongoDbSettings> settings)
        {
            var client = new MongoClient(settings.Value.ConnectionString);
            _database = client.GetDatabase(settings.Value.DatabaseName);
        }

        public virtual IMongoCollection<Usuario> Usuarios => _database.GetCollection<Usuario>("Usuarios");
        public virtual IMongoCollection<Licencia> Licencias => _database.GetCollection<Licencia>("Licencias");
        public virtual IMongoCollection<Paciente> Pacientes => _database.GetCollection<Paciente>("Pacientes");
        public virtual IMongoCollection<Arritmia> Arritmias => _database.GetCollection<Arritmia>("Arritmias");
        public virtual IMongoCollection<Dispositivo> Dispositivos => _database.GetCollection<Dispositivo>("Dispositivos");
    }
}
