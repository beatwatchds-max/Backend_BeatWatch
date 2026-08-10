using System.Security.Claims;
using BeatWatch_BackEnd.Data;
using BeatWatch_BackEnd.infrescture;
using MongoDB.Bson;
using MongoDB.Driver;

namespace BeatWatch_BackEnd.Services;

public sealed class PacienteAccessService : IPacienteAccessService
{
    private readonly MongoDbContext _context;

    public PacienteAccessService(MongoDbContext context) => _context = context;

    public async Task<bool> PuedeAccederAsync(ClaimsPrincipal usuario, string idPaciente)
    {
        if (!ObjectId.TryParse(idPaciente, out _)) return false;

        var usuarioId = usuario.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? usuario.FindFirst("sub")?.Value;
        var licenciaId = usuario.FindFirst("idLicencia")?.Value ?? usuario.FindFirst("LicenciaId")?.Value;
        if (string.IsNullOrWhiteSpace(usuarioId) || string.IsNullOrWhiteSpace(licenciaId)) return false;

        var paciente = await _context.Pacientes.Find(p => p.Id == idPaciente && p.IdLicencia == licenciaId).FirstOrDefaultAsync();
        if (paciente is null) return false;

        return paciente.UsuarioId == usuarioId
            || usuario.IsInRole("Administrador")
            || usuario.IsInRole("Cuidador");
    }
}
