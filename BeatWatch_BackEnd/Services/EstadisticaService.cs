using BeatWatch_BackEnd.Data;
using BeatWatch_BackEnd.Dtos.graficas;
using BeatWatch_BackEnd.Dtos.pacientesDtos;
using BeatWatch_BackEnd.infrescture;
using BeatWatch_BackEnd.Models;
using MongoDB.Driver;

namespace BeatWatch_BackEnd.Services
{
    public class EstadisticaService : IEstadisticaService
    {
        private readonly MongoDbContext _context;

        public EstadisticaService(MongoDbContext context)
        {
            _context = context;
        }

        #region Métodos de consulta paciente y estadísticas

        public async Task<List<PacienteEstadisticaResumenDto>> ObtenerPacientesUnicosConUltimoRegistroAsync(string idLicencia)
        {
            var pacientesLicenciaIds = await _context.Pacientes
                .Find(p => p.IdLicencia == idLicencia)
                .Project(p => p.Id)
                .ToListAsync();

            if (!pacientesLicenciaIds.Any())
            {
                return new List<PacienteEstadisticaResumenDto>();
            }

            var pipeline = _context.EstadisticasDiarias.Aggregate()
      .Match(e => pacientesLicenciaIds.Contains(e.IdPaciente))
      .SortByDescending(e => e.Fecha)
      .Group(e => e.IdPaciente, g => new
      {
          IdPaciente = g.Key,
          UltimoRegistroStr = g.First().Fecha // Obtenemos el string primero
      });

            var resultadosDb = await pipeline.ToListAsync();

            // Lo convertimos al DTO mapeando el string a DateTime en memoria
            return resultadosDb.Select(g => new PacienteEstadisticaResumenDto
            {
                IdPaciente = g.IdPaciente,
                UltimoRegistro = DateTime.Parse(g.UltimoRegistroStr) // Transformación manual
            }).ToList();
        }

        public async Task<List<EstadisticasDiarias>> ObtenerEstadisticasPorPacienteAsync(string idPaciente, DateTime? fechaInicio = null, DateTime? fechaFin = null)
        {
            if (string.IsNullOrWhiteSpace(idPaciente))
            {
                throw new ArgumentException("El ID del paciente es obligatorio.");
            }

            var builder = Builders<EstadisticasDiarias>.Filter;
            var filtro = builder.Eq(e => e.IdPaciente, idPaciente);

            // 🟢 Filtramos comparando strings en formato "yyyy-MM-dd"
            if (fechaInicio.HasValue)
            {
                filtro &= builder.Gte(e => e.Fecha, fechaInicio.Value.ToString("yyyy-MM-dd"));
            }

            if (fechaFin.HasValue)
            {
                filtro &= builder.Lte(e => e.Fecha, fechaFin.Value.ToString("yyyy-MM-dd"));
            }

            if (fechaInicio.HasValue || fechaFin.HasValue)
            {
                return await _context.EstadisticasDiarias
                    .Find(filtro)
                    .SortBy(e => e.Fecha)
                    .ToListAsync();
            }

            var ultimaEstadistica = await _context.EstadisticasDiarias
                .Find(filtro)
                .SortByDescending(e => e.Fecha)
                .FirstOrDefaultAsync();

            return ultimaEstadistica != null ? new List<EstadisticasDiarias> { ultimaEstadistica } : new List<EstadisticasDiarias>();
        }
        #endregion

        #region Métodos de consulta para gráficas y series de datos

