using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BeatWatch_BackEnd.Models;

public class ActividadDiaria
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public string IdPaciente { get; set; } = null!;

    [BsonElement("Pasos")]
    public int Pasos { get; set; }

    [BsonElement("Calorias")]
    public double Calorias { get; set; }

    [BsonElement("DistanciaKm")]
    public double DistanciaKm { get; set; }

    [BsonElement("HorasSueno")]
    public double HorasSueno { get; set; }

    /// <summary>
    /// Identificador único del día en formato YYYY-MM-DD para evitar duplicados en re-sincronizaciones
    /// </summary>
    [BsonElement("FechaCorta")]
    public string FechaCorta { get; set; } = null!;

    [BsonElement("FechaSincronizacion")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime FechaSincronizacion { get; set; } = DateTime.UtcNow;
}