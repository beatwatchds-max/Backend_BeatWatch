namespace BeatWatch_BackEnd.Models
{
    public class ResultadoPaginado<T>
    {
        public long TotalRegistros { get; set; }
        public int PaginaActual { get; set; }
        public int TotalPaginas { get; set; }
        public List<T> Datos { get; set; } = new List<T>();
    }
}