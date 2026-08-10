using BeatWatch_BackEnd.Controllers;
using BeatWatch_BackEnd.Dtos;
using BeatWatch_BackEnd.infrescture;
using BeatWatch_BackEnd.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace BeatWatch_BackEnd.Tests.Controllers;

public class DispositivosControllerTests
{
    private readonly Mock<IDispositivoService> _service = new();
    private readonly Mock<IPacienteAccessService> _accessService = new();

    [Fact]
    public async Task Emparejar_SolicitudValida_Retorna201()
    {
        var dto = CrearDispositivo();
        _service.Setup(s => s.EmparejarDispositivoAsync(dto)).ReturnsAsync(new Dispositivo());

        var result = await CrearController().Emparejar(dto);

        Assert.Equal(StatusCodes.Status201Created, Assert.IsType<ObjectResult>(result).StatusCode);
    }

    [Fact]
    public async Task Emparejar_ErrorDeValidacion_Retorna400()
    {
        var dto = CrearDispositivo();
        _service.Setup(s => s.EmparejarDispositivoAsync(dto)).ThrowsAsync(new ArgumentException());

        Assert.IsType<BadRequestObjectResult>(await CrearController().Emparejar(dto));
    }

    [Fact]
    public async Task ObtenerDispositivos_IdentificadorInvalido_Retorna400()
    {
        _service.Setup(s => s.ObtenerDispositivosPorPacienteAsync("invalido")).ThrowsAsync(new ArgumentException());

        Assert.IsType<BadRequestObjectResult>(await CrearController().ObtenerDispositivos("invalido"));
    }

    [Fact]
    public async Task ObtenerDispositivos_SolicitudValida_Retorna200()
    {
        const string idPaciente = "65f1a2b3c4d5e6f7a8b9c0d1";
        _service.Setup(s => s.ObtenerDispositivosPorPacienteAsync(idPaciente)).ReturnsAsync([]);

        Assert.IsType<OkObjectResult>(await CrearController().ObtenerDispositivos(idPaciente));
    }

    [Fact]
    public async Task ActualizarAlias_DispositivoExistente_Retorna200()
    {
        const string id = "65f1a2b3c4d5e6f7a8b9c0d1";
        var dto = new ActualizarAliasDto { Alias = " Reloj " };
        PrepararDispositivo(id);
        _service.Setup(s => s.ActualizarAliasAsync(id, dto.Alias)).ReturnsAsync(true);

        Assert.IsType<OkObjectResult>(await CrearController().ActualizarAlias(id, dto));
    }

    [Fact]
    public async Task ActualizarAlias_DispositivoInexistente_Retorna404()
    {
        const string id = "65f1a2b3c4d5e6f7a8b9c0d1";
        PrepararDispositivo(id);
        _service.Setup(s => s.ActualizarAliasAsync(id, It.IsAny<string>())).ReturnsAsync(false);

        Assert.IsType<NotFoundObjectResult>(await CrearController().ActualizarAlias(id, new ActualizarAliasDto { Alias = "Reloj" }));
    }

    [Fact]
    public async Task ActualizarAlias_IdentificadorInvalido_Retorna400()
    {
        _service.Setup(s => s.ObtenerDispositivoAsync("invalido")).ThrowsAsync(new ArgumentException());
        _service.Setup(s => s.ActualizarAliasAsync("invalido", It.IsAny<string>())).ThrowsAsync(new ArgumentException());

        Assert.IsType<BadRequestObjectResult>(await CrearController().ActualizarAlias("invalido", new ActualizarAliasDto { Alias = "Reloj" }));
    }

    [Fact]
    public async Task EliminarDispositivo_IdentificadorInvalido_Retorna400()
    {
        _service.Setup(s => s.ObtenerDispositivoAsync("invalido")).ThrowsAsync(new ArgumentException());
        _service.Setup(s => s.EliminarDispositivoAsync("invalido")).ThrowsAsync(new ArgumentException());

        Assert.IsType<BadRequestObjectResult>(await CrearController().EliminarDispositivo("invalido"));
    }

    [Fact]
    public async Task EliminarDispositivo_Existente_Retorna200()
    {
        const string id = "65f1a2b3c4d5e6f7a8b9c0d1";
        PrepararDispositivo(id);
        _service.Setup(s => s.EliminarDispositivoAsync(id)).ReturnsAsync(true);

        Assert.IsType<OkObjectResult>(await CrearController().EliminarDispositivo(id));
    }

    [Fact]
    public async Task EliminarDispositivo_Inexistente_Retorna404()
    {
        const string id = "65f1a2b3c4d5e6f7a8b9c0d1";
        _service.Setup(s => s.EliminarDispositivoAsync(id)).ReturnsAsync(false);

        Assert.IsType<NotFoundObjectResult>(await CrearController().EliminarDispositivo(id));
    }

    private DispositivosController CrearController()
    {
        _accessService.Setup(s => s.PuedeAccederAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>(), It.IsAny<string>())).ReturnsAsync(true);
        return new(_service.Object, _accessService.Object);
    }

    private void PrepararDispositivo(string id) => _service.Setup(s => s.ObtenerDispositivoAsync(id))
        .ReturnsAsync(new Dispositivo { Id = id, IdPaciente = "65f1a2b3c4d5e6f7a8b9c0d1" });

    private static EmparejarDispositivoDto CrearDispositivo() => new()
    {
        IdSesion = "sesion-001",
        TokenEmparejamiento = "token-001",
        Alias = "Reloj",
        IdPaciente = "65f1a2b3c4d5e6f7a8b9c0d1"
    };
}
