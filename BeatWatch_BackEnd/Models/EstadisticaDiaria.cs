using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BeatWatch_BackEnd.Models
{
    [BsonIgnoreExtraElements]
    public class EstadisticaDiaria
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("IdPaciente")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string IdPaciente { get; set; } = null!;

        [BsonElement("Fecha")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime Fecha { get; set; }

        [BsonElement("FrecuenciaCardiaca")]
        public MetricasFrecuenciaCardiaca FrecuenciaCardiaca { get; set; } = new();

        [BsonElement("Arritmias")]
        public MetricasArritmias Arritmias { get; set; } = new();

        [BsonElement("Actividad")]
        public MetricasActividad Actividad { get; set; } = new();

        [BsonElement("UpdatedAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime UpdatedAt { get; set; }
    }

    public class MetricasFrecuenciaCardiaca
    {
        [BsonElement("Promedio")]
        public double Promedio { get; set; }

        [BsonElement("Minimo")]
        public int Minimo { get; set; }

        [BsonElement("Maximo")]
        public int Maximo { get; set; }

        [BsonElement("Mediana")]
        public double Mediana { get; set; }

        [BsonElement("Lecturas")]
        public int Lecturas { get; set; }
    }

    public class MetricasArritmias
    {
        [BsonElement("Total")]
        public int Total { get; set; }

        [BsonElement("Criticas")]
        public int Criticas { get; set; }

        [BsonElement("DuracionTotalSeconds")]
        public int DuracionTotalSeconds { get; set; }

        [BsonElement("DuracionPromedioSeconds")]
        public double DuracionPromedioSeconds { get; set; }
    }

    public class MetricasActividad
    {
        [BsonElement("Pasos")]
        public int Pasos { get; set; }

        [BsonElement("Calorias")]
        public double Calorias { get; set; }

        [BsonElement("DistanciaKm")]
        public double DistanciaKm { get; set; }

        [BsonElement("HorasSueno")]
        public double HorasSueno { get; set; }
    }
}