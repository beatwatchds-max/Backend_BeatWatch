using BeatWatch_BackEnd.Models;
using BeatWatch_BackEnd.Services;
using BeatWatch_BackEnd.Data;
using Moq;
using MongoDB.Driver;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace BeatWatch_Back_End.Tests
{
    public class UsuarioServiceTests
    {
        private readonly Mock<IMongoCollection<Usuario>> _mockCollection;
        private readonly Mock<MongoDbContext> _mockContext;
        private readonly IUsuarioService _service;

        public UsuarioServiceTests()
        {
            _mockCollection = new Mock<IMongoCollection<Usuario>>();
            _mockContext = new Mock<MongoDbContext>(MockBehavior.Strict);
            _mockContext.Setup(c => c.Usuarios).Returns(_mockCollection.Object);
            _service = new UsuarioService(_mockContext.Object);
        }

        [Fact]
        public async Task RegistrarAsync_CreaUsuarioCuandoCorreoNoExiste()
        {
            // Arrange: la búsqueda retorna null
            var mockFindFluent = new Mock<IAsyncCursor<Usuario>>();
            mockFindFluent.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
                .Returns(false);
            mockFindFluent.Setup(c => c.Current).Returns(new List<Usuario>());
            _mockCollection.Setup(c => c.FindAsync(It.IsAny<FilterDefinition<Usuario>>(), It.IsAny<FindOptions<Usuario, Usuario>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockFindFluent.Object);

            var request = new RegistroRequest
            {
                Nombre = "Juan",
                Correo = "juan@example.com",
                Telefono = "123456789",
                Contrasena = "Password123"
            };

            // Act
            var result = await _service.RegistrarAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(request.Nombre, result.Nombre);
            Assert.Equal(request.Correo, result.Correo);
            Assert.NotEqual(request.Contrasena, result.Contrasena); // hash should differ
            _mockCollection.Verify(c => c.InsertOneAsync(It.IsAny<Usuario>(), null, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RegistrarAsync_LanzaErrorCuandoCorreoDuplicado()
        {
            // Arrange: la búsqueda devuelve un usuario existente
            var existingUser = new Usuario { Correo = "juan@example.com" };
            var mockFindFluent = new Mock<IAsyncCursor<Usuario>>();
            mockFindFluent.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
                .Returns(true);
            mockFindFluent.Setup(c => c.Current).Returns(new List<Usuario> { existingUser });
            _mockCollection.Setup(c => c.FindAsync(It.IsAny<FilterDefinition<Usuario>>(), It.IsAny<FindOptions<Usuario, Usuario>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockFindFluent.Object);

            var request = new RegistroRequest
            {
                Nombre = "Juan",
                Correo = "juan@example.com",
                Telefono = "123456789",
                Contrasena = "Password123"
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.RegistrarAsync(request));
        }
    }
}
