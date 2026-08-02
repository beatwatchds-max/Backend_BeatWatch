using System.ComponentModel.DataAnnotations;

namespace BeatWatch_BackEnd.Dtos;

public class RegistrarAlertaFrecuenciaDto
{
    [Required]
    [RegularExpression("^[a-fA-F0-9]{24}$", ErrorMessage = "IdPaciente debe ser un ObjectId válido.")]
    public string IdPaciente { get; set; } = null!;

    [Required]
    [StringLength(100)]
    public string TipoAnomalia { get; set; } = null!;

    [Range(30, 300)]
    public int FrecuenciaCardiaca { get; set; }

    [Range(0, int.MaxValue)]
    public int DuracionEpisodioSeconds { get; set; }
}