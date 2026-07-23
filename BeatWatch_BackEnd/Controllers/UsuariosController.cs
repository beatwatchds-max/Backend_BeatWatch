using BeatWatch_BackEnd.Dtos;
using BeatWatch_BackEnd.infrescture;
using BeatWatch_BackEnd.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeatWatch_BackEnd.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsuariosController : ControllerBase
    {
        private readonly IUsuarioService _usuarioService;

        public UsuariosController(IUsuarioService usuarioService)
        {
            _usuarioService = usuarioService;
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerUsuarios(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchName = null,
            [FromQuery] string? searchEmail = null)
        {
            // Validaciones básicas de seguridad para la paginación
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100; // Evitamos que pidan demasiados datos de golpe

            var resultado = await _usuarioService.ObtenerUsuariosPaginadosAsync(page, pageSize, searchName, searchEmail);

            return Ok(resultado);
        }

        [Authorize(Roles = "Administrador,Cuidador, Paciente")]
        [HttpPost("perfil")]
        public async Task<IActionResult> CrearPerfilPaciente([FromServices] PacienteService pacienteService, [FromBody] CrearPerfilPacienteDto perfilDto)
        {
            try
            {
                var paciente = await pacienteService.CrearPerfilAsync(perfilDto);
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

        [Authorize(Roles = "Administrador")]
        [HttpDelete("{id}/borrado-logico")]
        public async Task<IActionResult> BorradoLogico(string id, CancellationToken cancellationToken)
        {
            try
            {
                var actualizado = await _usuarioService.DesactivarAsync(id, cancellationToken);
                return actualizado
                    ? NoContent()
                    : NotFound(new { mensaje = "Usuario no encontrado." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        [Authorize(Roles = "Administrador")]
        [HttpPut("{id}/cuidadores")]
        public async Task<IActionResult> ActualizarCuidadores(
            string id,
            [FromBody] ActualizarCuidadoresDto request,
            CancellationToken cancellationToken)
        {
            try
            {
                var actualizado = await _usuarioService.ActualizarCuidadoresAsync(id, request.Cuidadores, cancellationToken);
                return actualizado
                    ? NoContent()
                    : NotFound(new { mensaje = "Usuario no encontrado." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        [Authorize(Roles = "Administrador")]
        [HttpDelete("{id}/cuidadores/{cuidadorId}")]
        public async Task<IActionResult> DesvincularCuidador(string id, string cuidadorId, CancellationToken cancellationToken)
        {
            try
            {
                var actualizado = await _usuarioService.DesvincularCuidadorAsync(id, cuidadorId, cancellationToken);
                return actualizado
                    ? NoContent()
                    : NotFound(new { mensaje = "Usuario no encontrado." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }
    }
}
