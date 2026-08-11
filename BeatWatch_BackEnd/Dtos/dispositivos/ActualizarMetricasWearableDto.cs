using System.ComponentModel.DataAnnotations;

namespace BeatWatch_BackEnd.Dtos.dispositivos
{
    public class ActualizarMetricasWearableDto
    {
        [Range(30, 240, ErrorMessage = "Frecuencia cardíaca fuera de rango válido.")]
        public int FrecuenciaCardiacaBpm { get; set; }

        [Range(70, 100, ErrorMessage = "Saturación de oxígeno fuera de rango.")]
        public int SaturacionOxigenoSpO2 { get; set; }

        [Range(0, 200000, ErrorMessage = "Conteo de pasos no válido.")]
        public int Pasos { get; set; }
    }
}