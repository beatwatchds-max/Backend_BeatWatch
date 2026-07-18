using BeatWatch_BackEnd.DTOs;
using BeatWatch_BackEnd.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

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
        [EnableRateLimiting("LoginMovilPolicy")]
        public async Task<IActionResult> IniciarSesionMovil([FromBody] LoginMovilDto loginDto)
        {
            if (string.IsNullOrWhiteSpace(loginDto.Token) || loginDto.Token.Length != 9)
            {
                return BadRequest(new { mensaje = "El token debe tener exactamente 9 dígitos." });
            }

            var tokenJwt = await _authService.ValidarTokenYGenerarJwtAsync(loginDto.Token);

            if (tokenJwt == null)
            {
                return Unauthorized(new { mensaje = "Token inválido o paciente no encontrado." });
            }

            return Ok(new
            {
                mensaje = "Inicio de sesión exitoso.",
                token = tokenJwt
            });
        }
    }
}