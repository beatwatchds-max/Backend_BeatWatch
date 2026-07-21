using BeatWatch_BackEnd.Controllers;
using BeatWatch_BackEnd.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace BeatWatch_Back_End.Tests.Controllers // ◄ Ubicado correctamente en tu estructura de carpetas
{
    public class ReportesControllerTests
    {
        private readonly Mock<IReporteService> _mockReporteService;
        private readonly ReportesController _controller;

        public ReportesControllerTests()
        {
            // Simulamos el servicio que el controlador necesita
            _mockReporteService = new Mock<IReporteService>();

            // Inyectamos el mock al controlador real
            _controller = new ReportesController(_mockReporteService.Object);
        }

        [Fact]
        public async Task DescargarRecibo_LicenciaExiste_RetornaFileStreamPdf()
        {
            // Arrange
            string licenciaId = "65f1a2b3c4d5e6f7a8b9c0d1";
            byte[] pdfFalsoBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // Mock binario de cabecera PDF (%PDF)

            _mockReporteService.Setup(s => s.GenerarPdfReciboAsync(licenciaId))
                .ReturnsAsync(pdfFalsoBytes);

            // Act
            var response = await _controller.DescargarRecibo(licenciaId);

            // Assert
            // Comprobamos que el controlador responda un archivo binario directo (FileContentResult)
            var fileResult = Assert.IsType<FileContentResult>(response);

            // Validamos que el tipo MIME sea exactamente el de un PDF
            Assert.Equal("application/pdf", fileResult.ContentType);

            // Validamos que el nombre de descarga asignado coincida con tu formato
            Assert.Equal($"Recibo_BeatWatch_{licenciaId}.pdf", fileResult.FileDownloadName);
        }

        [Fact]
        public async Task DescargarRecibo_LicenciaNoExiste_Retorna404NotFound()
        {
            // Arrange
            string licenciaIdInexistente = "id_no_existente_en_atlas";

            // Forzamos al servicio simulado a lanzar la excepción exacta que atrapa tu catch
            _mockReporteService.Setup(s => s.GenerarPdfReciboAsync(licenciaIdInexistente))
                .ThrowsAsync(new KeyNotFoundException("La licencia especificada no existe."));

            // Act
            var response = await _controller.DescargarRecibo(licenciaIdInexistente);

            // Assert
            // Comprobamos que la respuesta HTTP mapee a un 404 (NotFoundObjectResult)
            var actionResult = Assert.IsType<NotFoundObjectResult>(response);
            Assert.Equal(404, actionResult.StatusCode);
        }

        [Fact]
        public async Task DescargarRecibo_ErrorInesperado_Retorna500()
        {
            const string licenciaId = "65f1a2b3c4d5e6f7a8b9c0d1";
            _mockReporteService.Setup(s => s.GenerarPdfReciboAsync(licenciaId))
                .ThrowsAsync(new InvalidOperationException("pdf engine failed"));

            var response = await _controller.DescargarRecibo(licenciaId);

            var result = Assert.IsType<ObjectResult>(response);
            Assert.Equal(500, result.StatusCode);
        }
    }
}
