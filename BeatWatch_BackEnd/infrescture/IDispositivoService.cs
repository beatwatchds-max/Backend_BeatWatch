using BeatWatch_BackEnd.Dtos;
using BeatWatch_BackEnd.Models;

namespace BeatWatch_BackEnd.infrescture
{
    public interface IDispositivoService
    {
        Task<SesionEmparejamientoResponseDto> CrearSesionEmparejamientoAsync(CrearSesionEmparejamientoDto dto);
        Task<Dispositivo> EmparejarDispositivoAsync(EmparejarDispositivoDto dto);
        Task<object> ObtenerEstadoEmparejamientoAsync(string idSesion, string watchSecret);
       // Task<List<Dispositivo>> ObtenerDispositivosPorPacienteAsync(string? idPaciente);
        Task<bool> ActualizarAliasAsync(string id, string nuevoAlias);
        Task<bool> EliminarDispositivoAsync(string id);
        Task<bool> ActualizarMetricasAsync(string idDispositivo, ActualizarMetricasWearableDto dto);
        Task<Dispositivo?> ObtenerDispositivoAsync(string idDispositivo);
        Task<bool> SolicitarMedicionAsync(string idDispositivo);
        Task<object> ObtenerComandosAsync(string idDispositivo, string watchAccessToken);
        Task<List<Dispositivo>> ObtenerDispositivosPorLicenciaAsync(string idLicencia);
        Task<List<Dispositivo>> ObtenerDispositivosPorPacienteAsync(string? idPaciente);
    }
}
