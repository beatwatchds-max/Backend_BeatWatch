using System.Text.Json.Serialization;

namespace BeatWatch_BackEnd.Dtos
{
    public class GraficaSeriesColumnarResponseDto
    {
        [JsonPropertyName("IdPaciente")]
        public string IdPaciente { get; set; } = null!;

        [JsonPropertyName("series")]
        public Dictionary<string, object> Series { get; set; } = new();
    }
}