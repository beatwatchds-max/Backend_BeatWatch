using BeatWatch_BackEnd.Controllers;
using BeatWatch_BackEnd.Dtos.arritmia;
using BeatWatch_BackEnd.infrescture;
using BeatWatch_BackEnd.Models;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace BeatWatch_BackEnd.Tests.Controllers;

public class SaludControllerTests
{
    [Fact]
    public async Task RegistrarArritmia_SolicitudValida_RegistraYRetorna201()
    {
        var service = new Mock<ISaludService>();
        var solicitud = CrearSolicitud();
        service.Setup(s => s.RegistrarArritmiaAsync(solicitud, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Arritmia());
        var accessService = new Mock<IPacienteAccessService>();
        accessService.Setup(s => s.PuedeAccederAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>(), solicitud.IdPaciente)).ReturnsAsync(true);
        var controller = new SaludController(service.Object, accessService.Object);

        var resultado = await controller.RegistrarArritmia(solicitud, CancellationToken.None);

        var creado = Assert.IsType<StatusCodeResult>(resultado);
        Assert.Equal(201, creado.StatusCode);
        service.Verify(s => s.RegistrarArritmiaAsync(solicitud, CancellationToken.None), Times.Once);
    }

    private static RegistrarArritmiaDto CrearSolicitud() => new()
    {
        Tipo = "Taquicardia",
        FrecuenciaCardiaca = 140,
        DuracionEpisodioSeconds = 30,
        IdPaciente = "65f1a2b3c4d5e6f7a8b9c0d1",
        Sintomas = new SintomasDto(),
        FactoresRiesgo = new FactoresRiesgoDto()
    };
}
