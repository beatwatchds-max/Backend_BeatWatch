using BeatWatch_BackEnd.Dtos;
using BeatWatch_BackEnd.DTOs;
using BeatWatch_BackEnd.Models;

namespace BeatWatch_BackEnd.infrescture;

public interface IPacienteService
{
    Task<Usuario> RegistrarPacienteAsync(CrearPacienteDto pacienteDto, string idLicencia);

    Task<Paciente> CrearPerfilAsync(CrearPerfilPacienteDto perfilDto);
    Task<DetallePacienteResponseDto?> ObtenerDetallePorPacienteIdAsync(string pacienteId);
 
    Task<bool> ActualizarPerfilPacienteAsync(string usuarioId, ActualizarPerfilPacienteDto dto);
    Task<(Usuario Usuario, Paciente Paciente)> RegistrarPacienteCompletoAsync(
    RegistrarPacienteCompletoDto dto,
    string idLicencia
);
    Task<DetallePacienteResponseDto?> ObtenerDetallePorUsuarioIdAsync(string usuarioId);
}
