using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BeatWatch_BackEnd.Models
{
    public class MedicionFrecuenciaCardiaca
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

        [BsonElement("FrecuenciaCardiacaBpm")]
        public int FrecuenciaCardiacaBpm { get; set; }

        [BsonElement("SaturacionOxigenoSpO2")]
        public int? SaturacionOxigenoSpO2 { get; set; }

        [BsonElement("Timestamp")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime Timestamp { get; set; }

        [BsonElement("FechaRegistro")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
    }
}