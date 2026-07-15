namespace BeatWatch_BackEnd.Services
{
    public interface IReporteService
    {
        Task<byte[]> GenerarPdfReciboAsync(string licenciaId);
    }
}