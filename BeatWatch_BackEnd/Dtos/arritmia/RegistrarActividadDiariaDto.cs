using System.ComponentModel.DataAnnotations;

namespace BeatWatch_BackEnd.Dtos.arritmia;

public class RegistrarActividadDiariaDto
{
    [Required]
    [RegularExpression("^[a-fA-F0-9]{24}$", ErrorMessage = "IdPaciente debe ser un ObjectId válido.")]
    public string IdPaciente { get; set; } = null!;

    [Range(0, 200000, ErrorMessage = "El número de pasos debe estar en un rango válido.")]
    public int Pasos { get; set; }

    [Range(0, 20000, ErrorMessage = "Las calorías deben ser un valor positivo razonable.")]
    public double Calorias { get; set; }

    [Range(0, 500, ErrorMessage = "La distancia debe estar en un rango válido (en Km).")]
    public double DistanciaKm { get; set; }

    [Range(0, 24, ErrorMessage = "Las horas de sueño deben ser entre 0 y 24.")]
    public double HorasSueno { get; set; }

    /// <summary>
    /// Fecha a la que corresponden los datos. Formato esperado: YYYY-MM-DD
    /// </summary>
    [Required]
    [RegularExpression(@"^\d{4}-\d{2}-\d{2}$", ErrorMessage = "La fecha debe tener el formato YYYY-MM-DD.")]
    public string Fecha { get; set; } = null!;
}