        public async Task<GraficaBpmResponseDto> ObtenerGraficaBpmAsync(string idPaciente, DateTime? fechaInicio = null, DateTime? fechaFin = null, int dias = 7)
        {
            if (string.IsNullOrWhiteSpace(idPaciente))
            {
                throw new ArgumentException("El ID del paciente es obligatorio.");
            }

            var builder = Builders<EstadisticasDiarias>.Filter;
            var filtro = builder.Eq(e => e.IdPaciente, idPaciente);

            if (fechaInicio.HasValue && fechaFin.HasValue)
            {
                var inicioStr = fechaInicio.Value.ToString("yyyy-MM-dd");
                var finStr = fechaFin.Value.ToString("yyyy-MM-dd");
                filtro &= builder.Gte(e => e.Fecha, inicioStr) & builder.Lte(e => e.Fecha, finStr);
            }
            else
            {
                var fechaLimiteStr = DateTime.UtcNow.AddDays(-dias).ToString("yyyy-MM-dd");
                filtro &= builder.Gte(e => e.Fecha, fechaLimiteStr);
            }

            var registros = await _context.EstadisticasDiarias
                .Find(filtro)
                .SortBy(e => e.Fecha)
                .ToListAsync();

            var puntos = registros.Select(e => new PuntoBpmDto
            {
                Fecha = e.Fecha,
                Promedio = e.FrecuenciaPromedio ?? 0,
                Minimo = (int)(e.FrecuenciaMinima ?? 0), // 🟢 Cast a int
                Maximo = (int)(e.FrecuenciaMaxima ?? 0)  // 🟢 Cast a int
            }).ToList();

            return new GraficaBpmResponseDto
            {
                IdPaciente = idPaciente,
                Puntos = puntos
            };
        }

        public async Task<GraficaEpisodiosResponseDto> ObtenerGraficaEpisodiosAsync(string idPaciente, DateTime? fechaInicio = null, DateTime? fechaFin = null, int dias = 7)
        {
            if (string.IsNullOrWhiteSpace(idPaciente))
            {
                throw new ArgumentException("El ID del paciente es obligatorio.");
            }

            var builder = Builders<EstadisticasDiarias>.Filter;
            var filtro = builder.Eq(e => e.IdPaciente, idPaciente);

            if (fechaInicio.HasValue && fechaFin.HasValue)
            {
                var inicioStr = fechaInicio.Value.ToString("yyyy-MM-dd");
                var finStr = fechaFin.Value.ToString("yyyy-MM-dd");
                filtro &= builder.Gte(e => e.Fecha, inicioStr) & builder.Lte(e => e.Fecha, finStr);
            }
            else
            {
                var fechaLimiteStr = DateTime.UtcNow.AddDays(-dias).ToString("yyyy-MM-dd");
                filtro &= builder.Gte(e => e.Fecha, fechaLimiteStr);
            }

            var registros = await _context.EstadisticasDiarias
                .Find(filtro)
                .SortBy(e => e.Fecha)
                .ToListAsync();

            var episodios = registros.Select(e => new EpisodioGraficaDto
            {
                Fecha = e.Fecha,
                TotalArritmias = e.TotalArritmias,
                Criticas = e.AlertasCriticas,
                DuracionTotalSegundos = e.DuracionTotalEpisodios
            }).ToList();

            return new GraficaEpisodiosResponseDto
            {
                IdPaciente = idPaciente,
                Episodios = episodios
            };
        }

