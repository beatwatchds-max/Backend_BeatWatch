using BeatWatch_BackEnd.Models;
using BeatWatch_BackEnd.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

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

        // 🟢 Regresamos la ruta a procesar-pago para que el frontend funcione
        [HttpPost("procesar-pago")]
        [AllowAnonymous]
        [EnableRateLimiting("license-activation")]
        public async Task<IActionResult> ActivarLicenciaGratuita([FromBody] ActivarLicenciaGratuitaDto dto)
        {
            try
            {
                // 🟢 Pasamos el DTO al servicio en lugar del ID del token
                var resultado = await _licenciaService.ActivarLicenciaGratuitaAsync(dto);

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
            catch (InvalidOperationException ex)
            {
                return Conflict(new { mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error interno al activar la licencia.", detalle = ex.Message });
            }
        }
    }
}