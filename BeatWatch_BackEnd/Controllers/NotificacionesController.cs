using System.Security.Claims;
using BeatWatch_BackEnd.Data;
using BeatWatch_BackEnd.Dtos.notificaciones;
using BeatWatch_BackEnd.infrescture;
using BeatWatch_BackEnd.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace BeatWatch_BackEnd.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class NotificacionesController : ControllerBase
{
    private readonly MongoDbContext _context;
    private readonly IFcmNotificationService _fcmNotificationService;

    public NotificacionesController(MongoDbContext context, IFcmNotificationService fcmNotificationService)
    {
        _context = context;
        _fcmNotificationService = fcmNotificationService;
    }

    [HttpPut("token")]
    public async Task<IActionResult> RegistrarToken([FromBody] TokenFcmDto dto, CancellationToken cancellationToken)
    {
        var usuarioId = ObtenerUsuarioId();
        if (usuarioId is null) return Unauthorized();

        var token = dto.Token.Trim();
        var update = Builders<Usuario>.Update.AddToSet(u => u.TokensFcm, token);
        var result = await _context.Usuarios.UpdateOneAsync(u => u.Id == usuarioId, update, cancellationToken: cancellationToken);
        if (result.MatchedCount == 0) return NotFound(new { mensaje = "Usuario no encontrado." });

        return NoContent();
    }

    [HttpDelete("token")]
    public async Task<IActionResult> EliminarToken([FromBody] TokenFcmDto dto, CancellationToken cancellationToken)
    {
        var usuarioId = ObtenerUsuarioId();
        if (usuarioId is null) return Unauthorized();

        var update = Builders<Usuario>.Update.Pull(u => u.TokensFcm, dto.Token.Trim());
        await _context.Usuarios.UpdateOneAsync(u => u.Id == usuarioId, update, cancellationToken: cancellationToken);
        return NoContent();
    }

    [HttpPost("prueba")]
    public async Task<IActionResult> EnviarPrueba([FromBody] TokenFcmDto dto, CancellationToken cancellationToken)
    {
        var usuarioId = ObtenerUsuarioId();
        if (usuarioId is null) return Unauthorized();

        var token = dto.Token.Trim();
        var perteneceAlUsuario = await _context.Usuarios
            .Find(u => u.Id == usuarioId && u.TokensFcm.Contains(token))
            .AnyAsync(cancellationToken);
        if (!perteneceAlUsuario) return BadRequest(new { mensaje = "El token FCM no está registrado para el usuario autenticado." });

        var idMensaje = await _fcmNotificationService.EnviarAsync(token, "Prueba BeatWatch", "Notificación FCM configurada correctamente.", cancellationToken);
        return Ok(new { mensaje = "Notificación de prueba enviada.", idMensaje });
    }

    private string? ObtenerUsuarioId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
}
