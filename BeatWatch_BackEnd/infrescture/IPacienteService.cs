using BeatWatch_BackEnd.Dtos;
using BeatWatch_BackEnd.Models;

namespace BeatWatch_BackEnd.infrescture;

public interface IPacienteService
{
    Task<Usuario> RegistrarPacienteAsync(CrearPacienteDto pacienteDto);
    Task<Paciente> CrearPerfilAsync(CrearPerfilPacienteDto perfilDto);
    Task<Paciente?> ObtenerPorUsuarioIdAsync(string usuarioId);
}
