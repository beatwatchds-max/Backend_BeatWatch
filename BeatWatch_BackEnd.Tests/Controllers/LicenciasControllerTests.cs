using System.Security.Claims;
using BeatWatch_BackEnd.Controllers;
using BeatWatch_BackEnd.Models;
using BeatWatch_BackEnd.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace BeatWatch_Back_End.Tests.Controllers;

public class LicenciasControllerTests
{
    private const string UsuarioId = "65f1a2b3c4d5e6f7a8b9c0d1";
    private readonly Mock<ILicenciaService> _service = new();

    [Fact]
    public async Task ActivarLicenciaGratuita_UsaLaIdentidadAutenticada()
    {
        _service.Setup(s => s.ActivarLicenciaGratuitaAsync(UsuarioId)).ReturnsAsync(new Licencia { UsuarioId = UsuarioId });

        var response = await CrearController(UsuarioId).ActivarLicenciaGratuita();

        Assert.IsType<OkObjectResult>(response);
        _service.Verify(s => s.ActivarLicenciaGratuitaAsync(UsuarioId), Times.Once);
    }

    [Fact]
    public async Task ActivarLicenciaGratuita_RechazaSesionSinIdentidad()
    {
        var controller = new LicenciasController(_service.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        Assert.IsType<UnauthorizedResult>(await controller.ActivarLicenciaGratuita());
        _service.Verify(s => s.ActivarLicenciaGratuitaAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ActivarLicenciaGratuita_LicenciaExistente_Retorna409()
    {
        _service.Setup(s => s.ActivarLicenciaGratuitaAsync(UsuarioId)).ThrowsAsync(new InvalidOperationException());

        Assert.IsType<ConflictObjectResult>(await CrearController(UsuarioId).ActivarLicenciaGratuita());
    }

    private LicenciasController CrearController(string usuarioId)
    {
        var controller = new LicenciasController(_service.Object);
        controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, usuarioId)]))
        };
        return controller;
    }
}
