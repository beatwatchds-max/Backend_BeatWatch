namespace BeatWatch_BackEnd.Services
{
    public interface IReporteService
    {
        Task<byte[]> GenerarPdfReciboAsync(string licenciaId);
        Task<bool> UsuarioPuedeDescargarReciboAsync(string licenciaId, string usuarioId, bool esAdministrador);
    }
}
