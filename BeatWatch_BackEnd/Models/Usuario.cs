using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

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
        public string Contrasena { get; set; } = null!;

        [BsonElement("RestablecimientoContrasenaTokenHash")]
        public string? RestablecimientoContrasenaTokenHash { get; set; }

        [BsonElement("RestablecimientoContrasenaExpiraEn")]
        public DateTime? RestablecimientoContrasenaExpiraEn { get; set; }

        [BsonElement("Activo")]
        public bool Activo { get; set; } = true;

        public string? EmpresaOrganizacion { get; set; }
        public string? RFC { get; set; }
        public string? Direccion { get; set; }
        public string? CiudadEstado { get; set; }

        [BsonElement("Cuidadores")]
        [BsonRepresentation(BsonType.ObjectId)]
        public List<string> Cuidadores { get; set; } = new();
        public string? TokenMovil { get; set; }
        [BsonElement("Rol")]
        public string Rol { get; set; } = "Paciente";

        [BsonElement("FechaCreacion")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime FechaCreacion { get; set; }

        [BsonElement("IdLicencia")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? IdLicencia { get; set; }
    }
}
