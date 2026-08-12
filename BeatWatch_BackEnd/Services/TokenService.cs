using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BeatWatch_BackEnd.Configuration;
using BeatWatch_BackEnd.infrescture;
using BeatWatch_BackEnd.Models;
using BeatWatch_BackEnd.Models.LoginR;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BeatWatch_BackEnd.Services;

public sealed class TokenService : ITokenService
{
    private readonly JwtSettings _settings;

    public TokenService(IOptions<JwtSettings> settings)
    {
        _settings = settings.Value;
    }

    public LoginResponse CreateAccessToken(Usuario usuario)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_settings.ExpirationMinutes);

        // 🟢 Agregamos los claims necesarios incluyendo el idLicencia y el Rol
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuario.Id!),
            new Claim(JwtRegisteredClaimNames.Email, usuario.Correo),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, usuario.Rol ?? string.Empty),
            new Claim("idLicencia", usuario.IdLicencia ?? string.Empty) // 👈 ESTE ES EL CLAVE PARA EL REGISTRO
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SigningKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            _settings.Issuer,
            _settings.Audience,
            claims,
            expires: expiresAt,
            signingCredentials: credentials
        );

        return new LoginResponse
        {
            AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresIn = (int)TimeSpan.FromMinutes(_settings.ExpirationMinutes).TotalSeconds
        };
    }
}
