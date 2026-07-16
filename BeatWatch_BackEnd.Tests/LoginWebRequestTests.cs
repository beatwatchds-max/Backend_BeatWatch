using System.ComponentModel.DataAnnotations;
using BeatWatch_BackEnd.Models;

namespace BeatWatch_Back_End.Tests;

public class LoginWebRequestTests
{
    [Fact]
    public void Validate_AdmiteCredencialesValidas()
    {
        var request = new LoginWebRequest
        {
            Correo = "usuario@example.com",
            Contrasena = "Password123"
        };

        var errors = Validate(request);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_RechazaCamposObligatoriosAusentes()
    {
        var errors = Validate(new LoginWebRequest());

        Assert.Equal(2, errors.Count);
    }

    [Fact]
    public void Validate_RechazaCorreoConFormatoInvalido()
    {
        var request = new LoginWebRequest
        {
            Correo = "correo-invalido",
            Contrasena = "Password123"
        };

        var errors = Validate(request);

        Assert.Single(errors);
        Assert.Equal("El correo no tiene un formato valido.", errors[0].ErrorMessage);
    }

    private static List<ValidationResult> Validate(LoginWebRequest request)
    {
        var errors = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), errors, validateAllProperties: true);
        return errors;
    }
}
