using BeatWatch_BackEnd.Dtos;
using BeatWatch_BackEnd.infrescture;
using BeatWatch_BackEnd.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeatWatch_BackEnd.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DispositivosController : ControllerBase
    {
        private readonly IDispositivoService _dispositivoService;

        public DispositivosController(IDispositivoService dispositivoService)
        {
            _dispositivoService = dispositivoService;
        }

        [HttpPost("emparejar")]
        public async Task<IActionResult> Emparejar([FromBody] EmparejarDispositivoDto dto)
        {
            try
            {
                var dispositivo = await _dispositivoService.EmparejarDispositivoAsync(dto);
                return StatusCode(StatusCodes.Status201Created, dispositivo);
            }
            catch (InvalidOperationException ex)
            {
                // Criterio de Aceptación: 400 Bad Request si el NumeroSerie ya existeAA
                return BadRequest(new { mensaje = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerDispositivos([FromQuery] string? idPaciente)
        {
            try
            {
                var dispositivos = await _dispositivoService.ObtenerDispositivosPorPacienteAsync(idPaciente);
                return Ok(dispositivos);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        // 🟢 HU3.3: Endpoint para actualizar el Alias de un Dispositivo
        [HttpPut("{id}")]
        public async Task<IActionResult> ActualizarAlias(string id, [FromBody] ActualizarAliasDto dto)
        {
            try
            {
                var actualizado = await _dispositivoService.ActualizarAliasAsync(id, dto.Alias);

                if (!actualizado)
                {
                    return NotFound(new { mensaje = $"No se encontró ningún dispositivo con el ID '{id}'." });
                }

                return Ok(new { mensaje = "Alias actualizado correctamente.", nuevoAlias = dto.Alias.Trim() });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        // 🟢 HU3.4: Endpoint para desvincular/eliminar un dispositivo
        [HttpDelete("{id}")]
        public async Task<IActionResult> EliminarDispositivo(string id)
        {
            try
            {
                var eliminado = await _dispositivoService.EliminarDispositivoAsync(id);

                if (!eliminado)
                {
                    return NotFound(new { mensaje = $"No se encontró ningún dispositivo registrado con el ID '{id}'." });
                }

                return Ok(new { mensaje = "Dispositivo desvinculado y eliminado exitosamente." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }
    }
}
