using BeatWatch_BackEnd.infrescture;
using BeatWatch_BackEnd.Services;
using Microsoft.AspNetCore.Mvc;

namespace BeatWatch_BackEnd.Controllers
{
    [ApiController]
    [Route("api")]
    public class EtlEstadisticasController : ControllerBase
    {
        private readonly IEstadisticaService _estadisticaService;

        public EtlEstadisticasController(IEstadisticaService estadisticaService)
        {
            _estadisticaService = estadisticaService;
        }

        // GET /api/pacientes
        [HttpGet("pacientes")]
        public async Task<IActionResult> ObtenerPacientesETL()
        {
            try
            {
                var resultado = await _estadisticaService.ObtenerPacientesUnicosConUltimoRegistroAsync();
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener la lista de pacientes registrados en estadísticas.", detalle = ex.Message });
            }
        }

        // GET /api/estadisticas/{id_paciente}
        // GET /api/estadisticas/{id_paciente}?fecha_inicio=2026-07-01&fecha_fin=2026-07-31
        [HttpGet("estadisticas/{id_paciente}")]
        public async Task<IActionResult> ObtenerEstadisticasPaciente(
            string id_paciente,
            [FromQuery] DateTime? fecha_inicio = null,
            [FromQuery] DateTime? fecha_fin = null)
        {
            try
            {
                var resultados = await _estadisticaService.ObtenerEstadisticasPorPacienteAsync(id_paciente, fecha_inicio, fecha_fin);

                if (!resultados.Any())
                {
                    return NotFound(new { mensaje = $"No se encontraron estadísticas para el paciente con ID '{id_paciente}' en el rango especificado." });
                }

                // Si NO se pasaron fechas en la URL, devolvemos el objeto único directamente (punto anterior)
                if (!fecha_inicio.HasValue && !fecha_fin.HasValue)
                {
                    return Ok(resultados.First());
                }

                // Si se pasaron fechas, devolvemos el arreglo/historial completo (punto actual)
                return Ok(resultados);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al consultar el historial de estadísticas.", detalle = ex.Message });
            }
        }

        // GET /api/graficas/{id_paciente}/bpm
        // GET /api/graficas/{id_paciente}/bpm?fecha_inicio=2026-07-01&fecha_fin=2026-07-31
        // GET /api/graficas/{id_paciente}/bpm?dias=30
        [HttpGet("graficas/{id_paciente}/bpm")]
        public async Task<IActionResult> ObtenerGraficaBpm(
            string id_paciente,
            [FromQuery] DateTime? fecha_inicio = null,
            [FromQuery] DateTime? fecha_fin = null,
            [FromQuery] int dias = 7)
        {
            try
            {
                var resultado = await _estadisticaService.ObtenerGraficaBpmAsync(id_paciente, fecha_inicio, fecha_fin, dias);
                return Ok(resultado);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al generar la gráfica de ritmo cardíaco.", detalle = ex.Message });
            }
        }

        // GET /api/graficas/{id_paciente}/episodios
        // GET /api/graficas/{id_paciente}/episodios?fecha_inicio=2026-07-01&fecha_fin=2026-07-31
        // GET /api/graficas/{id_paciente}/episodios?dias=30
        [HttpGet("graficas/{id_paciente}/episodios")]
        public async Task<IActionResult> ObtenerGraficaEpisodios(
            string id_paciente,
            [FromQuery] DateTime? fecha_inicio = null,
            [FromQuery] DateTime? fecha_fin = null,
            [FromQuery] int dias = 7)
        {
            try
            {
                var resultado = await _estadisticaService.ObtenerGraficaEpisodiosAsync(id_paciente, fecha_inicio, fecha_fin, dias);
                return Ok(resultado);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al generar la gráfica de episodios de arritmia.", detalle = ex.Message });
            }
        }

        // GET /api/graficas/{id_paciente}/series?metricas=BPMPromedio,BPMMinimo,BPMMaximo
        // GET /api/graficas/{id_paciente}/series?fecha_inicio=2026-07-01&fecha_fin=2026-07-31&metricas=BPMPromedio,Pasos
        [HttpGet("graficas/{id_paciente}/series")]
        public async Task<IActionResult> ObtenerGraficaSeries(
            string id_paciente,
            [FromQuery] DateTime? fecha_inicio = null,
            [FromQuery] DateTime? fecha_fin = null,
            [FromQuery] string? metricas = null)
        {
            try
            {
                var resultado = await _estadisticaService.ObtenerGraficaSeriesColumnarAsync(id_paciente, fecha_inicio, fecha_fin, metricas);
                return Ok(resultado);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al generar la serie de tiempo.", detalle = ex.Message });
            }
        }

        // GET /api/graficas/{id_paciente}/resumen
        // GET /api/graficas/{id_paciente}/resumen?dias=30
        // GET /api/graficas/{id_paciente}/resumen?fecha_inicio=2026-07-01&fecha_fin=2026-07-31
        [HttpGet("graficas/{id_paciente}/resumen")]
        public async Task<IActionResult> ObtenerResumenKpi(
            string id_paciente,
            [FromQuery] DateTime? fecha_inicio = null,
            [FromQuery] DateTime? fecha_fin = null,
            [FromQuery] int dias = 30)
        {
            try
            {
                var resultado = await _estadisticaService.ObtenerResumenKpiAsync(id_paciente, fecha_inicio, fecha_fin, dias);
                return Ok(resultado);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al generar el resumen de métricas KPI.", detalle = ex.Message });
            }
        }

    }
}