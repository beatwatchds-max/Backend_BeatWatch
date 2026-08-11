using BeatWatch_BackEnd.Dtos;
using BeatWatch_BackEnd.Dtos.pacientesDtos;
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
        private readonly IPacienteAccessService _pacienteAccessService;
        private readonly IMedicionService _medicionService;

        public PacientesController(IPacienteService pacienteService, IPacienteAccessService pacienteAccessService, IMedicionService medicionService)
        {
            _pacienteService = pacienteService;
            _pacienteAccessService = pacienteAccessService;
            _medicionService = medicionService;
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
            catch (Exception)
            {
                return StatusCode(500, new { mensaje = "Error al generar el token y registrar el paciente." });
            }
        }

        // 2. POST /api/Pacientes/perfil
        [Authorize(Roles = "Paciente")]
        [HttpPost("perfil")]
        public async Task<IActionResult> CrearPerfilPaciente([FromBody] CrearPerfilPacienteDto perfilDto)
        {
            try
            {
                perfilDto.UsuarioId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value ?? string.Empty;
                perfilDto.IdLicencia = User.FindFirst("idLicencia")?.Value ?? User.FindFirst("LicenciaId")?.Value ?? string.Empty;
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

        [HttpGet("perfil/paciente/{idPaciente}")]
        [Authorize(Roles = "Administrador,Cuidador,Paciente")]
        public async Task<IActionResult> ObtenerPerfilPorPacienteId(string idPaciente)
        {
            if (!await _pacienteAccessService.PuedeAccederAsync(User, idPaciente)) return Forbid();

            var detallePaciente = await _pacienteService.ObtenerDetallePorPacienteIdAsync(idPaciente);
            return detallePaciente is null
                ? NotFound(new { mensaje = "El perfil del paciente no existe." })
                : Ok(detallePaciente);
        }


        [HttpGet("usuario/{usuarioId}")]
        [Authorize]
        public async Task<IActionResult> ObtenerPerfilPorUsuarioId(string usuarioId)
        {
            try
            {
                var detallePaciente = await _pacienteService.ObtenerDetallePorUsuarioIdAsync(usuarioId);

                if (detallePaciente == null)
                {
                    return NotFound(new { mensaje = "El perfil del paciente aún no ha sido registrado en esta licencia." });
                }

                // Se valida el acceso contra el PacienteId resultante
                if (!await _pacienteAccessService.PuedeAccederAsync(User, detallePaciente.PacienteId))
                {
                    return Forbid();
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

            var detallePaciente = await _pacienteService.ObtenerDetallePorUsuarioIdAsync(usuarioId);
            if (detallePaciente is null)
            {
                return NotFound(new { mensaje = "No se encontró el perfil del paciente para actualizar." });
            }

            if (!await _pacienteAccessService.PuedeAccederAsync(User, detallePaciente.PacienteId)) return Forbid();

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
                    cuidadores = usuario.Cuidadores
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
            catch (Exception)
            {
                return StatusCode(500, new { mensaje = "Error al completar el registro del paciente." });
            }
        }

        [Authorize]
        [HttpGet("{idPaciente}/mediciones")]
        public async Task<IActionResult> ObtenerHistorialMediciones(string idPaciente,[FromQuery] DateTime? desde = null,[FromQuery] DateTime? hasta = null, [FromQuery] int limite = 100)
        {
            try
            {
                // Validar acceso del usuario al paciente
                if (!await _pacienteAccessService.PuedeAccederAsync(User, idPaciente))
                {
                    return Forbid();
                }

                var mediciones = await _medicionService.ObtenerHistorialPacienteAsync(idPaciente, desde, hasta, limite);

                return Ok(new HistorialMedicionesResponseDto
                {
                    Success = true,
                    Mediciones = mediciones
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, mensaje = "Error al obtener el historial de mediciones.", detalle = ex.Message });
            }
        }
    }
}
