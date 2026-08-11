using BeatWatch_BackEnd.infrescture;
using BeatWatch_BackEnd.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeatWatch_BackEnd.Controllers
{
    [ApiController]
    [Route("api")]
    [Authorize(Roles = "Administrador,Cuidador")]
    public class EtlEstadisticasController : ControllerBase
    {
        private readonly IEstadisticaService _estadisticaService;
        private readonly IPacienteAccessService _pacienteAccessService;

        public EtlEstadisticasController(IEstadisticaService estadisticaService, IPacienteAccessService pacienteAccessService)
        {
            _estadisticaService = estadisticaService;
            _pacienteAccessService = pacienteAccessService;
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
            catch (Exception)
            {
                return StatusCode(500, new { mensaje = "Error al obtener la lista de pacientes registrados en estadísticas." });
            }
        }

        // GET /api/estadisticas/{id_paciente}
        // GET /api/estadisticas/{id_paciente}?fecha_inicio=2026-07-01&fecha_fin=2026-07-31
        [HttpGet("estadisticas/{id_paciente}")]
        public async Task<IActionResult> ObtenerEstadisticasPaciente(string id_paciente,[FromQuery] DateTime? fecha_inicio = null,  [FromQuery] DateTime? fecha_fin = null)
        {
            try
            {
                if (!await _pacienteAccessService.PuedeAccederAsync(User, id_paciente)) return Forbid();
                var rangoInvalido = ValidarRango(fecha_inicio, fecha_fin);
                if (rangoInvalido is not null) return rangoInvalido;
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
            catch (Exception)
            {
                return StatusCode(500, new { mensaje = "Error al consultar el historial de estadísticas." });
            }
        }

        // GET /api/graficas/{id_paciente}/bpm
        // GET /api/graficas/{id_paciente}/bpm?fecha_inicio=2026-07-01&fecha_fin=2026-07-31
        // GET /api/graficas/{id_paciente}/bpm?dias=30
        [HttpGet("graficas/{id_paciente}/bpm")]
        public async Task<IActionResult> ObtenerGraficaBpm(string id_paciente,[FromQuery] DateTime? fecha_inicio = null,[FromQuery] DateTime? fecha_fin = null,[FromQuery] int dias = 7)
        {
            try
            {
                if (!await _pacienteAccessService.PuedeAccederAsync(User, id_paciente)) return Forbid();
                var rangoInvalido = ValidarRango(fecha_inicio, fecha_fin, dias);
                if (rangoInvalido is not null) return rangoInvalido;
                var resultado = await _estadisticaService.ObtenerGraficaBpmAsync(id_paciente, fecha_inicio, fecha_fin, dias);
                return Ok(resultado);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { mensaje = "Error al generar la gráfica de ritmo cardíaco." });
            }
        }

        // GET /api/graficas/{id_paciente}/episodios
        // GET /api/graficas/{id_paciente}/episodios?fecha_inicio=2026-07-01&fecha_fin=2026-07-31
        // GET /api/graficas/{id_paciente}/episodios?dias=30
        [HttpGet("graficas/{id_paciente}/episodios")]
        public async Task<IActionResult> ObtenerGraficaEpisodios(string id_paciente,[FromQuery] DateTime? fecha_inicio = null,[FromQuery] DateTime? fecha_fin = null, [FromQuery] int dias = 7)
        {
            try
            {
                if (!await _pacienteAccessService.PuedeAccederAsync(User, id_paciente)) return Forbid();
                var rangoInvalido = ValidarRango(fecha_inicio, fecha_fin, dias);
                if (rangoInvalido is not null) return rangoInvalido;
                var resultado = await _estadisticaService.ObtenerGraficaEpisodiosAsync(id_paciente, fecha_inicio, fecha_fin, dias);
                return Ok(resultado);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { mensaje = "Error al generar la gráfica de episodios de arritmia." });
            }
        }

        // GET /api/graficas/{id_paciente}/series?metricas=BPMPromedio,BPMMinimo,BPMMaximo
        // GET /api/graficas/{id_paciente}/series?fecha_inicio=2026-07-01&fecha_fin=2026-07-31&metricas=BPMPromedio,Pasos
        [HttpGet("graficas/{id_paciente}/series")]
        public async Task<IActionResult> ObtenerGraficaSeries(string id_paciente,[FromQuery] DateTime? fecha_inicio = null,[FromQuery] DateTime? fecha_fin = null, [FromQuery] string? metricas = null)
        {
            try
            {
                if (!await _pacienteAccessService.PuedeAccederAsync(User, id_paciente)) return Forbid();
                var rangoInvalido = ValidarRango(fecha_inicio, fecha_fin);
                if (rangoInvalido is not null) return rangoInvalido;
                var resultado = await _estadisticaService.ObtenerGraficaSeriesColumnarAsync(id_paciente, fecha_inicio, fecha_fin, metricas);
                return Ok(resultado);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { mensaje = "Error al generar la serie de tiempo." });
            }
        }

        // GET /api/graficas/{id_paciente}/resumen
        // GET /api/graficas/{id_paciente}/resumen?dias=30
        // GET /api/graficas/{id_paciente}/resumen?fecha_inicio=2026-07-01&fecha_fin=2026-07-31
        [HttpGet("graficas/{id_paciente}/resumen")]
        public async Task<IActionResult> ObtenerResumenKpi(string id_paciente,[FromQuery] DateTime? fecha_inicio = null,[FromQuery] DateTime? fecha_fin = null, [FromQuery] int dias = 30)
        {
            try
            {
                if (!await _pacienteAccessService.PuedeAccederAsync(User, id_paciente)) return Forbid();
                var rangoInvalido = ValidarRango(fecha_inicio, fecha_fin, dias);
                if (rangoInvalido is not null) return rangoInvalido;
                var resultado = await _estadisticaService.ObtenerResumenKpiAsync(id_paciente, fecha_inicio, fecha_fin, dias);
                return Ok(resultado);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { mensaje = "Error al generar el resumen de métricas KPI." });
            }
        }

        private IActionResult? ValidarRango(DateTime? fechaInicio, DateTime? fechaFin, int? dias = null)
        {
            if (dias is < 1 or > 90)
            {
                return BadRequest(new { mensaje = "El parámetro dias debe estar entre 1 y 90." });
            }

            if (fechaInicio.HasValue && fechaFin.HasValue)
            {
                if (fechaInicio > fechaFin)
                {
                    return BadRequest(new { mensaje = "La fecha de inicio no puede ser posterior a la fecha de fin." });
                }

                if ((fechaFin.Value.Date - fechaInicio.Value.Date).TotalDays > 90)
                {
                    return BadRequest(new { mensaje = "El rango máximo de consulta es de 90 días." });
                }
            }

            return null;
        }

    }
}
