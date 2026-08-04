using BeatWatch_BackEnd.Data;
using BeatWatch_BackEnd.Dtos;
using BeatWatch_BackEnd.Models;
using BeatWatch_BackEnd.Services;
using MongoDB.Driver;
using Moq;

namespace BeatWatch_BackEnd.Tests.Services;

public class SaludServiceTests
{
    [Fact]
    public async Task ObtenerHistorialArritmiasAsync_FiltraPorPacienteYOrdenaPorFechaDescendente()
    {
        const string idPaciente = "65f1a2b3c4d5e6f7a8b9c0d1";
        var arritmias = new List<EpisodioArritmia>
        {
            new() { IdPaciente = idPaciente, Fecha = DateTime.UtcNow },
            new() { IdPaciente = idPaciente, Fecha = DateTime.UtcNow.AddMinutes(-1) }
        };
        var cursor = new Mock<IAsyncCursor<EpisodioArritmia>>();
        cursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);
        cursor.Setup(c => c.Current).Returns(arritmias);
        var coleccion = new Mock<IMongoCollection<EpisodioArritmia>>();
        coleccion.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<EpisodioArritmia>>(),
                It.IsAny<FindOptions<EpisodioArritmia, EpisodioArritmia>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cursor.Object);
        var contexto = new Mock<MongoDbContext>();
        contexto.SetupGet(c => c.EpisodiosArritmia).Returns(coleccion.Object);
        var servicio = new SaludService(contexto.Object);

        var resultado = await servicio.ObtenerHistorialArritmiasAsync(idPaciente, CancellationToken.None);

        Assert.Equal(arritmias, resultado);
        coleccion.Verify(c => c.FindAsync(
            It.IsAny<FilterDefinition<EpisodioArritmia>>(),
            It.Is<FindOptions<EpisodioArritmia, EpisodioArritmia>>(options => options.Sort != null),
            CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task RegistrarArritmiaAsync_MapeaLecturaYSintomasEnUnaInsercionUtc()
    {
        Arritmia? insertada = null;
        var coleccion = new Mock<IMongoCollection<Arritmia>>();
        coleccion.Setup(c => c.InsertOneAsync(
                It.IsAny<Arritmia>(),
                It.IsAny<InsertOneOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<Arritmia, InsertOneOptions, CancellationToken>((arritmia, _, _) => insertada = arritmia)
            .Returns(Task.CompletedTask);

        var contexto = new Mock<MongoDbContext>();
        contexto.SetupGet(c => c.Arritmias).Returns(coleccion.Object);
        var servicio = new SaludService(contexto.Object);
        var antes = DateTime.UtcNow;

        await servicio.RegistrarArritmiaAsync(new RegistrarArritmiaDto
        {
            Tipo = "Fibrilacion auricular",
            FrecuenciaCardiaca = 155,
            DuracionEpisodioSeconds = 42,
            IdPaciente = "65f1a2b3c4d5e6f7a8b9c0d1",
            Sintomas = new SintomasDto
            {
                Mareo = true,
                Palpitaciones = true,
                DolorPecho = false,
                Desmayo = false,
                FaltaAire = true,
                Fatiga = true
            },
            FactoresRiesgo = new FactoresRiesgoDto
            {
                HipertensionArterial = true,
                ObesidadImcElevado = false,
                ApneaSueno = true,
                Tabaquismo = false,
                Alcoholismo = false,
                EstresCronico = true
            }
        }, CancellationToken.None);
        var despues = DateTime.UtcNow;

        Assert.NotNull(insertada);
        Assert.Equal("Fibrilacion auricular", insertada.Tipo);
        Assert.Equal(155, insertada.FrecuenciaCardiaca);
        Assert.Equal(42, insertada.DuracionEpisodioSeconds);
        Assert.Equal("65f1a2b3c4d5e6f7a8b9c0d1", insertada.IdPaciente);
        Assert.True(insertada.Sintomas.Mareo);
        Assert.True(insertada.Sintomas.Palpitaciones);
        Assert.False(insertada.Sintomas.DolorPecho);
        Assert.False(insertada.Sintomas.Desmayo);
        Assert.True(insertada.Sintomas.FaltaAire);
        Assert.True(insertada.Sintomas.Fatiga);
        Assert.True(insertada.FactoresRiesgo.HipertensionArterial);
        Assert.False(insertada.FactoresRiesgo.ObesidadImcElevado);
        Assert.True(insertada.FactoresRiesgo.ApneaSueno);
        Assert.False(insertada.FactoresRiesgo.Tabaquismo);
        Assert.False(insertada.FactoresRiesgo.Alcoholismo);
        Assert.True(insertada.FactoresRiesgo.EstresCronico);
        Assert.Equal(DateTimeKind.Utc, insertada.Fecha.Kind);
        Assert.InRange(insertada.Fecha, antes, despues);
        coleccion.Verify(c => c.InsertOneAsync(insertada, It.IsAny<InsertOneOptions>(), CancellationToken.None), Times.Once);
    }
}
