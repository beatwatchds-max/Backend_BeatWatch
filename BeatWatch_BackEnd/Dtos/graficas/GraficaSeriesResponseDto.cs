using System.Text.Json.Serialization;

namespace BeatWatch_BackEnd.Dtos.graficas
{
    public class PuntoSerieDto
    {
        [JsonPropertyName("fecha")]
        public string Fecha { get; set; } = null!;

        [JsonPropertyName("bpmPromedio")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? BpmPromedio { get; set; }

        [JsonPropertyName("bpmMinimo")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? BpmMinimo { get; set; }

        [JsonPropertyName("bpmMaximo")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? BpmMaximo { get; set; }

        [JsonPropertyName("pasos")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Pasos { get; set; }

        [JsonPropertyName("calorias")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? Calorias { get; set; }

        [JsonPropertyName("distanciaKm")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? DistanciaKm { get; set; }

        [JsonPropertyName("horasSueno")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? HorasSueno { get; set; }
    }

    public class GraficaSeriesResponseDto
    {
        [JsonPropertyName("IdPaciente")]
        public string IdPaciente { get; set; } = null!;

        [JsonPropertyName("metricasSolicitadas")]
        public List<string> MetricasSolicitadas { get; set; } = new();

        [JsonPropertyName("series")]
        public List<PuntoSerieDto> Series { get; set; } = new();
    }
}