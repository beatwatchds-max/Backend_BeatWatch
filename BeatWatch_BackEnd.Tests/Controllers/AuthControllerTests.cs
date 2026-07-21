using BeatWatch_BackEnd.Controllers;
using BeatWatch_BackEnd.infrescture;
using BeatWatch_BackEnd.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace BeatWatch_Back_End.Tests.Controllers // ◄ Namespace apuntando a Controllers
{
    public class AuthControllerTests
    {
        private readonly Mock<IUsuarioService> _mockUsuarioService;
        private readonly Mock<ITokenService> _mockTokenService;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ILogger<AuthController>> _mockLogger;
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            // Creamos el mock del servicio que requiere el controlador
            _mockUsuarioService = new Mock<IUsuarioService>();
            _mockTokenService = new Mock<ITokenService>();
            _mockEmailService = new Mock<IEmailService>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<AuthController>>();

            // Instanciamos el controlador inyectándole el mock
            _controller = new AuthController(
                _mockUsuarioService.Object,
                _mockTokenService.Object,
                _mockEmailService.Object,
                _mockConfiguration.Object,
                _mockLogger.Object);
            _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
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
            var actionResult = Assert.IsType<CreatedAtActionResult>(response);
            Assert.Equal(201, actionResult.StatusCode);
            Assert.NotNull(actionResult.Value);
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
            var actionResult = Assert.IsType<ConflictObjectResult>(response);
            Assert.Equal(409, actionResult.StatusCode);
        }

        [Fact]
        public async Task Login_CredencialesValidas_RetornaToken()
        {
            var request = new LoginWebRequest { Correo = "user@beatwatch.test", Contrasena = "Password123!" };
            var usuario = new Usuario { Id = "65f1a2b3c4d5e6f7a8b9c0d1", Correo = request.Correo, Nombre = "User" };
            var token = new LoginResponse { AccessToken = "jwt-token", ExpiresIn = 900 };
            _mockUsuarioService.Setup(s => s.AutenticarAsync(request.Correo, request.Contrasena)).ReturnsAsync(usuario);
            _mockTokenService.Setup(s => s.CreateAccessToken(usuario)).Returns(token);

            var response = await _controller.Login(request, CancellationToken.None);

            var result = Assert.IsType<OkObjectResult>(response.Result);
            Assert.Same(token, result.Value);
        }

        [Fact]
        public async Task Login_CredencialesInvalidas_Retorna401()
        {
            var request = new LoginWebRequest { Correo = "user@beatwatch.test", Contrasena = "incorrecta" };
            _mockUsuarioService.Setup(s => s.AutenticarAsync(request.Correo, request.Contrasena)).ReturnsAsync((Usuario?)null);

            var response = await _controller.Login(request, CancellationToken.None);

            Assert.IsType<UnauthorizedObjectResult>(response.Result);
            _mockTokenService.Verify(s => s.CreateAccessToken(It.IsAny<Usuario>()), Times.Never);
        }

        [Fact]
        public async Task SolicitarRestablecimiento_TokenDisponible_EnviaCorreoYRetorna202()
        {
            const string email = "user@beatwatch.test";
            _mockUsuarioService.Setup(s => s.CrearTokenRestablecimientoAsync(email, It.IsAny<CancellationToken>())).ReturnsAsync("secure-token");
            _mockConfiguration.Setup(c => c["EmailSettings:PasswordResetUrl"]).Returns("https://app.beatwatch.test/reset");

            var response = await _controller.SolicitarRestablecimiento(new SolicitarRestablecimientoRequest { Correo = email }, CancellationToken.None);

            Assert.IsType<AcceptedResult>(response);
            _mockEmailService.Verify(s => s.SendPasswordResetAsync(email, "https://app.beatwatch.test/reset?token=secure-token", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SolicitarRestablecimiento_ErrorDeCorreo_NoExponeErrorYRetorna202()
        {
            const string email = "user@beatwatch.test";
            _mockUsuarioService.Setup(s => s.CrearTokenRestablecimientoAsync(email, It.IsAny<CancellationToken>())).ReturnsAsync("secure-token");
            _mockConfiguration.Setup(c => c["EmailSettings:PasswordResetUrl"]).Returns("https://app.beatwatch.test/reset");
            _mockEmailService.Setup(s => s.SendPasswordResetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("smtp unavailable"));

            var response = await _controller.SolicitarRestablecimiento(new SolicitarRestablecimientoRequest { Correo = email }, CancellationToken.None);

            Assert.IsType<AcceptedResult>(response);
        }

        [Fact]
        public async Task SolicitarRestablecimiento_UsuarioInexistente_NoEnviaCorreoYRetorna202()
        {
            const string email = "missing@beatwatch.test";
            _mockUsuarioService.Setup(s => s.CrearTokenRestablecimientoAsync(email, It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);

            var response = await _controller.SolicitarRestablecimiento(new SolicitarRestablecimientoRequest { Correo = email }, CancellationToken.None);

            Assert.IsType<AcceptedResult>(response);
            _mockEmailService.Verify(s => s.SendPasswordResetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task SolicitarRestablecimiento_UrlConQuery_UsaSeparadorAmpersand()
        {
            const string email = "user@beatwatch.test";
            _mockUsuarioService.Setup(s => s.CrearTokenRestablecimientoAsync(email, It.IsAny<CancellationToken>())).ReturnsAsync("token with spaces");
            _mockConfiguration.Setup(c => c["EmailSettings:PasswordResetUrl"]).Returns("https://app.beatwatch.test/reset?source=email");

            await _controller.SolicitarRestablecimiento(new SolicitarRestablecimientoRequest { Correo = email }, CancellationToken.None);

            _mockEmailService.Verify(s => s.SendPasswordResetAsync(
                email,
                "https://app.beatwatch.test/reset?source=email&token=token%20with%20spaces",
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RestablecerContrasena_TokenValido_Retorna204()
        {
            var request = new RestablecerContrasenaRequest { Token = "token", Contrasena = "Password123!" };
            _mockUsuarioService.Setup(s => s.RestablecerContrasenaAsync(request.Token, request.Contrasena, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var response = await _controller.RestablecerContrasena(request, CancellationToken.None);

            Assert.IsType<NoContentResult>(response);
        }

        [Fact]
        public async Task RestablecerContrasena_TokenInvalido_Retorna400()
        {
            var request = new RestablecerContrasenaRequest { Token = "token", Contrasena = "Password123!" };
            _mockUsuarioService.Setup(s => s.RestablecerContrasenaAsync(request.Token, request.Contrasena, It.IsAny<CancellationToken>())).ReturnsAsync(false);

            var response = await _controller.RestablecerContrasena(request, CancellationToken.None);

            Assert.IsType<BadRequestObjectResult>(response);
        }
    }
}
