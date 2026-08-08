using BeatWatch_BackEnd.Controllers;
using BeatWatch_BackEnd.Dtos;
using BeatWatch_BackEnd.infrescture;
using BeatWatch_BackEnd.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace BeatWatch_BackEnd.Tests.Controllers;

public class PacientesControllerTests
{
    private readonly Mock<IPacienteService> _service = new();

    [Fact]
    public async Task RegistrarPaciente_SolicitudValida_Retorna200()
    {
        var dto = new CrearPacienteDto { NombreCompleto = "Paciente", Correo = "paciente@test.com" };
        _service.Setup(s => s.RegistrarPacienteAsync(dto, "65f1a2b3c4d5e6f7a8b9c0d2")).ReturnsAsync(new Usuario { Id = "65f1a2b3c4d5e6f7a8b9c0d1", TokenMovil = "123456789" });

        Assert.IsType<OkObjectResult>(await CrearController().RegistrarPaciente(dto));
    }

    [Fact]
    public async Task RegistrarPaciente_ErrorInterno_Retorna500()
    {
        var dto = new CrearPacienteDto { NombreCompleto = "Paciente", Correo = "paciente@test.com" };
        _service.Setup(s => s.RegistrarPacienteAsync(dto, "65f1a2b3c4d5e6f7a8b9c0d2")).ThrowsAsync(new InvalidOperationException());

        var result = Assert.IsType<ObjectResult>(await CrearController().RegistrarPaciente(dto));
        Assert.Equal(StatusCodes.Status500InternalServerError, result.StatusCode);
    }

    [Fact]
    public async Task CrearPerfil_SolicitudValida_Retorna201()
    {
        var dto = CrearPerfil();
        _service.Setup(s => s.CrearPerfilAsync(dto)).ReturnsAsync(new Paciente { Id = "65f1a2b3c4d5e6f7a8b9c0d3" });

        var result = Assert.IsType<ObjectResult>(await CrearController().CrearPerfilPaciente(dto));
        Assert.Equal(StatusCodes.Status201Created, result.StatusCode);
    }

    [Fact]
    public async Task CrearPerfil_CurpDuplicada_Retorna409()
    {
        var dto = CrearPerfil();
        _service.Setup(s => s.CrearPerfilAsync(dto)).ThrowsAsync(new InvalidOperationException());

        Assert.IsType<ConflictObjectResult>(await CrearController().CrearPerfilPaciente(dto));
    }

    [Fact]
    public async Task CrearPerfil_DatosInvalidos_Retorna400()
    {
        var dto = CrearPerfil();
        _service.Setup(s => s.CrearPerfilAsync(dto)).ThrowsAsync(new ArgumentException());

        Assert.IsType<BadRequestObjectResult>(await CrearController().CrearPerfilPaciente(dto));
    }

    private PacientesController CrearController()
    {
        var controller = new PacientesController(_service.Object);
        controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("idLicencia", "65f1a2b3c4d5e6f7a8b9c0d2")]))
        };
        return controller;
    }

    private static CrearPerfilPacienteDto CrearPerfil() => new()
    {
        UsuarioId = "65f1a2b3c4d5e6f7a8b9c0d1",
        CURP = "ABCD010101HDFABC01",
        Edad = 25,
        Sexo = "Masculino",
        Peso = 70,
        Estatura = 170,
        TipoSangre = "O+",
        IdLicencia = "65f1a2b3c4d5e6f7a8b9c0d2"
    };
}
