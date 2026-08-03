using BeatWatch_BackEnd.Dtos;
using BeatWatch_BackEnd.infrescture;
using BeatWatch_BackEnd.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BeatWatch_BackEnd.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PacientesController : ControllerBase
    {
        private readonly IPacienteService _pacienteService;

        public PacientesController(IPacienteService pacienteService)
        {
            _pacienteService = pacienteService;
        }

        // 1. POST /api/Pacientes/registrar
        [HttpPost("registrar")]
        public async Task<IActionResult> RegistrarPaciente([FromBody] CrearPacienteDto pacienteDto)
        {
            try
            {
                var pacienteCreado = await _pacienteService.RegistrarPacienteAsync(pacienteDto);

                return Ok(new
                {
                    mensaje = "Paciente registrado y token generado con éxito.",
                    pacienteId = pacienteCreado.Id,
                    tokenGenerado = pacienteCreado.TokenMovil
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al generar el token y registrar el paciente.", detalle = ex.Message });
            }
        }

        // 2. POST /api/Pacientes/perfil
        [Authorize(Roles = "Administrador,Cuidador,Paciente")]
        [HttpPost("perfil")]
        public async Task<IActionResult> CrearPerfilPaciente([FromBody] CrearPerfilPacienteDto perfilDto)
        {
            try
            {
                var paciente = await _pacienteService.CrearPerfilAsync(perfilDto);
                return StatusCode(StatusCodes.Status201Created, new { pacienteId = paciente.Id });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { mensaje = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }
        [HttpGet("usuario/{usuarioId}")]
        [Authorize]
        public async Task<IActionResult> ObtenerPerfilPorUsuarioId(string usuarioId)
        {
            // Extraer el UsuarioId del Token JWT cargado en los Claims
            var usuarioIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Validar seguridad: Solo el propio usuario (o un admin) puede consultar su perfil
            if (usuarioIdClaim != usuarioId && !User.IsInRole("Admin") && !User.IsInRole("Cuidador"))
            {
                return Forbid(); // 403 Forbidden si intenta acceder a datos de otro usuario
            }

            var paciente = await _pacienteService.ObtenerPorUsuarioIdAsync(usuarioId);

            if (paciente == null)
            {
                return NotFound(new { mensaje = "El perfil del paciente aún no ha sido registrado." });
            }

            return Ok(new
            {
                pacienteId = paciente.Id,
                usuarioId = paciente.UsuarioId,
                curp = paciente.CURP,
                edad = paciente.Edad,
                sexo = paciente.Sexo,
                peso = paciente.Peso,
                estatura = paciente.Estatura,
                fechaNacimiento = paciente.FechaNacimiento,
                direccion = paciente.Direccion,
                tipoSangre = paciente.TipoSangre,
                idLicencia = paciente.IdLicencia,
                fotografia = paciente.Fotografia
            });
        }
    }
}
