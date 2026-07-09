using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BeatWatch_BackEnd.Models
{
    public class Paciente
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("CURP")]
        public string CURP { get; set; } = null!;

        [BsonElement("Edad")]
        public int Edad { get; set; }

        [BsonElement("Sexo")]
        public string Sexo { get; set; } = null!;

        [BsonElement("Peso")]
        public double Peso { get; set; }

        [BsonElement("Estatura")]
        public double Estatura { get; set; }

        [BsonElement("TipoSangre")]
        public string TipoSangre { get; set; } = null!;

        [BsonElement("IdLicencia")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? IdLicencia { get; set; }

        [BsonElement("Fotografia")]
        public byte[]? Fotografia { get; set; }

        [BsonElement("TokenWeb")]
        public string? TokenWeb { get; set; }

        [BsonElement("TokenMovil")]
        public string? TokenMovil { get; set; }
    }
}
