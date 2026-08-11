using BeatWatch_BackEnd.Models;
using BeatWatch_BackEnd.Models.LoginR;

namespace BeatWatch_BackEnd.infrescture;

public interface ITokenService
{
    LoginResponse CreateAccessToken(Usuario usuario);
}
