using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BeatWatch_BackEnd.Models
{
    public class Dispositivo
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("NumeroSerie")]
        public string NumeroSerie { get; set; } = null!;

        [BsonElement("Alias")]
        public string Alias { get; set; } = null!;

        [BsonElement("IdPaciente")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? IdPaciente { get; set; }
    }
}
