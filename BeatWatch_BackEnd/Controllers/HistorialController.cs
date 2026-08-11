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
    private readonly IPacienteAccessService _pacienteAccessService;

    public HistorialController(ISaludService saludService, IPacienteAccessService pacienteAccessService)
    {
        _saludService = saludService;
        _pacienteAccessService = pacienteAccessService;
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerArritmias([FromQuery, Required, RegularExpression("^[a-fA-F0-9]{24}$")] string idPaciente, CancellationToken cancellationToken)
    {
        if (!await _pacienteAccessService.PuedeAccederAsync(User, idPaciente)) return Forbid();
        var arritmias = await _saludService.ObtenerHistorialArritmiasAsync(idPaciente, cancellationToken);
        return Ok(arritmias);
    }
}
