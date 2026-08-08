using System.Text.Json.Serialization;

namespace BeatWatch_BackEnd.Dtos
{
    public class EpisodioGraficaDto
    {
        [JsonPropertyName("fecha")]
        public string Fecha { get; set; } = null!;

        [JsonPropertyName("totalArritmias")]
        public int TotalArritmias { get; set; }

        [JsonPropertyName("criticas")]
        public int Criticas { get; set; }

        [JsonPropertyName("duracionTotalSegundos")]
        public int DuracionTotalSegundos { get; set; }
    }

    public class GraficaEpisodiosResponseDto
    {
        [JsonPropertyName("IdPaciente")]
        public string IdPaciente { get; set; } = null!;

        [JsonPropertyName("episodios")]
        public List<EpisodioGraficaDto> Episodios { get; set; } = new();
    }
}