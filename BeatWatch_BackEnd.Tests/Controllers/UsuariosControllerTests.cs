using BeatWatch_BackEnd.Controllers;
using BeatWatch_BackEnd.Dtos;
using BeatWatch_BackEnd.infrescture;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace BeatWatch_BackEnd.Tests.Controllers;

public class UsuariosControllerTests
{
    private readonly Mock<IUsuarioService> _usuarioService = new();

    [Fact]
    public async Task BorradoLogico_UsuarioExistente_Retorna204()
    {
        const string usuarioId = "65f1a2b3c4d5e6f7a8b9c0d1";
        _usuarioService.Setup(s => s.DesactivarAsync(usuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var controller = new UsuariosController(_usuarioService.Object);

        var resultado = await controller.BorradoLogico(usuarioId, CancellationToken.None);

        Assert.IsType<NoContentResult>(resultado);
    }

    [Fact]
    public async Task BorradoLogico_UsuarioInexistente_Retorna404()
    {
        const string usuarioId = "65f1a2b3c4d5e6f7a8b9c0d1";
        _usuarioService.Setup(s => s.DesactivarAsync(usuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var controller = new UsuariosController(_usuarioService.Object);

        var resultado = await controller.BorradoLogico(usuarioId, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(resultado);
    }

    [Fact]
    public async Task ActualizarCuidadores_ListaValida_Retorna204()
    {
        const string usuarioId = "65f1a2b3c4d5e6f7a8b9c0d1";
        var request = new ActualizarCuidadoresDto
        {
            Cuidadores = ["65f1a2b3c4d5e6f7a8b9c0d2"]
        };
        _usuarioService.Setup(s => s.ActualizarCuidadoresAsync(usuarioId, request.Cuidadores, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var controller = new UsuariosController(_usuarioService.Object);

        var resultado = await controller.ActualizarCuidadores(usuarioId, request, CancellationToken.None);

        Assert.IsType<NoContentResult>(resultado);
    }

    [Fact]
    public async Task ActualizarCuidadores_UsuarioInexistente_Retorna404()
    {
        const string usuarioId = "65f1a2b3c4d5e6f7a8b9c0d1";
        var request = new ActualizarCuidadoresDto();
        _usuarioService.Setup(s => s.ActualizarCuidadoresAsync(usuarioId, request.Cuidadores, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var controller = new UsuariosController(_usuarioService.Object);

        var resultado = await controller.ActualizarCuidadores(usuarioId, request, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(resultado);
    }

    [Fact]
    public async Task ActualizarCuidadores_IdentificadorInvalido_Retorna400()
    {
        var request = new ActualizarCuidadoresDto();
        _usuarioService.Setup(s => s.ActualizarCuidadoresAsync("invalido", request.Cuidadores, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Formato inválido."));
        var controller = new UsuariosController(_usuarioService.Object);

        var resultado = await controller.ActualizarCuidadores("invalido", request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(resultado);
    }

    [Fact]
    public async Task DesvincularCuidador_IdentificadorInvalido_Retorna400()
    {
        const string usuarioId = "65f1a2b3c4d5e6f7a8b9c0d1";
        const string cuidadorId = "invalido";
        _usuarioService.Setup(s => s.DesvincularCuidadorAsync(usuarioId, cuidadorId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("El identificador no tiene un formato válido."));
        var controller = new UsuariosController(_usuarioService.Object);

        var resultado = await controller.DesvincularCuidador(usuarioId, cuidadorId, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(resultado);
    }

    [Fact]
    public async Task DesvincularCuidador_UsuarioExistente_Retorna204()
    {
        const string usuarioId = "65f1a2b3c4d5e6f7a8b9c0d1";
        const string cuidadorId = "65f1a2b3c4d5e6f7a8b9c0d2";
        _usuarioService.Setup(s => s.DesvincularCuidadorAsync(usuarioId, cuidadorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var controller = new UsuariosController(_usuarioService.Object);

        var resultado = await controller.DesvincularCuidador(usuarioId, cuidadorId, CancellationToken.None);

        Assert.IsType<NoContentResult>(resultado);
    }

    [Fact]
    public async Task BorradoLogico_IdentificadorInvalido_Retorna400()
    {
        _usuarioService.Setup(s => s.DesactivarAsync("invalido", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Formato inválido."));
        var controller = new UsuariosController(_usuarioService.Object);

        var resultado = await controller.BorradoLogico("invalido", CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(resultado);
    }
}
