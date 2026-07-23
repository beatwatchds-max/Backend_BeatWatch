using BeatWatch_BackEnd.Models;
using BeatWatch_BackEnd.Services;
using BeatWatch_BackEnd.Data;
using Moq;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using BeatWatch_BackEnd.infrescture;

namespace BeatWatch_BackEnd.Tests.Services
{
    public class UsuarioServiceTests
    {
        private readonly Mock<IMongoCollection<Usuario>> _mockCollection;
        private readonly Mock<MongoDbContext> _mockContext;
        private readonly IUsuarioService _service;

        public UsuarioServiceTests()
        {
            _mockCollection = new Mock<IMongoCollection<Usuario>>();

            // CAMBIA ESTA LÍNEA (Quita el MockBehavior.Strict o déjalo vacío)
            _mockContext = new Mock<MongoDbContext>();

            _mockContext.Setup(c => c.Usuarios).Returns(_mockCollection.Object);
            _service = new UsuarioService(_mockContext.Object);
        }

        [Fact]
        public async Task RegistrarAsync_LanzaErrorCuandoCorreoDuplicado()
        {
            // Arrange: Simulamos que la base de datos SÍ encuentra un usuario con ese correo
            var existingUser = new Usuario { Correo = "juan@example.com" };
            var mockCursor = new Mock<IAsyncCursor<Usuario>>();

            // CORRECCIÓN: MoveNextAsync debe dar true una vez para entregar el usuario existente
            mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);

            mockCursor.Setup(c => c.Current).Returns(new List<Usuario> { existingUser });

            _mockCollection.Setup(c => c.FindAsync(
                    It.IsAny<FilterDefinition<Usuario>>(),
                    It.IsAny<FindOptions<Usuario, Usuario>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockCursor.Object);

            var request = new RegistroRequest
            {
                Nombre = "Juan Clón",
                Correo = "juan@example.com", // Mismo correo que el existente
                Telefono = "987654321",
                Contrasena = "Password123"
            };

            // Act & Assert: Validamos que lance la excepción con tu mensaje exacto
            var excepcion = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.RegistrarAsync(request)
            );

            Assert.Equal("El correo ya está registrado.", excepcion.Message);
        }
    }
}
