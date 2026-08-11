using BeatWatch_BackEnd.Dtos;
using BeatWatch_BackEnd.Models;

namespace BeatWatch_BackEnd.infrescture
{
    public interface IDispositivoService
    {
        Task<SesionEmparejamientoResponseDto> CrearSesionEmparejamientoAsync(CrearSesionEmparejamientoDto dto);
        Task<Dispositivo> EmparejarDispositivoAsync(EmparejarDispositivoDto dto);
        Task<object> ObtenerEstadoEmparejamientoAsync(string idSesion, string watchSecret);
       
        Task<bool> ActualizarAliasAsync(string id, string nuevoAlias);
        Task<bool> EliminarDispositivoAsync(string id);
  
        Task<Dispositivo?> ObtenerDispositivoAsync(string idDispositivo);

        Task<List<Dispositivo>> ObtenerDispositivosPorLicenciaAsync(string idLicencia);
        Task<List<Dispositivo>> ObtenerDispositivosPorPacienteAsync(string? idPaciente);
    }
}
