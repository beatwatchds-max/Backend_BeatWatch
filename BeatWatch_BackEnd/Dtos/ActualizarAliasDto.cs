using System.ComponentModel.DataAnnotations;

namespace BeatWatch_BackEnd.Dtos
{
    public class ActualizarAliasDto
    {
        [Required(ErrorMessage = "El nuevo alias es obligatorio.")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "El alias debe tener entre 1 y 100 caracteres.")]
        public string Alias { get; set; } = string.Empty;
    }
}