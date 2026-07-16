using BeatWatch_BackEnd.Models;

namespace BeatWatch_BackEnd.Services
{
    public interface ILicenciaService
    {
        Task<Licencia?> ProcesarPagoYCrearLicenciaAsync(PagoSimuladoDto pagoDto);
    }
}