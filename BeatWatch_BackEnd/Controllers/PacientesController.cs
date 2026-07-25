using BeatWatch_BackEnd.Dtos;
using BeatWatch_BackEnd.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeatWatch_BackEnd.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PacientesController : ControllerBase
    {
        private readonly PacienteService _pacienteService;

        public PacientesController(PacienteService pacienteService)
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
    }
}