using System.Text.Json.Serialization;

namespace BeatWatch_BackEnd.Dtos.graficas
{
    public class GraficaResumenResponseDto
    {
        [JsonPropertyName("IdPaciente")]
        public string IdPaciente { get; set; } = null!;

        [JsonPropertyName("periodo")]
        public string Periodo { get; set; } = null!;

        [JsonPropertyName("promedioBPM")]
        public double PromedioBPM { get; set; }

        [JsonPropertyName("totalPasos")]
        public int TotalPasos { get; set; }

        [JsonPropertyName("totalArritmias")]
        public int TotalArritmias { get; set; }

        [JsonPropertyName("promedioHorasSueno")]
        public double PromedioHorasSueno { get; set; }
    }
}