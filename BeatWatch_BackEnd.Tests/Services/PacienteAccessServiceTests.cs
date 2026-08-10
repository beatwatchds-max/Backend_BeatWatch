using System.Security.Claims;
using BeatWatch_BackEnd.Data;
using BeatWatch_BackEnd.Models;
using BeatWatch_BackEnd.Services;
using MongoDB.Driver;
using Moq;

namespace BeatWatch_BackEnd.Tests.Services;

public class PacienteAccessServiceTests
{
    [Theory]
    [InlineData("Administrador")]
    [InlineData("Cuidador")]
    public async Task PuedeAccederAsync_PersonalDeLaMismaLicencia_PermiteAccesoAlPaciente(string rol)
    {
        const string idPaciente = "65f1a2b3c4d5e6f7a8b9c0d1";
        const string idLicencia = "65f1a2b3c4d5e6f7a8b9c0d2";
        var pacientes = new Mock<IMongoCollection<Paciente>>();
        var cursor = new Mock<IAsyncCursor<Paciente>>();
        cursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);
        cursor.Setup(c => c.Current).Returns(new List<Paciente>
        {
            new() { Id = idPaciente, UsuarioId = "65f1a2b3c4d5e6f7a8b9c0d3", IdLicencia = idLicencia }
        });
        pacientes.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<Paciente>>(),
                It.IsAny<FindOptions<Paciente, Paciente>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cursor.Object);
        var contexto = new Mock<MongoDbContext>();
        contexto.SetupGet(c => c.Pacientes).Returns(pacientes.Object);
        var usuario = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "65f1a2b3c4d5e6f7a8b9c0d4"),
            new Claim(ClaimTypes.Role, rol),
            new Claim("idLicencia", idLicencia)
        ], "test"));

        var puedeAcceder = await new PacienteAccessService(contexto.Object).PuedeAccederAsync(usuario, idPaciente);

        Assert.True(puedeAcceder);
        contexto.VerifyGet(c => c.Usuarios, Times.Never);
    }
}
