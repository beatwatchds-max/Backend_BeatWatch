using BeatWatch_BackEnd.Controllers;
using BeatWatch_BackEnd.Models;
using BeatWatch_BackEnd.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace BeatWatch_Back_End.Tests.Controllers
{
    public class LicenciasControllerTests
    {
        private readonly Mock<ILicenciaService> _mockLicenciaService;
        private readonly LicenciasController _controller;

        public LicenciasControllerTests()
        {
            _mockLicenciaService = new Mock<ILicenciaService>();
            _controller = new LicenciasController(_mockLicenciaService.Object);
        }

        [Fact]
        public async Task ProcesarPagoSimulado_PagoExitoso_Retorna200OkConLicencia()
        {
            // Arrange
            var dto = new PagoSimuladoDto
            {
                UsuarioId = "65f1a2b3c4d5e6f7a8b9c0d1",
                TipoLicencia = "Grupal",
                MetodoPago = "Tarjeta",
                CorreoElectronico = "tobi@test.com"
            };

            var licenciaCreada = new Licencia
            {
                Id = "6a5716986f71b40415aa13d3",
                UsuarioId = dto.UsuarioId,
                Tipo = dto.TipoLicencia,
                MetodoPago = dto.MetodoPago,
                EstadoPago = "Aprobado",
                Activa = true
            };

            _mockLicenciaService.Setup(s => s.ProcesarPagoYCrearLicenciaAsync(dto))
                .ReturnsAsync(licenciaCreada);

            // Act
            var response = await _controller.ProcesarPagoSimulado(dto);

            // Assert
            var actionResult = Assert.IsType<OkObjectResult>(response);
            Assert.Equal(200, actionResult.StatusCode);
        }

        [Fact]
        public async Task ProcesarPagoSimulado_TipoInvalido_Retorna400BadRequest()
        {
            // Arrange
            var dto = new PagoSimuladoDto { TipoLicencia = "Invalida", MetodoPago = "OXXO" };

            _mockLicenciaService.Setup(s => s.ProcesarPagoYCrearLicenciaAsync(dto))
                .ThrowsAsync(new ArgumentException("Tipo de licencia no soportado."));

            // Act
            var response = await _controller.ProcesarPagoSimulado(dto);

            // Assert
            var actionResult = Assert.IsType<BadRequestObjectResult>(response);
            Assert.Equal(400, actionResult.StatusCode);
        }

        [Fact]
        public async Task ProcesarPagoSimulado_ServicioRetornaNull_Retorna400BadRequest()
        {
            var dto = new PagoSimuladoDto { TipoLicencia = "Individual", MetodoPago = "OXXO" };
            _mockLicenciaService.Setup(s => s.ProcesarPagoYCrearLicenciaAsync(dto)).ReturnsAsync((Licencia?)null);

            var response = await _controller.ProcesarPagoSimulado(dto);

            Assert.IsType<BadRequestObjectResult>(response);
        }

        [Fact]
        public async Task ProcesarPagoSimulado_ErrorInesperado_Retorna500()
        {
            var dto = new PagoSimuladoDto { TipoLicencia = "Individual", MetodoPago = "OXXO" };
            _mockLicenciaService.Setup(s => s.ProcesarPagoYCrearLicenciaAsync(dto))
                .ThrowsAsync(new InvalidOperationException("database unavailable"));

            var response = await _controller.ProcesarPagoSimulado(dto);

            Assert.IsType<ObjectResult>(response);
            Assert.Equal(500, ((ObjectResult)response).StatusCode);
        }
    }
}
