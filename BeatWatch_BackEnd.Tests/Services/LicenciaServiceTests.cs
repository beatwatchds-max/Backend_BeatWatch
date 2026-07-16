using BeatWatch_BackEnd.Models;
using BeatWatch_BackEnd.Services;
using BeatWatch_BackEnd.Data;
using Moq;
using MongoDB.Driver;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace BeatWatch_BackEnd.Tests.Services
{
    public class LicenciaServiceTests
    {
        private readonly Mock<IMongoCollection<Licencia>> _mockLicenciasCollection;
        private readonly Mock<IMongoCollection<Usuario>> _mockUsuariosCollection;
        private readonly Mock<MongoDbContext> _mockContext;
        private readonly ILicenciaService _service;

        public LicenciaServiceTests()
        {
            _mockLicenciasCollection = new Mock<IMongoCollection<Licencia>>();
            _mockUsuariosCollection = new Mock<IMongoCollection<Usuario>>();

            // Contexto en modo "Loose" por defecto para evitar MockExceptions
            _mockContext = new Mock<MongoDbContext>();

            // Configuramos ambas propiedades de tu MongoDbContext real
            _mockContext.Setup(c => c.Licencias).Returns(_mockLicenciasCollection.Object);
            _mockContext.Setup(c => c.Usuarios).Returns(_mockUsuariosCollection.Object);

            _service = new LicenciaService(_mockContext.Object);
        }

        [Fact]
        public async Task ProcesarPagoYCrearLicenciaAsync_PagoTarjeta_CreaLicenciaActivaYActivaUsuario()
        {
            // Arrange
            var request = new PagoSimuladoDto
            {
                UsuarioId = "65f1a2b3c4d5e6f7a8b9c0d1",
                TipoLicencia = "Grupal",
                MetodoPago = "Tarjeta",
                CorreoElectronico = "oscar@test.com"
            };

            // Act
            var result = await _service.ProcesarPagoYCrearLicenciaAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Grupal", result.Tipo);
            Assert.Equal("Tarjeta", result.MetodoPago);
            Assert.Equal("Aprobado", result.EstadoPago);
            Assert.True(result.Activa);
            Assert.StartsWith("BW-GR-", result.CodigoGrupo); // Valida que se genere el código de grupo

            // Verificar que se insertó la licencia en la colección
            _mockLicenciasCollection.Verify(c => c.InsertOneAsync(
                It.IsAny<Licencia>(),
                null,
                It.IsAny<CancellationToken>()),
                Times.Once);

            // Verificar que SÍ se actualizó el estado del usuario en la base de datos
            _mockUsuariosCollection.Verify(c => c.UpdateOneAsync(
                It.IsAny<FilterDefinition<Usuario>>(),
                It.IsAny<UpdateDefinition<Usuario>>(),
                null,
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ProcesarPagoYCrearLicenciaAsync_PagoOxxo_CreaLicenciaInactivaYNoModificaUsuario()
        {
            // Arrange
            var request = new PagoSimuladoDto
            {
                UsuarioId = "65f1a2b3c4d5e6f7a8b9c0d2",
                TipoLicencia = "Individual",
                MetodoPago = "OXXO",
                CorreoElectronico = "oscar@test.com"
            };

            // Act
            var result = await _service.ProcesarPagoYCrearLicenciaAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Individual", result.Tipo);
            Assert.Equal("OXXO", result.MetodoPago);
            Assert.Equal("Pendiente", result.EstadoPago);
            Assert.False(result.Activa); // En OXXO la licencia queda inactiva al inicio

            // Verificar que se insertó el documento de la licencia
            _mockLicenciasCollection.Verify(c => c.InsertOneAsync(
                It.IsAny<Licencia>(),
                null,
                It.IsAny<CancellationToken>()),
                Times.Once);

            // Verificar que NO se mandó a actualizar al usuario a activo (Times.Never)
            _mockUsuariosCollection.Verify(c => c.UpdateOneAsync(
                It.IsAny<FilterDefinition<Usuario>>(),
                It.IsAny<UpdateDefinition<Usuario>>(),
                null,
                It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ProcesarPagoYCrearLicenciaAsync_TipoLicenciaInvalido_LanzaArgumentException()
        {
            // Arrange
            var request = new PagoSimuladoDto
            {
                UsuarioId = "65f1a2b3c4d5e6f7a8b9c0d3",
                TipoLicencia = "PlanHackeado", // Tipo inválido
                MetodoPago = "Tarjeta",
                CorreoElectronico = "oscar@test.com"
            };

            // Act & Assert
            var excepcion = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.ProcesarPagoYCrearLicenciaAsync(request)
            );

            Assert.Equal("Tipo de licencia no soportado.", excepcion.Message);
        }
    }
}