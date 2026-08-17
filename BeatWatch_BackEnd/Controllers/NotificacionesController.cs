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

        if (string.IsNullOrWhiteSpace(dto.Token) || string.IsNullOrWhiteSpace(dto.DeviceId) || string.IsNullOrWhiteSpace(dto.DeviceType))
        {
            return BadRequest(new { mensaje = "token, deviceId y deviceType son obligatorios." });
        }

        var token = dto.Token.Trim();
        var limpiarTokenDuplicado = Builders<Usuario>.Update
            .Set(u => u.FcmToken, null)
            .Set(u => u.FcmDeviceId, null)
            .Set(u => u.FcmTokenActualizadoEn, null);
        await _context.Usuarios.UpdateManyAsync(u => u.Id != usuarioId && u.FcmToken == token, limpiarTokenDuplicado, cancellationToken: cancellationToken);

        var update = Builders<Usuario>.Update
            .Set(u => u.FcmToken, token)
            .Set(u => u.FcmDeviceId, dto.DeviceId.Trim())
            .Set(u => u.FcmTokenActualizadoEn, DateTime.UtcNow);
        UpdateResult result;
        try
        {
            result = await _context.Usuarios.UpdateOneAsync(u => u.Id == usuarioId, update, cancellationToken: cancellationToken);
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            return Conflict(new { mensaje = "El token FCM ya fue registrado por otra sesión." });
        }
        if (result.MatchedCount == 0) return NotFound(new { mensaje = "Usuario no encontrado." });

        return NoContent();
    }

    // Ruta temporal: retirar o limitar antes de producción.
    [HttpPost("prueba")]
    public async Task<IActionResult> EnviarPrueba(CancellationToken cancellationToken)
    {
        var usuarioId = ObtenerUsuarioId();
        if (usuarioId is null) return Unauthorized();

        var usuario = await _context.Usuarios.Find(u => u.Id == usuarioId).FirstOrDefaultAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(usuario?.FcmToken)) return BadRequest(new { mensaje = "El usuario autenticado no tiene un token FCM registrado." });

        var idMensaje = await _fcmNotificationService.EnviarAsync(usuario.FcmToken, "Prueba BeatWatch", "Notificación FCM configurada correctamente.", new Dictionary<string, string> { ["tipo"] = "prueba" }, cancellationToken);
        return Ok(new { mensaje = "Notificación de prueba enviada.", idMensaje });
    }

    private string? ObtenerUsuarioId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
}
