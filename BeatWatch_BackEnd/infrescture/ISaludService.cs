using BeatWatch_BackEnd.Dtos.arritmia;
using BeatWatch_BackEnd.Dtos.historial;
using BeatWatch_BackEnd.Models;

namespace BeatWatch_BackEnd.infrescture;

public interface ISaludService
{
    Task<Arritmia> RegistrarArritmiaAsync(RegistrarArritmiaDto solicitud, CancellationToken cancellationToken);
    Task<EpisodioArritmia> RegistrarAlertaFrecuenciaAsync(RegistrarAlertaFrecuenciaDto solicitud, CancellationToken cancellationToken);
    Task RegistrarActividadDiariaAsync(RegistrarActividadDiariaDto solicitud, CancellationToken cancellationToken);
    Task<IReadOnlyList<EpisodioArritmia>> ObtenerHistorialArritmiasAsync(string idPaciente, CancellationToken cancellationToken);
    Task<ResumenTableroDto> ObtenerResumenTableroAsync(string idPaciente, int dias, CancellationToken cancellationToken);
}
