using BeatWatch_BackEnd.Models;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using BeatWatch_BackEnd.infrescture;

namespace BeatWatch_BackEnd.Controllers;

[ApiController]
[Route("api/autenticacion")]
public class AuthController : ControllerBase
{
    private readonly IUsuarioService _usuarioService;
    private readonly ITokenService _tokenService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IUsuarioService usuarioService, ITokenService tokenService, IEmailService emailService, IConfiguration configuration, ILogger<AuthController> logger)
    {
        _usuarioService = usuarioService;
        _tokenService = tokenService;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("registrar")]
    public async Task<ActionResult> Registrar([FromBody] RegistroRequest request)
    {
        try
        {
            var usuario = await _usuarioService.RegistrarAsync(request);

            // Devolvemos una respuesta estructurada que incluya el token
            return CreatedAtAction(nameof(Registrar), new { id = usuario.Id }, new
            {
                mensaje = "Administrador registrado con éxito.",
                usuarioId = usuario.Id,
                tokenGenerado = usuario.TokenMovil,
                rol = usuario.Rol
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("login")]
    [EnableRateLimiting("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginWebRequest request, CancellationToken cancellationToken)
    {
        var remoteIpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var usuario = await _usuarioService.AutenticarAsync(request.Correo, request.Contrasena);

        if (usuario is null)
        {
            _logger.LogWarning("Intento de inicio de sesion rechazado desde {RemoteIpAddress}", remoteIpAddress);
            return Unauthorized(new { message = "Credenciales o verificacion invalidas." });
        }

        return Ok(_tokenService.CreateAccessToken(usuario));
    }

    [HttpPost("recuperar-contrasena")]
    [EnableRateLimiting("password-recovery")]
    public async Task<IActionResult> SolicitarRestablecimiento([FromBody] SolicitarRestablecimientoRequest request, CancellationToken cancellationToken)
    {
        var token = await _usuarioService.CrearTokenRestablecimientoAsync(request.Correo, cancellationToken);
        var resetUrl = _configuration["EmailSettings:PasswordResetUrl"];
        if (token is not null && !string.IsNullOrWhiteSpace(resetUrl))
        {
            var separator = resetUrl.Contains('?') ? '&' : '?';
            try
            {
                await _emailService.SendPasswordResetAsync(request.Correo, $"{resetUrl}{separator}token={Uri.EscapeDataString(token)}", cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo enviar el correo de restablecimiento.");
            }
        }

        return Accepted(new { message = "Si el correo esta registrado, recibira instrucciones para restablecer su contrasena." });
    }

    [HttpPost("restablecer-contrasena")]
    [EnableRateLimiting("password-recovery")]
    public async Task<IActionResult> RestablecerContrasena([FromBody] RestablecerContrasenaRequest request, CancellationToken cancellationToken)
    {
        var restablecida = await _usuarioService.RestablecerContrasenaAsync(request.Token, request.Contrasena, cancellationToken);
        if (!restablecida)
        {
            return BadRequest(new { message = "El enlace de restablecimiento no es valido o ha expirado." });
        }

        return NoContent();
    }
}
