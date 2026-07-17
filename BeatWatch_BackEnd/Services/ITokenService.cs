using BeatWatch_BackEnd.Models;

namespace BeatWatch_BackEnd.Services;

public interface ITokenService
{
    LoginResponse CreateAccessToken(Usuario usuario);
}
