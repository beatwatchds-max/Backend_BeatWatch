namespace BeatWatch_BackEnd.Dtos
{
    public class SesionEmparejamientoResponseDto
    {
        public string IdSesion { get; set; } = string.Empty;
        public string TokenEmparejamiento { get; set; } = string.Empty;
        public string WatchSecret { get; set; } = string.Empty;
        public DateTime ExpiraEn { get; set; }
    }
}