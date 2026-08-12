using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BeatWatch_BackEnd.Models
{
    [BsonIgnoreExtraElements]
    public class EstadisticasDiarias
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("Fecha")]
        public string Fecha { get; set; } = null!;

        [BsonElement("IdPaciente")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string IdPaciente { get; set; } = null!;

        [BsonElement("AlertasCriticas")]
        public int AlertasCriticas { get; set; }

        [BsonElement("DistanciaTotalKm")]
        public double DistanciaTotalKm { get; set; }

        [BsonElement("DuracionTotalEpisodios")]
        public int DuracionTotalEpisodios { get; set; }

        [BsonElement("FechaActualizacion")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime FechaActualizacion { get; set; }

        [BsonElement("FechaCreacion")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime FechaCreacion { get; set; }

        [BsonElement("FrecuenciaMaxima")]
        public double? FrecuenciaMaxima { get; set; }

        [BsonElement("FrecuenciaMinima")]
        public double? FrecuenciaMinima { get; set; }

        [BsonElement("FrecuenciaPromedio")]
        public double? FrecuenciaPromedio { get; set; }

        [BsonElement("HorasSueno")]
        public double? HorasSueno { get; set; }

        [BsonElement("TotalArritmias")]
        public int TotalArritmias { get; set; }

        [BsonElement("TotalCalorias")]
        public double TotalCalorias { get; set; }

        [BsonElement("TotalEpisodios")]
        public int TotalEpisodios { get; set; }

        [BsonElement("TotalPasos")]
        public int TotalPasos { get; set; }
    }
}