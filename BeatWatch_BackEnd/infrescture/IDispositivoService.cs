using BeatWatch_BackEnd.Dtos;
using BeatWatch_BackEnd.Models;

namespace BeatWatch_BackEnd.infrescture;

public interface IDispositivoService
{
    Task<Dispositivo> EmparejarDispositivoAsync(EmparejarDispositivoDto dto);
    Task<List<Dispositivo>> ObtenerDispositivosPorPacienteAsync(string? idPaciente);
    Task<bool> ActualizarAliasAsync(string id, string nuevoAlias);
    Task<bool> EliminarDispositivoAsync(string id);
}
