namespace BeatWatch_BackEnd.Dtos.historial;

public class ResumenTableroDto
{
    public int TotalEpisodiosPeriodo { get; set; }
    public double BpmPromedio { get; set; }
    public int BpmMaximo { get; set; }
    public double PorcentajeDiasEstables { get; set; }

    // Conteo consolidado de síntomas en el periodo seleccionado (ej: Palpitaciones: 4, Mareo: 3)
    public Dictionary<string, int> ConteoSintomas { get; set; } = new();

    // Datos resumidos de actividad diaria (sumas/promedios)
    public int TotalPasos { get; set; }
    public double TotalCalorias { get; set; }
    public double TotalDistanciaKm { get; set; }

    // Puntos para la gráfica de picos (Fecha vs BPM Max)
    public List<PuntoGraficaDto> GraficaPicos { get; set; } = new();
}

public class PuntoGraficaDto
{
    public string Fecha { get; set; } = null!; // Formato YYYY-MM-DD
    public int BpmMaximo { get; set; }
    public int BpmPromedio { get; set; }
}