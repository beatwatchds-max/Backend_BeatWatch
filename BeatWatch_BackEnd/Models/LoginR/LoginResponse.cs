namespace BeatWatch_BackEnd.Models.LoginR;

public sealed class LoginResponse
{
    public string AccessToken { get; init; } = null!;
    public string TokenType { get; init; } = "Bearer";
    public int ExpiresIn { get; init; }
}
