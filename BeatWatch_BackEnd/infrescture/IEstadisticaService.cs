using BeatWatch_BackEnd.Dtos.graficas;
using BeatWatch_BackEnd.Dtos.pacientesDtos;
using BeatWatch_BackEnd.Models;

namespace BeatWatch_BackEnd.infrescture
{
    public interface IEstadisticaService
    {
        Task<List<PacienteEstadisticaResumenDto>> ObtenerPacientesUnicosConUltimoRegistroAsync(string idLicencia);
        // 🟢 Devuelve una lista si hay filtro de fechas, o un elemento si no hay fechas
        Task<List<EstadisticaDiarias>> ObtenerEstadisticasPorPacienteAsync(
            string idPaciente,
            DateTime? fechaInicio = null,
            DateTime? fechaFin = null);

        Task<GraficaBpmResponseDto> ObtenerGraficaBpmAsync(
            string idPaciente,
            DateTime? fechaInicio = null,
            DateTime? fechaFin = null,
            int dias = 7);
        Task<GraficaEpisodiosResponseDto> ObtenerGraficaEpisodiosAsync(string idPaciente, DateTime? fechaInicio = null, DateTime? fechaFin = null, int dias = 7);

        Task<GraficaSeriesResponseDto> ObtenerGraficaSeriesAsync(
            string idPaciente,
            DateTime? fechaInicio = null,
            DateTime? fechaFin = null,
            string? metricas = null);

        Task<GraficaSeriesColumnarResponseDto> ObtenerGraficaSeriesColumnarAsync(
            string idPaciente,
            DateTime? fechaInicio = null,
            DateTime? fechaFin = null,
            string? metricas = null);

        Task<GraficaResumenResponseDto> ObtenerResumenKpiAsync(
            string idPaciente,
            DateTime? fechaInicio = null,
            DateTime? fechaFin = null,
            int dias = 30);
    }
}
