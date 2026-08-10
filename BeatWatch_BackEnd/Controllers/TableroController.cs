using BeatWatch_BackEnd.Dtos;
using BeatWatch_BackEnd.infrescture;
using BeatWatch_BackEnd.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace BeatWatch_BackEnd.Controllers;

[ApiController]
[Route("api/tablero")]
[Authorize]
public class TableroController : ControllerBase
{
    private readonly ISaludService _saludService;
    private readonly IPacienteAccessService _pacienteAccessService;

    public TableroController(ISaludService saludService, IPacienteAccessService pacienteAccessService)
    {
        _saludService = saludService;
        _pacienteAccessService = pacienteAccessService;
    }

    /// <summary>
    /// HU6.3: Consolidado para la pantalla de Dashboard/Reportes
    /// </summary>
    /// <param name="idPaciente">ID del paciente a consultar</param>
    /// <param name="dias">Número de días hacia atrás (por defecto 7 días)</param>
    [HttpGet("resumen")]
    public async Task<IActionResult> ObtenerResumenTablero(
        [FromQuery, Required, RegularExpression("^[a-fA-F0-9]{24}$")] string idPaciente,
        [FromQuery] int dias = 7,
        CancellationToken cancellationToken = default)
    {
        if (!await _pacienteAccessService.PuedeAccederAsync(User, idPaciente)) return Forbid();
        var resumen = await _saludService.ObtenerResumenTableroAsync(idPaciente, dias, cancellationToken);
        return Ok(resumen);
    }
}
