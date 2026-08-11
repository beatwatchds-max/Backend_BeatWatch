using BeatWatch_BackEnd.Dtos.cuidadores;
using BeatWatch_BackEnd.infrescture;
using BeatWatch_BackEnd.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> ObtenerUsuarios(
      [FromQuery] int page = 1,
      [FromQuery] int pageSize = 10,
      [FromQuery] string? searchName = null,
       [FromQuery] string? searchEmail = null)
        {
            // 1. Validaciones básicas de paginación
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            try
            {
                // El ámbito de licencia siempre procede del JWT, nunca de la consulta del cliente.
                var idLicencia = User.FindFirst("idLicencia")?.Value
                              ?? User.FindFirst("LicenciaId")?.Value;

                if (string.IsNullOrEmpty(idLicencia))
                {
                    return BadRequest(new { mensaje = "No se encontró un identificador de licencia válido en la sesión." });
                }

                var resultado = await _usuarioService.ObtenerUsuariosPaginadosAsync(page, pageSize, searchName, searchEmail, idLicencia);
                return Ok(resultado);
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

        [HttpPost("registrar-cuidador")]
        [Authorize(Roles = "Administrador")] // 🔒 Requiere token del Admin logueado
        public async Task<ActionResult> RegistrarCuidador([FromBody] RegistrarCuidadorDto request)
        {
            try
            {
                // 🟢 Extraemos la ID del Admin desde el Token JWT de la sesión activa
                var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                ?? User.FindFirst("sub")?.Value;

                if (string.IsNullOrEmpty(adminIdClaim))
                {
                    return Unauthorized(new { message = "Sesión no válida o expirada." });
                }

                var cuidador = await _usuarioService.RegistrarCuidadorDesdeSesionAsync(request, adminIdClaim);

                return Ok(new
                {
                    mensaje = "Cuidador registrado exitosamente.",
                    cuidadorId = cuidador.Id,
                    nombre = cuidador.Nombre,
                    correo = cuidador.Correo,
                    tokenGenerado = cuidador.TokenMovil,
                    idLicencia = cuidador.IdLicencia,
                    rol = cuidador.Rol
                });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error al registrar el cuidador." });
            }
        }
        // 🟢 GET /api/Usuarios/cuidadores-disponibles
        [HttpGet("cuidadores-disponibles")]
        [Authorize(Roles = "Administrador,Cuidador")]
        public async Task<IActionResult> ObtenerCuidadoresDisponibles()
        {
            try
            {
                // Extraer idLicencia desde el Token JWT
                var idLicenciaClaim = User.FindFirst("idLicencia")?.Value
                                    ?? User.FindFirst("LicenciaId")?.Value;

                if (string.IsNullOrEmpty(idLicenciaClaim))
                {
                    return BadRequest(new { mensaje = "No se encontró el identificador de la licencia en la sesión actual." });
                }

                var cuidadores = await _usuarioService.ObtenerCuidadoresYAdminsPorLicenciaAsync(idLicenciaClaim);
                return Ok(cuidadores);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { mensaje = "Error al obtener la lista de cuidadores." });
            }
        }
    }
}
