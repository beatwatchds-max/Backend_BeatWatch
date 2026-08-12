using BeatWatch_BackEnd.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

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

        [HttpPost("activar-gratuita")]
        [Authorize]
        [EnableRateLimiting("license-activation")]
        public async Task<IActionResult> ActivarLicenciaGratuita()
        {
            try
            {
                var usuarioId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
                if (string.IsNullOrWhiteSpace(usuarioId)) return Unauthorized();
                var resultado = await _licenciaService.ActivarLicenciaGratuitaAsync(usuarioId);

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
