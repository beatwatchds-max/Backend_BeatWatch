using BeatWatch_BackEnd.Models;
using BeatWatch_BackEnd.Services;
using BeatWatch_BackEnd.Data;
using Moq;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace BeatWatch_BackEnd.Tests.Services
{
    public class ReporteServiceTests
    {
        private readonly Mock<IMongoCollection<Licencia>> _mockLicenciasCollection;
        private readonly Mock<IMongoCollection<Usuario>> _mockUsuariosCollection;
        private readonly Mock<MongoDbContext> _mockContext;
        private readonly IReporteService _service;

        public ReporteServiceTests()
        {
            _mockLicenciasCollection = new Mock<IMongoCollection<Licencia>>();
            _mockUsuariosCollection = new Mock<IMongoCollection<Usuario>>();

            _mockContext = new Mock<MongoDbContext>();
            _mockContext.Setup(c => c.Licencias).Returns(_mockLicenciasCollection.Object);
            _mockContext.Setup(c => c.Usuarios).Returns(_mockUsuariosCollection.Object);

            _service = new ReporteService(_mockContext.Object);
        }

        [Fact]
        public async Task GenerarPdfReciboAsync_LicenciaYUsuarioExisten_RetornaPdfBytesValido()
        {
            // Arrange
            string licenciaId = "65f1a2b3c4d5e6f7a8b9c0d1";
            string usuarioId = "65f1a2b3c4d5e6f7a8b9c999";

            var licenciaFalsa = new Licencia
            {
                Id = licenciaId,
                UsuarioId = usuarioId,
                Tipo = "Grupal",
                CodigoGrupo = "BW-GR-TEST12",
                MetodoPago = "Tarjeta",
                EstadoPago = "Aprobado",
                FechaInicio = DateTime.UtcNow,
                FechaFin = DateTime.UtcNow.AddMonths(1)
            };

            var usuarioFalso = new Usuario
            {
                Id = usuarioId,
                Nombre = "Oscar Salinas",
                Correo = "oscar@test.com",
                EmpresaOrganizacion = "BeatWatch Inc",
                RFC = "XAXX010101000"
            };

            // 1. Mockear el cursor de la Licencia
            var mockLicenciaCursor = new Mock<IAsyncCursor<Licencia>>();
            mockLicenciaCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            mockLicenciaCursor.Setup(c => c.Current).Returns(new List<Licencia> { licenciaFalsa });

            _mockLicenciasCollection.Setup(c => c.FindAsync(
                    It.IsAny<FilterDefinition<Licencia>>(),
                    It.IsAny<FindOptions<Licencia, Licencia>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockLicenciaCursor.Object);

            // 2. Mockear el cursor del Usuario
            var mockUsuarioCursor = new Mock<IAsyncCursor<Usuario>>();
            mockUsuarioCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            mockUsuarioCursor.Setup(c => c.Current).Returns(new List<Usuario> { usuarioFalso });

            _mockUsuariosCollection.Setup(c => c.FindAsync(
                    It.IsAny<FilterDefinition<Usuario>>(),
                    It.IsAny<FindOptions<Usuario, Usuario>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockUsuarioCursor.Object);

            // Act
            var result = await _service.GenerarPdfReciboAsync(licenciaId);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Length > 0, "El PDF generado no debería estar vacío.");

            // Verificación mágica: Todo archivo PDF válido arranca con los caracteres binarios %PDF
            string cabeceraPdf = Encoding.UTF8.GetString(result, 0, 4);
            Assert.Equal("%PDF", cabeceraPdf);
        }

        [Fact]
        public async Task GenerarPdfReciboAsync_LicenciaNoExiste_LanzaKeyNotFoundException()
        {
            // Arrange
            string licenciaIdInexistente = "id_falso_que_no_esta_en_mongo";

            // El cursor devuelve vacía la lista de licencias
            var mockLicenciaCursor = new Mock<IAsyncCursor<Licencia>>();
            mockLicenciaCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            mockLicenciaCursor.Setup(c => c.Current).Returns(new List<Licencia>());

            _mockLicenciasCollection.Setup(c => c.FindAsync(
                    It.IsAny<FilterDefinition<Licencia>>(),
                    It.IsAny<FindOptions<Licencia, Licencia>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockLicenciaCursor.Object);

            // Act & Assert
            var excepcion = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.GenerarPdfReciboAsync(licenciaIdInexistente)
            );

            Assert.Equal("La licencia especificada no existe.", excepcion.Message);
        }
    }
}