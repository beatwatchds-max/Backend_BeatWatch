using BeatWatch_BackEnd.Data;
using BeatWatch_BackEnd.Dtos;
using BeatWatch_BackEnd.Models;
using BeatWatch_BackEnd.Services;
using MongoDB.Driver;
using Moq;

namespace BeatWatch_BackEnd.Tests.Services;

public class DispositivoServiceTests
{
    [Fact]
    public async Task EmparejarDispositivoAsync_IdPacienteInvalido_RechazaAntesDeConsultarOPersistir()
    {
        var context = new Mock<MongoDbContext>();
        var service = new DispositivoService(context.Object);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.EmparejarDispositivoAsync(new EmparejarDispositivoDto
        {
            IdPaciente = "invalido",
            NumeroSerie = " serie-001 ",
            Alias = " Reloj "
        }));

        Assert.Equal("El identificador del paciente no tiene un formato válido.", exception.Message);
        context.VerifyGet(c => c.Dispositivos, Times.Never);
    }

    [Theory]
    [InlineData("invalido")]
    [InlineData("")]
    public async Task ActualizarAliasAsync_IdInvalido_RechazaAntesDeActualizar(string id)
    {
        var context = new Mock<MongoDbContext>();
        var service = new DispositivoService(context.Object);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.ActualizarAliasAsync(id, "Alias"));

        Assert.Equal("El identificador del dispositivo no tiene un formato válido.", exception.Message);
        context.VerifyGet(c => c.Dispositivos, Times.Never);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(0, false)]
    public async Task ActualizarAliasAsync_ResultadoMongo_ReflejaDocumentosCoincidentes(long matchedCount, bool expected)
    {
        var result = new Mock<UpdateResult>();
        result.SetupGet(r => r.MatchedCount).Returns(matchedCount);
        var collection = new Mock<IMongoCollection<Dispositivo>>();
        collection.Setup(c => c.UpdateOneAsync(
                It.IsAny<FilterDefinition<Dispositivo>>(),
                It.IsAny<UpdateDefinition<Dispositivo>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(result.Object);
        var context = new Mock<MongoDbContext>();
        context.SetupGet(c => c.Dispositivos).Returns(collection.Object);
        var service = new DispositivoService(context.Object);

        var updated = await service.ActualizarAliasAsync("65f1a2b3c4d5e6f7a8b9c0d1", " Alias actualizado ");

        Assert.Equal(expected, updated);
        collection.Verify(c => c.UpdateOneAsync(
            It.IsAny<FilterDefinition<Dispositivo>>(),
            It.IsAny<UpdateDefinition<Dispositivo>>(),
            It.IsAny<UpdateOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("invalido")]
    [InlineData("")]
    public async Task EliminarDispositivoAsync_IdInvalido_RechazaAntesDeEliminar(string id)
    {
        var context = new Mock<MongoDbContext>();
        var service = new DispositivoService(context.Object);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.EliminarDispositivoAsync(id));

        Assert.Equal("El identificador del dispositivo no tiene un formato válido.", exception.Message);
        context.VerifyGet(c => c.Dispositivos, Times.Never);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(0, false)]
    public async Task EliminarDispositivoAsync_ResultadoMongo_ReflejaDocumentosEliminados(long deletedCount, bool expected)
    {
        var collection = new Mock<IMongoCollection<Dispositivo>>();
        collection.Setup(c => c.DeleteOneAsync(
                It.IsAny<FilterDefinition<Dispositivo>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteResult.Acknowledged(deletedCount));
        var context = new Mock<MongoDbContext>();
        context.SetupGet(c => c.Dispositivos).Returns(collection.Object);
        var service = new DispositivoService(context.Object);

        var deleted = await service.EliminarDispositivoAsync("65f1a2b3c4d5e6f7a8b9c0d1");

        Assert.Equal(expected, deleted);
        collection.Verify(c => c.DeleteOneAsync(
            It.IsAny<FilterDefinition<Dispositivo>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
