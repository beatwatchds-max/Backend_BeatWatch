using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BeatWatch_BackEnd.Models
{
    public class AlertaDispositivo
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("IdDispositivo")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string IdDispositivo { get; set; } = null!;

        [BsonElement("CodigoDispositivo")]
        public string CodigoDispositivo { get; set; } = null!;

        [BsonElement("IdPaciente")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string IdPaciente { get; set; } = null!;

        [BsonElement("Tipo")]
        public string Tipo { get; set; } = null!; // PULSO_ANORMAL o TIEMPO_EXCEDIDO

        [BsonElement("ValorDetectado")]
        public double ValorDetectado { get; set; }

        [BsonElement("Mensaje")]
        public string Mensaje { get; set; } = null!;

        [BsonElement("Timestamp")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime Timestamp { get; set; }

        [BsonElement("FechaRegistro")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
    }
}