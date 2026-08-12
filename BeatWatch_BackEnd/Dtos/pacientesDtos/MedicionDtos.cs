using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BeatWatch_BackEnd.Dtos.pacientesDtos
{
    public class RegistrarMedicionDto
    {
        [Required]
        [Range(30, 250, ErrorMessage = "La frecuencia cardíaca debe ser un valor válido.")]
        public int FrecuenciaCardiacaBpm { get; set; }

        [Range(50, 100)]
        public int? SaturacionOxigenoSpO2 { get; set; }

        [Required]
        public DateTime Timestamp { get; set; }
    }

    public class MedicionResponseDto
    {
        [JsonPropertyName("idMedicion")]
        public string IdMedicion { get; set; } = null!;

        [JsonPropertyName("frecuenciaCardiacaBpm")]
        public int FrecuenciaCardiacaBpm { get; set; }

        [JsonPropertyName("saturacionOxigenoSpO2")]
        public int? SaturacionOxigenoSpO2 { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }
    }

    public class HistorialMedicionesResponseDto
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; } = true;

        [JsonPropertyName("mediciones")]
        public List<MedicionResponseDto> Mediciones { get; set; } = new();
    }
}