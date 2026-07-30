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

    /// <summary>
    /// Formulario manual de la App / Expediente Clínico (Registra la patología/condición de arritmia)
    /// </summary>
    [HttpPost("arritmia")]
    public async Task<IActionResult> RegistrarArritmia(
        [FromBody] RegistrarArritmiaDto solicitud,
        CancellationToken cancellationToken)
    {
        await _saludService.RegistrarArritmiaAsync(solicitud, cancellationToken);
        return StatusCode(StatusCodes.Status201Created);
    }

    /// <summary>
    /// Alerta enviada por el Wearable en tiempo real (Episodio/Pico anormal en reposo)
    /// </summary>
    [HttpPost("episodio-arritmia")]
    public async Task<IActionResult> RegistrarAlertaFrecuencia(
        [FromBody] RegistrarAlertaFrecuenciaDto solicitud,
        CancellationToken cancellationToken)
    {
        await _saludService.RegistrarAlertaFrecuenciaAsync(solicitud, cancellationToken);
        return StatusCode(StatusCodes.Status201Created);
    }
}