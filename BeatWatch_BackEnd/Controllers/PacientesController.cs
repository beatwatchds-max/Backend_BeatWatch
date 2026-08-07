using BeatWatch_BackEnd.Dtos;
using BeatWatch_BackEnd.DTOs;
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
        // 1. POST /api/Pacientes/registrar
        [HttpPost("registrar")]
        [Authorize(Roles = "Administrador,Cuidador")] // Endpoint restringido a la Web
        public async Task<IActionResult> RegistrarPaciente([FromBody] CrearPacienteDto pacienteDto)
        {
            try
            {
                // Extraer la IdLicencia desde el Token JWT del usuario de la sesión Web
                var idLicenciaClaim = User.FindFirst("idLicencia")?.Value
                                    ?? User.FindFirst("LicenciaId")?.Value;

                if (string.IsNullOrEmpty(idLicenciaClaim))
                {
                    return BadRequest(new { mensaje = "No se encontró el identificador de licencia en el token de autenticación." });
                }

                var pacienteCreado = await _pacienteService.RegistrarPacienteAsync(pacienteDto, idLicenciaClaim);

                return Ok(new
                {
                    mensaje = "Paciente registrado y token generado con éxito.",
                    pacienteId = pacienteCreado.Id,
                    tokenGenerado = pacienteCreado.TokenMovil
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
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
            try
            {
                // Extraer el UsuarioId del Token JWT
                var usuarioIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                  ?? User.FindFirst("sub")?.Value;

                // Validar seguridad: Solo el propio usuario, un Admin o un Cuidador pueden consultar el perfil
                if (usuarioIdClaim != usuarioId && !User.IsInRole("Administrador") && !User.IsInRole("Cuidador"))
                {
                    return Forbid();
                }

                var detallePaciente = await _pacienteService.ObtenerDetallePorUsuarioIdAsync(usuarioId);

                if (detallePaciente == null)
                {
                    return NotFound(new { mensaje = "El perfil del paciente aún no ha sido registrado." });
                }

                return Ok(detallePaciente);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener el perfil del paciente.", detalle = ex.Message });
            }
        }

        [HttpPatch("perfil/{usuarioId}")]
        [Authorize]
        public async Task<IActionResult> ActualizarPerfilParcial(string usuarioId, [FromBody] ActualizarPerfilPacienteDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var actualizado = await _pacienteService.ActualizarPerfilPacienteAsync(usuarioId, dto);

            if (!actualizado)
                return NotFound(new { mensaje = "No se encontró el perfil del paciente para actualizar." });

            return Ok(new { mensaje = "Perfil actualizado exitosamente." });
        }

        [HttpPost("registrar-completo")]
        [Authorize(Roles = "Administrador,Cuidador")]
        public async Task<IActionResult> RegistrarPacienteCompleto([FromBody] RegistrarPacienteCompletoDto dto)
        {
            try
            {
                // 1. Extracción de la IdLicencia desde los claims del token JWT
                var idLicenciaClaim = User.FindFirst("idLicencia")?.Value
                                    ?? User.FindFirst("LicenciaId")?.Value;

                if (string.IsNullOrEmpty(idLicenciaClaim))
                {
                    return BadRequest(new { mensaje = "No se encontró el identificador de la licencia en la sesión actual." });
                }

                // 2. Extraer el ID del usuario logueado (Admin/Cuidador) de la sesión
                var usuarioSesionId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                    ?? User.FindFirst("sub")?.Value;

                // 3. Si la lista viene vacía en el DTO, auto-asignamos el usuario de la sesión activa
                if ((dto.CuidadoresIds == null || !dto.CuidadoresIds.Any()) && !string.IsNullOrEmpty(usuarioSesionId))
                {
                    dto.CuidadoresIds = new List<string> { usuarioSesionId };
                }

                var (usuario, paciente) = await _pacienteService.RegistrarPacienteCompletoAsync(dto, idLicenciaClaim);

                return StatusCode(StatusCodes.Status201Created, new
                {
                    mensaje = "Paciente y perfil registrados exitosamente con sus cuidadores asignados.",
                    usuarioId = usuario.Id,
                    pacienteId = paciente.Id,
                    tokenMovil = usuario.TokenMovil,
                    idLicencia = usuario.IdLicencia,
                    cuidadores = paciente.Cuidadores
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
                return StatusCode(500, new { mensaje = "Error al completar el registro del paciente.", detalle = ex.Message });
            }
        }
    }
}
