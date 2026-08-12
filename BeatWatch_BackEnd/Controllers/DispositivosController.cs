using BeatWatch_BackEnd.Dtos;
using BeatWatch_BackEnd.Dtos.dispositivos;
using BeatWatch_BackEnd.Dtos.pacientesDtos;
using BeatWatch_BackEnd.infrescture;
using BeatWatch_BackEnd.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BeatWatch_BackEnd.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DispositivosController : ControllerBase
    {
        private readonly IDispositivoService _dispositivoService;
        private readonly IPacienteAccessService _pacienteAccessService;
        private readonly IMedicionService _medicionService;
        private readonly IAlertaService _alertaService;

        public DispositivosController(IDispositivoService dispositivoService, IPacienteAccessService pacienteAccessService, IMedicionService medicionService, IAlertaService alertaService)
        {
            _dispositivoService = dispositivoService;
            _pacienteAccessService = pacienteAccessService;
            _medicionService = medicionService;
            _alertaService = alertaService;
        }

        // 🟢 1. Endpoint llamado por el Reloj para iniciar la sesión QR
        [AllowAnonymous]
        [EnableRateLimiting("device-pairing")]
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
                if (!await _pacienteAccessService.PuedeAccederAsync(User, dto.IdPaciente)) return Forbid();
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
        [EnableRateLimiting("device-pairing")]
        [HttpGet("emparejamiento/{idSesion}/estado")]
        public async Task<IActionResult> ObtenerEstadoEmparejamiento(string idSesion, [FromHeader(Name = "X-Watch-Secret")] string watchSecret)
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
                // 1. Extraer idLicencia desde las Claims del JWT del usuario logueado
                var idLicenciaClaim = User.FindFirst("idLicencia")?.Value
                                    ?? User.FindFirst("LicenciaId")?.Value;

                // 2. Si se envió idPaciente explícito (App Móvil), validamos el acceso
                if (!string.IsNullOrWhiteSpace(idPaciente))
                {
                    if (!await _pacienteAccessService.PuedeAccederAsync(User, idPaciente))
                    {
                        return Forbid();
                    }

                    var dispositivosPaciente = await _dispositivoService.ObtenerDispositivosPorPacienteAsync(idPaciente);
                    return Ok(dispositivosPaciente);
                }

                // 3. Si NO se envió idPaciente (Dashboard Web), obtenemos los dispositivos por Licencia
                if (string.IsNullOrWhiteSpace(idLicenciaClaim))
                {
                    return BadRequest(new { mensaje = "El usuario autenticado no tiene una licencia asociada." });
                }

                var dispositivosLicencia = await _dispositivoService.ObtenerDispositivosPorLicenciaAsync(idLicenciaClaim);
                return Ok(dispositivosLicencia);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { mensaje = "Error interno al consultar dispositivos." });
            }
        }
        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> ActualizarAlias(string id, [FromBody] ActualizarAliasDto dto)
        {
            try
            {
                var dispositivo = await _dispositivoService.ObtenerDispositivoAsync(id);
                if (dispositivo is null) return NotFound(new { mensaje = $"No se encontró ningún dispositivo con el ID '{id}'." });
                if (!await _pacienteAccessService.PuedeAccederAsync(User, dispositivo.IdPaciente)) return Forbid();
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
                var dispositivo = await _dispositivoService.ObtenerDispositivoAsync(id);
                if (dispositivo is null) return NotFound(new { mensaje = $"No se encontró ningún dispositivo registrado con el ID '{id}'." });
                if (!await _pacienteAccessService.PuedeAccederAsync(User, dispositivo.IdPaciente)) return Forbid();
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

        [AllowAnonymous]
        [HttpPost("{idDispositivo}/mediciones")]
        public async Task<IActionResult> RegistrarMedicion(string idDispositivo, [FromBody] RegistrarMedicionDto dto, [FromHeader(Name = "X-Watch-Access-Token")] string? watchAccessToken = null)
        {
            try
            {
                var esDispositivoAutenticado = !string.IsNullOrWhiteSpace(watchAccessToken) && await _dispositivoService.ValidarTokenDeDispositivoAsync(idDispositivo, watchAccessToken);
                if (!esDispositivoAutenticado && User.Identity?.IsAuthenticated != true) return Unauthorized();
                if (!esDispositivoAutenticado && !await _pacienteAccessService.PuedeAccederADispositivoAsync(User, idDispositivo)) return Forbid();
                var idMedicion = await _medicionService.RegistrarMedicionAsync(idDispositivo, dto);

                return StatusCode(201, new
                {
                    success = true,
                    idMedicion = idMedicion
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, mensaje = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, mensaje = "Error al registrar la medición.", detalle = ex.Message });
            }
        }
        // POST /api/Dispositivos/{idDispositivo}/alertas
        [AllowAnonymous]
        [HttpPost("{idDispositivo}/alertas")]
        public async Task<IActionResult> RegistrarAlerta(string idDispositivo, [FromBody] CrearAlertaDto dto, [FromHeader(Name = "X-Watch-Access-Token")] string? watchAccessToken = null)
        {
            try
            {
                var esDispositivoAutenticado = !string.IsNullOrWhiteSpace(watchAccessToken) && await _dispositivoService.ValidarTokenDeDispositivoAsync(idDispositivo, watchAccessToken);
                if (!esDispositivoAutenticado && User.Identity?.IsAuthenticated != true) return Unauthorized();
                if (!esDispositivoAutenticado && !await _pacienteAccessService.PuedeAccederADispositivoAsync(User, idDispositivo)) return Forbid();
                var respuesta = await _alertaService.RegistrarAlertaAsync(idDispositivo, dto);

                return StatusCode(201, respuesta);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { mensaje = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al registrar la alerta del dispositivo.", detalle = ex.Message });
            }
        }

    }
}