        public async Task<GraficaSeriesResponseDto> ObtenerGraficaSeriesAsync(string idPaciente, DateTime? fechaInicio = null, DateTime? fechaFin = null, string? metricas = null)
        {
            if (string.IsNullOrWhiteSpace(idPaciente))
            {
                throw new ArgumentException("El ID del paciente es obligatorio.");
            }

            var builder = Builders<EstadisticasDiarias>.Filter;
            var filtro = builder.Eq(e => e.IdPaciente, idPaciente);

            if (fechaInicio.HasValue && fechaFin.HasValue)
            {
                var inicioStr = fechaInicio.Value.ToString("yyyy-MM-dd");
                var finStr = fechaFin.Value.ToString("yyyy-MM-dd");
                filtro &= builder.Gte(e => e.Fecha, inicioStr) & builder.Lte(e => e.Fecha, finStr);
            }
            else
            {
                var fechaLimiteStr = DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd");
                filtro &= builder.Gte(e => e.Fecha, fechaLimiteStr);
            }

            var registros = await _context.EstadisticasDiarias
                .Find(filtro)
                .SortBy(e => e.Fecha)
                .ToListAsync();

            var listaMetricas = string.IsNullOrWhiteSpace(metricas)
                ? new List<string> { "BPMPromedio", "Pasos" }
                : metricas.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

            var listaMetricasUpper = listaMetricas.Select(m => m.ToUpperInvariant()).ToList();

            bool incluirBpmPromedio = listaMetricasUpper.Contains("BPMPROMEDIO") || listaMetricasUpper.Contains("PROMEDIO");
            bool incluirBpmMinimo = listaMetricasUpper.Contains("BPMMINIMO") || listaMetricasUpper.Contains("MINIMO");
            bool incluirBpmMaximo = listaMetricasUpper.Contains("BPMMAXIMO") || listaMetricasUpper.Contains("MAXIMO");
            bool incluirPasos = listaMetricasUpper.Contains("PASOS");
            bool incluirCalorias = listaMetricasUpper.Contains("CALORIAS");
            bool incluirDistancia = listaMetricasUpper.Contains("DISTANCIAKM") || listaMetricasUpper.Contains("DISTANCIA");
            bool incluirSueno = listaMetricasUpper.Contains("HORASSUENO") || listaMetricasUpper.Contains("SUENO");

            var series = registros.Select(e => new PuntoSerieDto
            {
                Fecha = e.Fecha,
                BpmPromedio = incluirBpmPromedio ? e.FrecuenciaPromedio : null,
                BpmMinimo = incluirBpmMinimo ? (int?)e.FrecuenciaMinima : null, // 🟢 Cast a int?
                BpmMaximo = incluirBpmMaximo ? (int?)e.FrecuenciaMaxima : null, // 🟢 Cast a int?
                Pasos = incluirPasos ? e.TotalPasos : null,
                Calorias = incluirCalorias ? e.TotalCalorias : null,
                DistanciaKm = incluirDistancia ? e.DistanciaTotalKm : null,
                HorasSueno = incluirSueno ? e.HorasSueno : null
            }).ToList();

            return new GraficaSeriesResponseDto
            {
                IdPaciente = idPaciente,
                MetricasSolicitadas = listaMetricas,
                Series = series
            };
        }

        public async Task<GraficaSeriesColumnarResponseDto> ObtenerGraficaSeriesColumnarAsync(string idPaciente, DateTime? fechaInicio = null, DateTime? fechaFin = null, string? metricas = null)
        {
            if (string.IsNullOrWhiteSpace(idPaciente))
            {
                throw new ArgumentException("El ID del paciente es obligatorio.");
            }

            var builder = Builders<EstadisticasDiarias>.Filter;
            var filtro = builder.Eq(e => e.IdPaciente, idPaciente);

            if (fechaInicio.HasValue && fechaFin.HasValue)
            {
                var inicioStr = fechaInicio.Value.ToString("yyyy-MM-dd");
                var finStr = fechaFin.Value.ToString("yyyy-MM-dd");
                filtro &= builder.Gte(e => e.Fecha, inicioStr) & builder.Lte(e => e.Fecha, finStr);
            }
            else
            {
                var fechaLimiteStr = DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd");
                filtro &= builder.Gte(e => e.Fecha, fechaLimiteStr);
            }

            var registros = await _context.EstadisticasDiarias
                .Find(filtro)
                .SortBy(e => e.Fecha)
                .ToListAsync();

            var listaMetricas = string.IsNullOrWhiteSpace(metricas)
                ? new List<string> { "BPMPromedio", "BPMMinimo", "BPMMaximo" }
                : metricas.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

            var dictSeries = new Dictionary<string, object>();
            dictSeries["fechas"] = registros.Select(e => e.Fecha).ToList();

            foreach (var metrica in listaMetricas)
            {
                var keyUpper = metrica.ToUpperInvariant();

                switch (keyUpper)
                {
                    case "BPMPROMEDIO":
                    case "PROMEDIO":
                        dictSeries["BPMPromedio"] = registros.Select(e => e.FrecuenciaPromedio).ToList();
                        break;
                    case "BPMMINIMO":
                    case "MINIMO":
                        dictSeries["BPMMinimo"] = registros.Select(e => e.FrecuenciaMinima).ToList();
                        break;
                    case "BPMMAXIMO":
                    case "MAXIMO":
                        dictSeries["BPMMAXIMO"] = registros.Select(e => e.FrecuenciaMaxima).ToList();
                        break;
                    case "PASOS":
                        dictSeries["Pasos"] = registros.Select(e => e.TotalPasos).ToList();
                        break;
                    case "CALORIAS":
                        dictSeries["Calorias"] = registros.Select(e => e.TotalCalorias).ToList();
                        break;
                    case "DISTANCIAKM":
                    case "DISTANCIA":
                        dictSeries["DistanciaKm"] = registros.Select(e => e.DistanciaTotalKm).ToList();
                        break;
                    case "HORASSUENO":
                    case "SUENO":
                        dictSeries["HorasSueno"] = registros.Select(e => e.HorasSueno).ToList();
                        break;
                    case "TOTALARRITMIAS":
                    case "ARRITMIAS":
                        dictSeries["TotalArritmias"] = registros.Select(e => e.TotalArritmias).ToList();
                        break;
                }
            }

            return new GraficaSeriesColumnarResponseDto
            {
                IdPaciente = idPaciente,
                Series = dictSeries
            };
        }

