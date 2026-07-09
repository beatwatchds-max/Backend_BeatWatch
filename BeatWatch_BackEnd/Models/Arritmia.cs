using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BeatWatch_BackEnd.Models
{
    public class Arritmia
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("Tipo")]
        public string Tipo { get; set; } = null!;

        [BsonElement("FrecuenciaCardiaca")]
        public int FrecuenciaCardiaca { get; set; }

        [BsonElement("DuracionEpisodioSeconds")]
        public int DuracionEpisodioSeconds { get; set; }

        [BsonElement("IdPaciente")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string IdPaciente { get; set; } = null!;

        [BsonElement("Sintomas")]
        public Sintomas Sintomas { get; set; } = new();

        [BsonElement("Fecha")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime Fecha { get; set; } = DateTime.UtcNow;
    }
}
