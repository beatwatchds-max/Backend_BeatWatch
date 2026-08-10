using System.ComponentModel.DataAnnotations;
using BeatWatch_BackEnd.Dtos;

namespace BeatWatch_BackEnd.Tests.Models;

public class DeviceAndHealthDtoValidationTests
{
    [Fact]
    public void EmparejarDispositivoDto_RequiereCamposObligatorios()
    {
        var errors = Validar(new EmparejarDispositivoDto());

        Assert.Equal(3, errors.Count);
    }

    [Fact]
    public void EmparejarDispositivoDto_AdmiteSolicitudValida()
    {
        var errors = Validar(new EmparejarDispositivoDto
        {
            IdSesion = "sesion-001",
            TokenEmparejamiento = "token-001",
            Alias = "Reloj",
            IdPaciente = "65f1a2b3c4d5e6f7a8b9c0d1"
        });

        Assert.Empty(errors);
    }

    [Fact]
    public void RegistrarArritmiaDto_RechazaCamposInvalidosYSubdocumentosAusentes()
    {
        var errors = Validar(new RegistrarArritmiaDto
        {
            Tipo = string.Empty,
            FrecuenciaCardiaca = 301,
            DuracionEpisodioSeconds = -1,
            IdPaciente = "invalido"
        });

        Assert.Equal(6, errors.Count);
    }

    [Fact]
    public void RegistrarArritmiaDto_AdmiteSolicitudValida()
    {
        var errors = Validar(new RegistrarArritmiaDto
        {
            Tipo = "Taquicardia",
            FrecuenciaCardiaca = 120,
            DuracionEpisodioSeconds = 30,
            IdPaciente = "65f1a2b3c4d5e6f7a8b9c0d1",
            Sintomas = new SintomasDto(),
            FactoresRiesgo = new FactoresRiesgoDto()
        });

        Assert.Empty(errors);
    }

    private static List<ValidationResult> Validar(object request)
    {
        var errors = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), errors, validateAllProperties: true);
        return errors;
    }
}
