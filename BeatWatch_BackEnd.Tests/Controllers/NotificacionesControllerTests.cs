using System.Security.Claims;
using BeatWatch_BackEnd.Controllers;
using BeatWatch_BackEnd.Data;
using BeatWatch_BackEnd.Dtos.notificaciones;
using BeatWatch_BackEnd.infrescture;
using BeatWatch_BackEnd.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Moq;

namespace BeatWatch_BackEnd.Tests.Controllers;

public class NotificacionesControllerTests
{
    [Fact]
    public async Task RegistrarToken_CamposEnBlanco_Retorna400SinModificarMongo()
    {
        var usuarios = new Mock<IMongoCollection<Usuario>>();
        var context = new Mock<MongoDbContext>();
        context.SetupGet(c => c.Usuarios).Returns(usuarios.Object);
        var fcm = new Mock<IFcmNotificationService>();
        var controller = CrearController(context.Object, fcm.Object);

        var resultado = await controller.RegistrarToken(new TokenFcmDto
        {
            Token = " ",
            DeviceId = "android-id",
            DeviceType = "android"
        }, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(resultado);
        usuarios.Verify(c => c.UpdateManyAsync(It.IsAny<FilterDefinition<Usuario>>(), It.IsAny<UpdateDefinition<Usuario>>(), It.IsAny<UpdateOptions>(), It.IsAny<CancellationToken>()), Times.Never);
        usuarios.Verify(c => c.UpdateOneAsync(It.IsAny<FilterDefinition<Usuario>>(), It.IsAny<UpdateDefinition<Usuario>>(), It.IsAny<UpdateOptions>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static NotificacionesController CrearController(MongoDbContext context, IFcmNotificationService fcm)
    {
        var controller = new NotificacionesController(context, fcm);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "65f1a2b3c4d5e6f7a8b9c0d1")], "test"))
            }
        };
        return controller;
    }
}
