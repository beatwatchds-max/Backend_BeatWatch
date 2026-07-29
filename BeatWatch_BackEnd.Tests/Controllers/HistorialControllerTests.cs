using BeatWatch_BackEnd.Controllers;
using BeatWatch_BackEnd.infrescture;
using BeatWatch_BackEnd.Models;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace BeatWatch_BackEnd.Tests.Controllers;

public class HistorialControllerTests
{
    [Fact]
    public async Task ObtenerArritmias_RetornaHistorialDelPaciente()
    {
        const string idPaciente = "65f1a2b3c4d5e6f7a8b9c0d1";
        var historial = new List<Arritmia> { new() { IdPaciente = idPaciente } };
        var service = new Mock<ISaludService>();
        service.Setup(s => s.ObtenerHistorialArritmiasAsync(idPaciente, It.IsAny<CancellationToken>()))
            .ReturnsAsync(historial);
        var controller = new HistorialController(service.Object);

        var resultado = await controller.ObtenerArritmias(idPaciente, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(resultado);
        Assert.Same(historial, ok.Value);
        service.Verify(s => s.ObtenerHistorialArritmiasAsync(idPaciente, CancellationToken.None), Times.Once);
    }
}
