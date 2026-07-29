using System.ComponentModel.DataAnnotations;

namespace BeatWatch_BackEnd.Dtos;

public class RegistrarArritmiaDto
{
    [Required]
    [StringLength(100)]
    public string Tipo { get; set; } = null!;

    [Range(1, 300)]
    public int FrecuenciaCardiaca { get; set; }

    [Range(0, int.MaxValue)]
    public int DuracionEpisodioSeconds { get; set; }

    [Required]
    [RegularExpression("^[a-fA-F0-9]{24}$", ErrorMessage = "IdPaciente debe ser un ObjectId de MongoDB valido.")]
    public string IdPaciente { get; set; } = null!;

    [Required]
    public SintomasDto Sintomas { get; set; } = null!;
    [Required]
    public FactoresRiesgoDto FactoresRiesgo { get; set; } = null!;
}

public class SintomasDto
{
    public bool Mareo { get; set; }
    public bool Palpitaciones { get; set; }
    public bool DolorPecho { get; set; }
    public bool Desmayo { get; set; }
    public bool FaltaAire { get; set; }
    public bool Fatiga { get; set; }
}
public class FactoresRiesgoDto
{
    public bool HipertensionArterial { get; set; }
    public bool ObesidadImcElevado { get; set; }
    public bool ApneaSueno { get; set; }
    public bool Tabaquismo { get; set; }
    public bool Alcoholismo { get; set; }
    public bool EstresCronico { get; set; }
}