using BeatWatch_BackEnd.Models;
using BeatWatch_BackEnd.Services;
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
        public async Task<IActionResult> ProcesarPagoSimulado([FromBody] PagoSimuladoDto pagoDto)
        {
            try
            {
                var resultado = await _licenciaService.ProcesarPagoYCrearLicenciaAsync(pagoDto);

                if (resultado == null)
                {
                    return BadRequest(new { mensaje = "El pago simulado no pudo procesarse." });
                }

                return Ok(new
                {
                    mensaje = "Pago procesado con éxito y licencia activada.",
                    licencia = resultado
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error interno del servidor.", detalle = ex.Message });
            }
        }
    }
}