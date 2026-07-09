using MongoDB.Bson.Serialization.Attributes;

namespace BeatWatch_BackEnd.Models
{
    public class Sintomas
    {
        [BsonElement("Mareo")]
        public bool Mareo { get; set; }

        [BsonElement("Palpitaciones")]
        public bool Palpitaciones { get; set; }

        [BsonElement("DolorPecho")]
        public bool DolorPecho { get; set; }

        [BsonElement("Desmayo")]
        public bool Desmayo { get; set; }

        [BsonElement("FaltaAire")]
        public bool FaltaAire { get; set; }

        [BsonElement("Fatiga")]
        public bool Fatiga { get; set; }
    }
}
