using BeatWatch_BackEnd.Dtos.licencia;
using BeatWatch_BackEnd.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeatWatch_BackEnd.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LicenciasController : ControllerBase
    {
        private readonly ILicenciaService _licenciaService;

        public LicenciasController(ILicenciaService licenciaService)
        {
            _licenciaService = licenciaService;
        }

        [HttpPost("procesar-pago")]
        [AllowAnonymous] // 🟢 Omitir bloqueo 401 si se permite activar sin JWT
        public async Task<IActionResult> ProcesarPagoSimulado([FromBody] ActivarLicenciaGratuitaDto dto)
        {
            try
            {
                var resultado = await _licenciaService.ProcesarPagoYCrearLicenciaAsync(dto);

                if (resultado == null)
                {
                    return BadRequest(new { mensaje = "No se pudo activar la licencia gratuita." });
                }

                return Ok(new
                {
                    mensaje = "Plan gratuito activado con éxito.",
                    licencia = resultado
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error interno al activar la licencia.", detalle = ex.Message });
            }
        }
    }
}