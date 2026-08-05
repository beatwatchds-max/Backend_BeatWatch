using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BeatWatch_BackEnd.Models
{
    public class SesionEmparejamiento
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("IdSesion")]
        public string IdSesion { get; set; } = string.Empty; // GUID

        [BsonElement("TokenEmparejamiento")]
        public string TokenEmparejamiento { get; set; } = string.Empty; // Token temporal leído en QR

        [BsonElement("WatchSecret")]
        public string WatchSecret { get; set; } = string.Empty; // Secreto para la cabecera X-Watch-Secret

        [BsonElement("Estado")]
        public string Estado { get; set; } = "PENDIENTE"; // PENDIENTE, EMPAREJADO, EXPIRADO, CANCELADO

        // Datos del Reloj
        public string NumeroSerie { get; set; } = string.Empty;
        public string Alias { get; set; } = string.Empty;
        public string TipoDispositivo { get; set; } = string.Empty;
        public string CodigoModelo { get; set; } = string.Empty;
        public string CodigoDispositivo { get; set; } = string.Empty;
        public string SistemaOperativo { get; set; } = string.Empty;
        public string VersionAplicacion { get; set; } = string.Empty;

        // Datos tras emparejar
        public string? IdDispositivo { get; set; }
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }

        [BsonElement("FechaCreacion")]
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        [BsonElement("FechaExpiracion")]
        public DateTime FechaExpiracion { get; set; }
    }
}