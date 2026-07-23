using System.ComponentModel.DataAnnotations;
using BeatWatch_BackEnd.Models;
using BeatWatch_BackEnd.Dtos;

namespace BeatWatch_BackEnd.Tests.Models;

public class AuthRequestValidationTests
{
    [Fact]
    public void RegistroRequest_RechazaTelefonoYContrasenaInvalidos()
    {
        var request = new RegistroRequest
        {
            Nombre = "User",
            Correo = "user@beatwatch.test",
            Telefono = "not-a-phone",
            Contrasena = "short"
        };

        var errors = Validate(request);

        Assert.Equal(2, errors.Count);
    }

    [Fact]
    public void SolicitarRestablecimientoRequest_RechazaCorreoInvalido()
    {
        var errors = Validate(new SolicitarRestablecimientoRequest { Correo = "invalid" });

        Assert.Single(errors);
    }

    [Fact]
    public void RestablecerContrasenaRequest_RequiereTokenYContrasenaSegura()
    {
        var errors = Validate(new RestablecerContrasenaRequest { Token = "", Contrasena = "short" });

        Assert.Equal(2, errors.Count);
    }

    [Fact]
    public void RestablecerContrasenaRequest_AdmiteSolicitudValida()
    {
        var errors = Validate(new RestablecerContrasenaRequest { Token = "one-time-token", Contrasena = "Password123!" });

        Assert.Empty(errors);
    }

    [Fact]
    public void CrearPerfilPacienteDto_RechazaCurpYLicenciaInvalidas()
    {
        var request = new CrearPerfilPacienteDto
        {
            CURP = "invalid",
            Edad = 131,
            Sexo = "",
            Peso = 0,
            Estatura = 0,
            TipoSangre = "X",
            IdLicencia = "invalid"
        };

        var errors = Validate(request);

        Assert.True(errors.Count >= 6);
    }

    [Fact]
    public void CrearPerfilPacienteDto_AdmiteDatosValidos()
    {
        var request = new CrearPerfilPacienteDto
        {
            CURP = "ABCD010101HDFABC01",
            Edad = 25,
            Sexo = "Masculino",
            Peso = 72.5,
            Estatura = 175,
            TipoSangre = "O+",
            IdLicencia = "65f1a2b3c4d5e6f7a8b9c0d1"
        };

        Assert.Empty(Validate(request));
    }

    private static List<ValidationResult> Validate(object request)
    {
        var errors = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), errors, validateAllProperties: true);
        return errors;
    }
}
