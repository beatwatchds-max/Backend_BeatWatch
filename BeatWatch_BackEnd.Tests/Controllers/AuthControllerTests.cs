using BeatWatch_BackEnd.Controllers;
using BeatWatch_BackEnd.infrescture;
using BeatWatch_BackEnd.Models;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace BeatWatch_Back_End.Tests.Controllers // ◄ Namespace apuntando a Controllers
{
    public class AuthControllerTests
    {
        private readonly Mock<IUsuarioService> _mockUsuarioService;
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            // Creamos el mock del servicio que requiere el controlador
            _mockUsuarioService = new Mock<IUsuarioService>();

            // Instanciamos el controlador inyectándole el mock
            _controller = new AuthController(_mockUsuarioService.Object);
        }

        [Fact]
        public async Task Registrar_FlujoExitoso_Retorna201CreatedConUsuario()
        {
            // Arrange
            var request = new RegistroRequest
            {
                Nombre = "Tobi Salinas",
                Correo = "tobi@test.com",
                Contrasena = "contrasena123"
            };

            var usuarioCreado = new Usuario
            {
                Id = "65f1a2b3c4d5e6f7a8b9c0d1",
                Nombre = request.Nombre,
                Correo = request.Correo,
                Activo = true
            };

            // Configuramos el servicio simulado para que devuelva el usuario con su ID de Mongo
            _mockUsuarioService.Setup(s => s.RegistrarAsync(request))
                .ReturnsAsync(usuarioCreado);

            // Act
            var response = await _controller.Registrar(request);

            // Assert
            // Comprobamos que el resultado sea de tipo CreatedAtActionResult (HTTP 201)
            var actionResult = Assert.IsType<CreatedAtActionResult>(response.Result);
            Assert.Equal(201, actionResult.StatusCode);

            // Validamos que el objeto devuelto en el body sea el usuario correcto
            var usuarioRetornado = Assert.IsType<Usuario>(actionResult.Value);
            Assert.Equal(usuarioCreado.Id, usuarioRetornado.Id);
            Assert.Equal(usuarioCreado.Correo, usuarioRetornado.Correo);
        }

        [Fact]
        public async Task Registrar_CorreoDuplicado_Retorna409ConflictConMensaje()
        {
            // Arrange
            var request = new RegistroRequest
            {
                Nombre = "Juan Clón",
                Correo = "duplicado@test.com",
                Contrasena = "password"
            };

            // Simulamos que el servicio arroja la excepción de negocio que tú programaste
            _mockUsuarioService.Setup(s => s.RegistrarAsync(request))
                .ThrowsAsync(new InvalidOperationException("El correo ya está registrado."));

            // Act
            var response = await _controller.Registrar(request);

            // Assert
            // Comprobamos que la respuesta HTTP sea un ConflictObjectResult (HTTP 409)
            var actionResult = Assert.IsType<ConflictObjectResult>(response.Result);
            Assert.Equal(409, actionResult.StatusCode);
        }
    }
}