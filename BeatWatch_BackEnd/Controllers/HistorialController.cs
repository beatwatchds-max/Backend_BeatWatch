using System.ComponentModel.DataAnnotations;
using BeatWatch_BackEnd.infrescture;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeatWatch_BackEnd.Controllers;

[ApiController]
[Route("api/historial")]
[Authorize(Roles = "Administrador,Cuidador,Paciente")]
public class HistorialController : ControllerBase
{
    private readonly ISaludService _saludService;

    public HistorialController(ISaludService saludService)
    {
        _saludService = saludService;
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerArritmias(
        [FromQuery, Required, RegularExpression("^[a-fA-F0-9]{24}$")] string idPaciente,
        CancellationToken cancellationToken)
    {
        var arritmias = await _saludService.ObtenerHistorialArritmiasAsync(idPaciente, cancellationToken);
        return Ok(arritmias);
    }
}
