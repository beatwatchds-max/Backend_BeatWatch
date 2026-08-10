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
            Correo = "paciente@example.com"
        }, "licencia-invalida"));

        Assert.Equal("La licencia del usuario autenticado no es válida.", exception.Message);
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
    public async Task RegistrarPacienteAsync_LicenciaInvalida_RechazaAntesDeConsultarLaBaseDeDatos()
    {
        var contexto = new Mock<MongoDbContext>();
        var servicio = new PacienteService(contexto.Object);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => servicio.RegistrarPacienteAsync(new CrearPacienteDto
        {
            NombreCompleto = "Paciente prueba",
            Correo = "paciente@example.com",
            Telefono = "5512345678"
        }, string.Empty));

        Assert.Equal("La licencia del usuario autenticado no es válida.", exception.Message);
        contexto.VerifyGet(c => c.Usuarios, Times.Never);
        contexto.VerifyGet(c => c.Licencias, Times.Never);
    }
}
