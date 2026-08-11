using BeatWatch_BackEnd.infrescture;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BeatWatch_BackEnd.Dtos.arritmia;

namespace BeatWatch_BackEnd.Controllers;

[ApiController]
[Route("api/salud")]
[Authorize]
public class SaludController : ControllerBase
{
    private readonly ISaludService _saludService;
    private readonly IPacienteAccessService _pacienteAccessService;

    public SaludController(ISaludService saludService, IPacienteAccessService pacienteAccessService)
    {
        _saludService = saludService;
        _pacienteAccessService = pacienteAccessService;
    }

    /// <summary>
    /// Formulario manual de la App / Expediente Clínico (Registra la patología/condición de arritmia)
    /// </summary>
    [HttpPost("arritmia")]
    public async Task<IActionResult> RegistrarArritmia([FromBody] RegistrarArritmiaDto solicitud, CancellationToken cancellationToken)
    {
        if (!await _pacienteAccessService.PuedeAccederAsync(User, solicitud.IdPaciente)) return Forbid();
        await _saludService.RegistrarArritmiaAsync(solicitud, cancellationToken);
        return StatusCode(StatusCodes.Status201Created);
    }

    /// <summary>
    /// Alerta enviada por el Wearable en tiempo real (Episodio/Pico anormal en reposo)
    /// </summary>
    [HttpPost("episodio-arritmia")]
    public async Task<IActionResult> RegistrarAlertaFrecuencia([FromBody] RegistrarAlertaFrecuenciaDto solicitud,CancellationToken cancellationToken)
    {
        if (!await _pacienteAccessService.PuedeAccederAsync(User, solicitud.IdPaciente)) return Forbid();
        await _saludService.RegistrarAlertaFrecuenciaAsync(solicitud, cancellationToken);
        return StatusCode(StatusCodes.Status201Created);
    }

    /// <summary>
    /// HU6.2: Sincronización Diaria de Actividad Física y Sueño desde la App Móvil/Wearable
    /// </summary>
    [HttpPost("actividad")]
    public async Task<IActionResult> RegistrarActividadDiaria([FromBody] RegistrarActividadDiariaDto solicitud,CancellationToken cancellationToken)
    {
        if (!await _pacienteAccessService.PuedeAccederAsync(User, solicitud.IdPaciente)) return Forbid();
        await _saludService.RegistrarActividadDiariaAsync(solicitud, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new { mensaje = "Actividad diaria registrada con éxito." });
    }
}
