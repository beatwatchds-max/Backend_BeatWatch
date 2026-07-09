using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BeatWatch_BackEnd.Models
{
    public class Usuario
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("Nombre")]
        public string Nombre { get; set; } = null!;

        [BsonElement("Correo")]
        public string Correo { get; set; } = null!;

        [BsonElement("Telefono")]
        public string Telefono { get; set; } = null!;

        [BsonElement("Contrasena")]
        public string Contrasena { get; set; } = null!;

        [BsonElement("Activo")]
        public bool Activo { get; set; } = true;

        [BsonElement("Cuidadores")]
        [BsonRepresentation(BsonType.ObjectId)]
        public List<string> Cuidadores { get; set; } = new();
    }
}
