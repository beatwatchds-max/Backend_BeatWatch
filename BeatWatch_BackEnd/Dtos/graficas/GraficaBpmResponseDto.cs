using System.Text.Json.Serialization;

namespace BeatWatch_BackEnd.Dtos.graficas
{
    public class PuntoBpmDto
    {
        [JsonPropertyName("fecha")]
        public string Fecha { get; set; } = null!;

        [JsonPropertyName("promedio")]
        public double Promedio { get; set; }

        [JsonPropertyName("minimo")]
        public int Minimo { get; set; }

        [JsonPropertyName("maximo")]
        public int Maximo { get; set; }
    }

    public class GraficaBpmResponseDto
    {
        [JsonPropertyName("IdPaciente")]
        public string IdPaciente { get; set; } = null!;

        [JsonPropertyName("puntos")]
        public List<PuntoBpmDto> Puntos { get; set; } = new();
    }
}