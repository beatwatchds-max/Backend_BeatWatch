using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace BeatWatch_BackEnd.Models
{
    public class Dispositivo
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("NumeroSerie")]
        public string NumeroSerie { get; set; } = string.Empty;

        [BsonElement("Alias")]
        public string Alias { get; set; } = string.Empty; // Ej. "Apple Watch Series 9"

        [BsonElement("CodigoModelo")]
        public string CodigoModelo { get; set; } = string.Empty; // Ej. "A2982"

        [BsonElement("CodigoDispositivo")]
        public string CodigoDispositivo { get; set; } = string.Empty; // Ej. "DEV-001"

        [BsonElement("TipoDispositivo")]
        public string TipoDispositivo { get; set; } = "Wearable"; // "Wearable" o "Smartphone"

        [BsonElement("AsignadoA")]
        public string AsignadoA { get; set; } = "Paciente"; // Ej. "Paciente", "Cuidador"

        [BsonElement("IdPaciente")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string IdPaciente { get; set; } = string.Empty;

        [BsonElement("EstadoConexion")]
        public string EstadoConexion { get; set; } = "Online"; // "Online" o "Offline"

        [BsonElement("Bateria")]
        public int Bateria { get; set; } = 100; // Porcentaje 0 - 100

        [BsonElement("SistemaOperativo")]
        public string SistemaOperativo { get; set; } = string.Empty; // Ej. "watchOS 11.2", "iOS 18.1.1"

        [BsonElement("UltimaSincronizacion")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime UltimaSincronizacion { get; set; } = DateTime.UtcNow;
        [BsonElement("FechaRegistro")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

        // Métricas dinámicas opcionales
        [BsonElement("MetricasWearable")]
        public MetricasWearableDto? MetricasWearable { get; set; }

        [BsonElement("MetricasSmartphone")]
        public MetricasSmartphoneDto? MetricasSmartphone { get; set; }

        [BsonElement("Activo")]
        public bool Activo { get; set; } = true;

        [BsonElement("MedicionSolicitadaEn")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? MedicionSolicitadaEn { get; set; }

        [BsonElement("WatchAccessToken")]
        [JsonIgnore]
        public string? WatchAccessToken { get; set; }

        [BsonElement("WatchAccessTokenExpiraEn")]
        [JsonIgnore]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? WatchAccessTokenExpiraEn { get; set; }
    }

    public class MetricasWearableDto
    {
        public int FrecuenciaCardiacaBpm { get; set; }
        public int SaturacionOxigenoSpO2 { get; set; }
        public int Pasos { get; set; }
    }

    public class MetricasSmartphoneDto
    {
        public string VersionApp { get; set; } = "v1.0";
        public string EstadoNotificaciones { get; set; } = "Activas";
        public string EstadoGps { get; set; } = "Activo";
    }
}
