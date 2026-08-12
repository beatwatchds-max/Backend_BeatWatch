using BeatWatch_BackEnd.Dtos;
using BeatWatch_BackEnd.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using BeatWatch_BackEnd.Dtos.Login;

namespace BeatWatch_BackEnd.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AutenticacionController : ControllerBase
    {
        private readonly AutenticacionService _authService;

        public AutenticacionController(AutenticacionService authService)
        {
            _authService = authService;
        }

        [HttpPost("iniciar-sesion-movil")]
        [AllowAnonymous]
        [EnableRateLimiting("LoginMovilPolicy")]
        public async Task<IActionResult> IniciarSesionMovil([FromBody] LoginMovilDto loginDto)
        {
            if (string.IsNullOrWhiteSpace(loginDto.Token) || loginDto.Token.Length != 9)
            {
                return BadRequest(new { mensaje = "El token debe tener exactamente 9 dígitos." });
            }

            try
            {
                var respuesta = await _authService.ValidarTokenYGenerarJwtAsync(loginDto.Token);

                if (respuesta == null)
                {
                    return Unauthorized(new { mensaje = "Token inválido o paciente no encontrado." });
                }

                return Ok(respuesta);
            }
            catch (InvalidOperationException ex)
            {
                // 🟢 Bloqueo a un segundo dispositivo mientras la sesión esté activa
                return StatusCode(409, new { mensaje = ex.Message }); // 409 Conflict
            }
            catch (Exception)
            {
                return StatusCode(500, new { mensaje = "Error interno al procesar el inicio de sesión." });
            }
        }

        // 🟢 Endpoint para liberar la sesión activa desde la app móvil
        [HttpPost("cerrar-sesion-movil")]
        [Authorize]
        public async Task<IActionResult> CerrarSesionMovil()
        {
            var usuarioId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(usuarioId)) return Unauthorized();

            await _authService.CerrarSesionMovilAsync(usuarioId);
            return Ok(new { mensaje = "Sesión cerrada correctamente." });
        }


    }
}
