using BeatWatch_BackEnd.Dtos;

namespace BeatWatch_BackEnd.Services
{
    public interface IAlertaService
    {
        Task<AlertaResponseDto> RegistrarAlertaAsync(string idDispositivoIdentificador, CrearAlertaDto dto);
    }
}