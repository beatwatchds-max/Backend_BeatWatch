using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BeatWatch_BackEnd.Models;

public class EpisodioArritmia
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public string IdPaciente { get; set; } = null!;

    [BsonElement("TipoAnomalia")]
    public string TipoAnomalia { get; set; } = null!; // Ej: "Taquicardia en Reposo", "Fibrilación"

    [BsonElement("FrecuenciaCardiaca")]
    public int FrecuenciaCardiaca { get; set; } // Ej: 135 bpm

    [BsonElement("DuracionEpisodioSeconds")]
    public int DuracionEpisodioSeconds { get; set; }

    [BsonElement("EsAlertaCritica")]
    public bool EsAlertaCritica { get; set; } = true;

    [BsonElement("Fecha")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
}