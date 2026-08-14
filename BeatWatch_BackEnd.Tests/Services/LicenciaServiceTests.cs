using BeatWatch_BackEnd.Data;
using BeatWatch_BackEnd.Models;
using BeatWatch_BackEnd.Services;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace BeatWatch_BackEnd.Tests.Services;

public class LicenciaServiceTests
{
    [Fact]
    public async Task ActivarLicenciaGratuitaAsync_DtoVacio_RechazaAntesDeConsultar()
    {
        // Arrange
        var context = new Mock<MongoDbContext>();
        var service = new LicenciaService(context.Object);
        var dtoVacio = new ActivarLicenciaGratuitaDto(); // Sin Correo ni UsuarioId

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.ActivarLicenciaGratuitaAsync(dtoVacio));
        context.VerifyGet(c => c.Usuarios, Times.Never);
    }
}