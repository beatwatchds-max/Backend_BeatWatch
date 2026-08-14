using BeatWatch_BackEnd.Controllers;
using BeatWatch_BackEnd.Models;
using BeatWatch_BackEnd.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace BeatWatch_Back_End.Tests.Controllers;

public class LicenciasControllerTests
{
    private const string UsuarioId = "65f1a2b3c4d5e6f7a8b9c0d1";
    private const string CorreoPrueba = "usuario@beatwatch.com";
    private readonly Mock<ILicenciaService> _service = new();

    [Fact]
    public async Task ActivarLicenciaGratuita_DtoValido_RetornaOk()
    {
        // Arrange
        var dto = new ActivarLicenciaGratuitaDto { CorreoElectronico = CorreoPrueba };
        _service.Setup(s => s.ActivarLicenciaGratuitaAsync(It.IsAny<ActivarLicenciaGratuitaDto>()))
                .ReturnsAsync(new Licencia { UsuarioId = UsuarioId });

        var controller = new LicenciasController(_service.Object);

        // Act
        var response = await controller.ActivarLicenciaGratuita(dto);

        // Assert
        Assert.IsType<OkObjectResult>(response);
        _service.Verify(s => s.ActivarLicenciaGratuitaAsync(dto), Times.Once);
    }

    [Fact]
    public async Task ActivarLicenciaGratuita_UsuarioNoExiste_RetornaBadRequest()
    {
        // Arrange
        var dto = new ActivarLicenciaGratuitaDto { CorreoElectronico = "noexiste@beatwatch.com" };
        _service.Setup(s => s.ActivarLicenciaGratuitaAsync(It.IsAny<ActivarLicenciaGratuitaDto>()))
                .ThrowsAsync(new ArgumentException("El usuario proporcionado no existe en el sistema."));

        var controller = new LicenciasController(_service.Object);

        // Act
        var response = await controller.ActivarLicenciaGratuita(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(response);
    }

    [Fact]
    public async Task ActivarLicenciaGratuita_LicenciaExistente_Retorna409()
    {
        // Arrange
        var dto = new ActivarLicenciaGratuitaDto { CorreoElectronico = CorreoPrueba };
        _service.Setup(s => s.ActivarLicenciaGratuitaAsync(It.IsAny<ActivarLicenciaGratuitaDto>()))
                .ThrowsAsync(new InvalidOperationException("El usuario ya tiene una licencia gratuita activa."));

        var controller = new LicenciasController(_service.Object);

        // Act
        var response = await controller.ActivarLicenciaGratuita(dto);

        // Assert
        Assert.IsType<ConflictObjectResult>(response);
    }
}