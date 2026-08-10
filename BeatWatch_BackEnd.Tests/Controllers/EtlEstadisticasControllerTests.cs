using BeatWatch_BackEnd.Controllers;
using BeatWatch_BackEnd.infrescture;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace BeatWatch_BackEnd.Tests.Controllers;

public class EtlEstadisticasControllerTests
{
    private readonly Mock<IEstadisticaService> _service = new();
    private readonly Mock<IPacienteAccessService> _accessService = new();

    [Fact]
    public async Task ObtenerGraficaBpm_DiasFueraDeRango_Retorna400SinConsultarServicio()
    {
        var result = await CrearController().ObtenerGraficaBpm("paciente", dias: 91);

        Assert.IsType<BadRequestObjectResult>(result);
        _service.Verify(s => s.ObtenerGraficaBpmAsync(
            It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task ObtenerEstadisticas_RangoDeFechasInvalido_Retorna400SinConsultarServicio()
    {
        var result = await CrearController().ObtenerEstadisticasPaciente(
            "paciente", new DateTime(2026, 2, 1), new DateTime(2026, 1, 1));

        Assert.IsType<BadRequestObjectResult>(result);
        _service.Verify(s => s.ObtenerEstadisticasPorPacienteAsync(
            It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>()), Times.Never);
    }

    private EtlEstadisticasController CrearController()
    {
        _accessService.Setup(s => s.PuedeAccederAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>(), It.IsAny<string>())).ReturnsAsync(true);
        return new(_service.Object, _accessService.Object);
    }
}
