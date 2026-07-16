using BeatWatch_BackEnd.Models;
using BeatWatch_BackEnd.Services;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using BeatWatch_BackEnd.Configuration;
using Microsoft.Extensions.Options;

namespace BeatWatch_BackEnd.Controllers;

[ApiController]
[Route("api/autenticacion")]
public class AuthController : ControllerBase
{
    private readonly IUsuarioService _usuarioService;
    private readonly ICaptchaVerifier _captchaVerifier;
    private readonly ITokenService _tokenService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly RecaptchaSettings _recaptchaSettings;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IUsuarioService usuarioService, ICaptchaVerifier captchaVerifier, ITokenService tokenService, IEmailService emailService, IConfiguration configuration, IOptions<RecaptchaSettings> recaptchaSettings, ILogger<AuthController> logger)
    {
        _usuarioService = usuarioService;
        _captchaVerifier = captchaVerifier;
        _tokenService = tokenService;
        _emailService = emailService;
        _configuration = configuration;
        _recaptchaSettings = recaptchaSettings.Value;
        _logger = logger;
    }

    [HttpPost("registrar")]
    public async Task<ActionResult<Usuario>> Registrar([FromBody] RegistroRequest request)
    {
        try
        {
            var usuario = await _usuarioService.RegistrarAsync(request);
            return CreatedAtAction(nameof(Registrar), new { id = usuario.Id }, usuario);
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
        var captchaValid = !_recaptchaSettings.Enabled
            || await _captchaVerifier.IsValidAsync(request.RecaptchaToken ?? string.Empty, remoteIpAddress, cancellationToken);
        var usuario = captchaValid
            ? await _usuarioService.AutenticarAsync(request.Correo, request.Contrasena)
            : null;

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
