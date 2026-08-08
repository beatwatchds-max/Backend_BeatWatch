using System.Text.Json.Serialization;

namespace BeatWatch_BackEnd.Dtos
{
    public class PacienteEstadisticaResumenDto
    {
        [JsonPropertyName("IdPaciente")]
        public string IdPaciente { get; set; } = null!;

        [JsonPropertyName("UltimoRegistro")]
        public DateTime UltimoRegistro { get; set; }
    }
}