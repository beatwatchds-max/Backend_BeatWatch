using BeatWatch_BackEnd.Dtos;
using BeatWatch_BackEnd.infrescture;
using BeatWatch_BackEnd.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeatWatch_BackEnd.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DispositivosController : ControllerBase
    {
        private readonly IDispositivoService _dispositivoService;

        public DispositivosController(IDispositivoService dispositivoService)
        {
            _dispositivoService = dispositivoService;
        }

        // 🟢 1. Endpoint llamado por el Reloj para iniciar la sesión QR
        [AllowAnonymous]
        [HttpPost("sesion-emparejamiento")]
        public async Task<IActionResult> CrearSesionEmparejamiento([FromBody] CrearSesionEmparejamientoDto dto)
        {
            try
            {
                var response = await _dispositivoService.CrearSesionEmparejamientoAsync(dto);
                return StatusCode(StatusCodes.Status201Created, response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        // 🟢 2. Endpoint llamado por el Teléfono al escanear el QR
        [Authorize]
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
                return BadRequest(new { mensaje = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        // 🟢 3. Endpoint consultado (Polling) por el Reloj para validar si se emparejó
        [AllowAnonymous]
        [HttpGet("emparejamiento/{idSesion}/estado")]
        public async Task<IActionResult> ObtenerEstadoEmparejamiento(
            string idSesion,
            [FromHeader(Name = "X-Watch-Secret")] string watchSecret)
        {
            try
            {
                var resultado = await _dispositivoService.ObtenerEstadoEmparejamientoAsync(idSesion, watchSecret);

                // Si la sesión venció, responder 410 Gone según especificación
                if (resultado is { } r && r.GetType().GetProperty("estado")?.GetValue(r)?.ToString() == "EXPIRADO")
                {
                    return StatusCode(StatusCodes.Status410Gone, resultado);
                }

                return Ok(resultado);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { success = false, mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, mensaje = ex.Message });
            }
        }

        [Authorize]
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

        [Authorize]
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

        [Authorize]
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