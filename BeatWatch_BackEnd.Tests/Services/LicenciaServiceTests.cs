using BeatWatch_BackEnd.Data;
using BeatWatch_BackEnd.Services;
using MongoDB.Driver;
using Moq;

namespace BeatWatch_BackEnd.Tests.Services;

public class LicenciaServiceTests
{
    [Fact]
    public async Task ActivarLicenciaGratuitaAsync_IdUsuarioInvalido_RechazaAntesDeConsultar()
    {
        var context = new Mock<MongoDbContext>();
        var service = new LicenciaService(context.Object);

        await Assert.ThrowsAsync<ArgumentException>(() => service.ActivarLicenciaGratuitaAsync("invalido"));
        context.VerifyGet(c => c.Usuarios, Times.Never);
    }
}
