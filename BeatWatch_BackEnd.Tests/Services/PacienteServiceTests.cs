using BeatWatch_BackEnd.Data;
using BeatWatch_BackEnd.Dtos;
using BeatWatch_BackEnd.Models;
using BeatWatch_BackEnd.Services;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;

namespace BeatWatch_BackEnd.Tests.Services;

public class PacienteServiceTests
{
    [Fact]
    public async Task RegistrarPacienteAsync_IdLicenciaInvalido_RechazaAntesDeInsertarUsuario()
    {
        var usuarios = new Mock<IMongoCollection<Usuario>>();
        var contexto = new Mock<MongoDbContext>();
        contexto.SetupGet(c => c.Usuarios).Returns(usuarios.Object);
        var servicio = new PacienteService(contexto.Object);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => servicio.RegistrarPacienteAsync(new CrearPacienteDto
        {
            NombreCompleto = "Paciente prueba",
            Correo = "paciente@example.com",
            IdLicencia = "licencia-invalida"
        }));

        Assert.Equal("El IdLicencia proporcionado no tiene un formato válido.", exception.Message);
        usuarios.Verify(c => c.InsertOneAsync(
            It.IsAny<Usuario>(),
            It.IsAny<InsertOneOptions>(),
            It.IsAny<CancellationToken>()), Times.Never);
        contexto.VerifyGet(c => c.Usuarios, Times.Never);
    }

    [Fact]
    public async Task CrearPerfilAsync_UsuarioIdInvalido_RechazaAntesDeConsultasOInserciones()
    {
        var contexto = new Mock<MongoDbContext>();
        var servicio = new PacienteService(contexto.Object);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => servicio.CrearPerfilAsync(new CrearPerfilPacienteDto
        {
            UsuarioId = "usuario-invalido",
            CURP = "ABCD010101HDFRRL01",
            TipoSangre = "O+"
        }));

        Assert.Equal("El UsuarioId no tiene un formato de ObjectId válido.", exception.Message);
        contexto.VerifyGet(c => c.Usuarios, Times.Never);
        contexto.VerifyGet(c => c.Pacientes, Times.Never);
        contexto.VerifyGet(c => c.Licencias, Times.Never);
    }

    [Fact]
    public async Task RegistrarPacienteAsync_SinLicencia_InsertaPacienteActivoConRolYTokenDeNueveDigitos()
    {
        Usuario? usuarioInsertado = null;
        var cursor = new Mock<IAsyncCursor<BsonDocument>>(MockBehavior.Strict);
        cursor.Setup(c => c.MoveNext(It.IsAny<CancellationToken>())).Returns(false);
        cursor.Setup(c => c.MoveNextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        cursor.Setup(c => c.Current).Returns(Array.Empty<BsonDocument>());
        cursor.Setup(c => c.Dispose());

        var usuarios = new Mock<IMongoCollection<Usuario>>(MockBehavior.Strict);
        usuarios.Setup(c => c.FindAsync<BsonDocument>(
                It.IsAny<FilterDefinition<Usuario>>(),
                It.IsAny<FindOptions<Usuario, BsonDocument>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cursor.Object);
        usuarios.Setup(c => c.InsertOneAsync(
                It.IsAny<Usuario>(),
                It.IsAny<InsertOneOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<Usuario, InsertOneOptions, CancellationToken>((usuario, _, _) => usuarioInsertado = usuario)
            .Returns(Task.CompletedTask);

        var contexto = new Mock<MongoDbContext>();
        contexto.SetupGet(c => c.Usuarios).Returns(usuarios.Object);
        var servicio = new PacienteService(contexto.Object);

        var resultado = await servicio.RegistrarPacienteAsync(new CrearPacienteDto
        {
            NombreCompleto = "Paciente prueba",
            Correo = "paciente@example.com",
            Telefono = "5512345678"
        });

        Assert.Same(usuarioInsertado, resultado);
        Assert.NotNull(usuarioInsertado);
        Assert.Equal("Paciente prueba", usuarioInsertado.Nombre);
        Assert.Equal("paciente@example.com", usuarioInsertado.Correo);
        Assert.Equal("5512345678", usuarioInsertado.Telefono);
        Assert.Equal("Paciente", usuarioInsertado.Rol);
        Assert.True(usuarioInsertado.Activo);
        Assert.Matches("^[0-9]{9}$", usuarioInsertado.TokenMovil!);
        usuarios.Verify(c => c.InsertOneAsync(
            usuarioInsertado,
            It.IsAny<InsertOneOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
        contexto.VerifyGet(c => c.Licencias, Times.Never);
    }
}
