using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BeatWatch_BackEnd.Models
{
    public class Licencia
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("UsuarioId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string UsuarioId { get; set; } = null!;
        [BsonElement("UsuariosAsociados")]
        [BsonRepresentation(BsonType.ObjectId)]
        public List<string> UsuariosAsociados { get; set; } = new List<string>();
        [BsonElement("Tipo")]
        public string Tipo { get; set; } = null!;

        [BsonElement("CodigoGrupo")]
        public string CodigoGrupo { get; set; } = null!;

        [BsonElement("FechaInicio")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime FechaInicio { get; set; }

        [BsonElement("FechaFin")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime FechaFin { get; set; }
        public bool Activa { get; set; }

        // NUEVOS CAMPOS PARA LA SIMULACIÓN DE LA MAQUETA
        public string MetodoPago { get; set; } = null!; // 'Tarjeta', 'PayPal', 'OXXO'
        public string EstadoPago { get; set; } = null!; // 'Aprobado' o 'Pendiente'
    }
}
