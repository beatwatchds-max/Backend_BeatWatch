using BeatWatch_BackEnd.Dtos;
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

            // 'respuesta' ya trae el JWT + Nombre + Correo + Telefono + Rol
            var respuesta = await _authService.ValidarTokenYGenerarJwtAsync(loginDto.Token);

            if (respuesta == null)
            {
                return Unauthorized(new { mensaje = "Token inválido o paciente no encontrado." });
            }

            // Devolvemos el DTO completo con status 200 OK
            return Ok(respuesta);
        }
    }
}