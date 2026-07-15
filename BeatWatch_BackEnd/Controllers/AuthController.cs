using BeatWatch_BackEnd.Models;
using BeatWatch_BackEnd.Services;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;

namespace BeatWatch_BackEnd.Controllers;

[ApiController]
[Route("api/autenticacion")]
public class AuthController : ControllerBase
{
    private readonly IUsuarioService _usuarioService;
    private readonly ICaptchaVerifier _captchaVerifier;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IUsuarioService usuarioService, ICaptchaVerifier captchaVerifier, ITokenService tokenService, ILogger<AuthController> logger)
    {
        _usuarioService = usuarioService;
        _captchaVerifier = captchaVerifier;
        _tokenService = tokenService;
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
        var captchaValid = await _captchaVerifier.IsValidAsync(request.RecaptchaToken, remoteIpAddress, cancellationToken);
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
}
