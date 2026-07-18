using BeatWatch_BackEnd.Models;

namespace BeatWatch_BackEnd.infrescture;

public interface ITokenService
{
    LoginResponse CreateAccessToken(Usuario usuario);
}