        public async Task<GraficaResumenResponseDto> ObtenerResumenKpiAsync(string idPaciente, DateTime? fechaInicio = null, DateTime? fechaFin = null, int dias = 30)
        {
            if (string.IsNullOrWhiteSpace(idPaciente))
            {
                throw new ArgumentException("El ID del paciente es obligatorio.");
            }

            var builder = Builders<EstadisticasDiarias>.Filter;
            var filtro = builder.Eq(e => e.IdPaciente, idPaciente);

            string etiquetaPeriodo;

            if (fechaInicio.HasValue && fechaFin.HasValue)
            {
                var inicioStr = fechaInicio.Value.ToString("yyyy-MM-dd");
                var finStr = fechaFin.Value.ToString("yyyy-MM-dd");
                filtro &= builder.Gte(e => e.Fecha, inicioStr) & builder.Lte(e => e.Fecha, finStr);
                etiquetaPeriodo = $"{fechaInicio.Value:yyyy-MM-dd} a {fechaFin.Value:yyyy-MM-dd}";
            }
            else
            {
                var fechaLimiteStr = DateTime.UtcNow.AddDays(-dias).ToString("yyyy-MM-dd");
                filtro &= builder.Gte(e => e.Fecha, fechaLimiteStr);
                etiquetaPeriodo = $"Ultimos {dias} dias";
            }

            var registros = await _context.EstadisticasDiarias
                .Find(filtro)
                .ToListAsync();

            if (!registros.Any())
            {
                return new GraficaResumenResponseDto
                {
                    IdPaciente = idPaciente,
                    Periodo = etiquetaPeriodo,
                    PromedioBPM = 0,
                    TotalPasos = 0,
                    TotalArritmias = 0,
                    PromedioHorasSueno = 0
                };
            }

            // Validamos HasValue para no romper el Average() si algún día llega nulo por el ETL
            double promedioBpm = registros.Any(e => e.FrecuenciaPromedio.HasValue)
                ? Math.Round(registros.Where(e => e.FrecuenciaPromedio.HasValue).Average(e => e.FrecuenciaPromedio!.Value), 1)
                : 0;

            int totalPasos = registros.Sum(e => e.TotalPasos);
            int totalArritmias = registros.Sum(e => e.TotalArritmias);

            double promedioSueno = registros.Any(e => e.HorasSueno.HasValue)
                ? Math.Round(registros.Where(e => e.HorasSueno.HasValue).Average(e => e.HorasSueno!.Value), 1)
                : 0;

            return new GraficaResumenResponseDto
            {
                IdPaciente = idPaciente,
                Periodo = etiquetaPeriodo,
                PromedioBPM = promedioBpm,
                TotalPasos = totalPasos,
                TotalArritmias = totalArritmias,
                PromedioHorasSueno = promedioSueno
            };
        }
        #endregion
    }
}