using BeatWatch_BackEnd.Dtos;

namespace BeatWatch_BackEnd.infrescture
{
    public interface IMedicionService
    {
        Task<string> RegistrarMedicionAsync(string idDispositivoIdentificador, RegistrarMedicionDto dto);
        Task<List<MedicionResponseDto>> ObtenerHistorialPacienteAsync(string idPaciente, DateTime? desde, DateTime? hasta, int limite);
    }
}