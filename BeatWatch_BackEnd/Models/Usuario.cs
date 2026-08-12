using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

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
        [JsonIgnore]
        public string Contrasena { get; set; } = null!;

        [BsonElement("RestablecimientoContrasenaTokenHash")]
        [JsonIgnore]
        public string? RestablecimientoContrasenaTokenHash { get; set; }

        [BsonElement("RestablecimientoContrasenaExpiraEn")]
        [JsonIgnore]
        public DateTime? RestablecimientoContrasenaExpiraEn { get; set; }

        [BsonElement("Activo")]
        public bool Activo { get; set; } = true;

        [BsonElement("Cuidadores")]
        [BsonRepresentation(BsonType.ObjectId)]
        public List<string> Cuidadores { get; set; } = new();
        [JsonIgnore]
        public string? TokenMovil { get; set; }
        [BsonElement("Rol")]
        public string Rol { get; set; } = "Paciente";

        [BsonElement("FechaCreacion")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime FechaCreacion { get; set; }

        [BsonElement("IdLicencia")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? IdLicencia { get; set; }


        [BsonElement("SesionActiva")]
        [JsonIgnore]
        public bool SesionActiva { get; set; } = false;

        [BsonElement("UltimaSesionId")]
        [JsonIgnore]
        public string? UltimaSesionId { get; set; }

        [BsonElement("UltimoAcceso")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? UltimoAcceso { get; set; }
    }
}
