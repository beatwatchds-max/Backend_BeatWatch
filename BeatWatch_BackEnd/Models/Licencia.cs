using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BeatWatch_BackEnd.Models
{
    public class Licencia
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("Tipo")]
        public string Tipo { get; set; } = null!;

        [BsonElement("CodigoGrupo")]
        public string CodigoGrupo { get; set; } = null!;

        [BsonElement("FechaInicio")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime FechaInicio { get; set; }

        [BsonElement("FechaFin")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime FechaFin { get; set; }
    }
}
