using BeatWatch_BackEnd.Dtos;
using BeatWatch_BackEnd.Models;

namespace BeatWatch_BackEnd.infrescture;

public interface ISaludService
{
    Task<Arritmia> RegistrarArritmiaAsync(RegistrarArritmiaDto solicitud, CancellationToken cancellationToken);
    Task<IReadOnlyList<Arritmia>> ObtenerHistorialArritmiasAsync(string idPaciente, CancellationToken cancellationToken);
}
