using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using BeatWatch_BackEnd.Configuration;
using BeatWatch_BackEnd.Models;
using BeatWatch_BackEnd.Services;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BeatWatch_BackEnd.Tests.Services;

public class TokenServiceTests
{
    [Fact]
    public void CreateAccessToken_UsuarioValido_EmiteJwtConIdentidadYExpiracion()
    {
        var settings = new JwtSettings
        {
            Issuer = "https://api.beatwatch.test",
            Audience = "beatwatch-tests",
            SigningKey = "unit-test-signing-key-must-be-at-least-32-bytes",
            ExpirationMinutes = 15
        };
        var usuario = new Usuario
        {
            Id = "65f1a2b3c4d5e6f7a8b9c0d1",
            Correo = "user@beatwatch.test",
            Nombre = "User"
        };
        var service = new TokenService(Options.Create(settings));

        var response = service.CreateAccessToken(usuario);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(response.AccessToken);

        Assert.Equal("Bearer", response.TokenType);
        Assert.Equal(900, response.ExpiresIn);
        Assert.Equal(settings.Issuer, token.Issuer);
        Assert.Equal(settings.Audience, token.Audiences.Single());
        Assert.Equal(usuario.Id, token.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal(usuario.Correo, token.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.True(token.ValidTo > DateTime.UtcNow.AddMinutes(14));
    }

    [Fact]
    public void CreateAccessToken_FirmaYEmisorValidos_ValidaCorrectamente()
    {
        var settings = new JwtSettings
        {
            Issuer = "https://api.beatwatch.test",
            Audience = "beatwatch-tests",
            SigningKey = "unit-test-signing-key-must-be-at-least-32-bytes",
            ExpirationMinutes = 15
        };
        var service = new TokenService(Options.Create(settings));
        var response = service.CreateAccessToken(new Usuario
        {
            Id = "65f1a2b3c4d5e6f7a8b9c0d1",
            Correo = "user@beatwatch.test",
            Nombre = "User"
        });

        var principal = new JwtSecurityTokenHandler().ValidateToken(response.AccessToken, new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = settings.Issuer,
            ValidateAudience = true,
            ValidAudience = settings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(settings.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        }, out _);

        Assert.Equal("65f1a2b3c4d5e6f7a8b9c0d1", principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
    }
}
