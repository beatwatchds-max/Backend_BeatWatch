using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BeatWatch_BackEnd.Dtos
{
    public class CrearAlertaDto
    {
        [Required]
        [JsonPropertyName("tipo")]
        public string Tipo { get; set; } = null!; // PULSO_ANORMAL o TIEMPO_EXCEDIDO

        [Required]
        [JsonPropertyName("valorDetectado")]
        public double ValorDetectado { get; set; }

        [Required]
        [JsonPropertyName("mensaje")]
        public string Mensaje { get; set; } = null!;

        [Required]
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }
    }

    public class AlertaResponseDto
    {
        [JsonPropertyName("idAlerta")]
        public string? IdAlerta { get; set; }

        [JsonPropertyName("tipo")]
        public string Tipo { get; set; } = null!;

        [JsonPropertyName("valorDetectado")]
        public double ValorDetectado { get; set; }

        [JsonPropertyName("mensaje")]
        public string Mensaje { get; set; } = null!;

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }
    }
}