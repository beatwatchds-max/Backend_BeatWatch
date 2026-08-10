using System.Security.Claims;

namespace BeatWatch_BackEnd.infrescture;

public interface IPacienteAccessService
{
    Task<bool> PuedeAccederAsync(ClaimsPrincipal usuario, string idPaciente);
}
