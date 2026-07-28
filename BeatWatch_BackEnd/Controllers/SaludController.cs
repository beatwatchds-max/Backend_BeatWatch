using BeatWatch_BackEnd.Dtos;
using BeatWatch_BackEnd.infrescture;
using Microsoft.AspNetCore.Mvc;

namespace BeatWatch_BackEnd.Controllers;

[ApiController]
[Route("api/salud")]
public class SaludController : ControllerBase
{
    private readonly ISaludService _saludService;

    public SaludController(ISaludService saludService)
    {
        _saludService = saludService;
    }

    [HttpPost("arritmia")]
    public async Task<IActionResult> RegistrarArritmia(
        [FromBody] RegistrarArritmiaDto solicitud,
        CancellationToken cancellationToken)
    {
        await _saludService.RegistrarArritmiaAsync(solicitud, cancellationToken);
        return StatusCode(StatusCodes.Status201Created);
    }
}
