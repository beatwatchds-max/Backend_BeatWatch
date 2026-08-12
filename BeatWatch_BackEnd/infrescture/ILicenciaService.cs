using BeatWatch_BackEnd.Dtos.licencia;
using BeatWatch_BackEnd.Models;

namespace BeatWatch_BackEnd.Services
{
    public interface ILicenciaService
    {
        Task<Licencia?> ProcesarPagoYCrearLicenciaAsync(ActivarLicenciaGratuitaDto dto);
    }
}