using BeatWatch_BackEnd.Configuration;
using BeatWatch_BackEnd.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace BeatWatch_BackEnd.Data
{
    public class MongoDbContext
    {
        private readonly IMongoDatabase _database;

        public MongoDbContext(IOptions<MongoDbSettings> settings)
        {
            var client = new MongoClient(settings.Value.ConnectionString);
            _database = client.GetDatabase(settings.Value.DatabaseName);
        }

        public IMongoCollection<Usuario> Usuarios => _database.GetCollection<Usuario>("Usuarios");
        public IMongoCollection<Licencia> Licencias => _database.GetCollection<Licencia>("Licencias");
        public IMongoCollection<Paciente> Pacientes => _database.GetCollection<Paciente>("Pacientes");
        public IMongoCollection<Arritmia> Arritmias => _database.GetCollection<Arritmia>("Arritmias");
        public IMongoCollection<Dispositivo> Dispositivos => _database.GetCollection<Dispositivo>("Dispositivos");
    }
}
