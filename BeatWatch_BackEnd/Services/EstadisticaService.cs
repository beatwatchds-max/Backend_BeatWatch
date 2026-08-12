using BeatWatch_BackEnd.Data;
using BeatWatch_BackEnd.Dtos.graficas;
using BeatWatch_BackEnd.Dtos.pacientesDtos;
using BeatWatch_BackEnd.infrescture;
using BeatWatch_BackEnd.Models;
using MongoDB.Bson;
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
        #region Métodos de consulta pasiente y estadísticas


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
                    UltimoRegistro = g.First().Fecha
                })
                .Project(g => new PacienteEstadisticaResumenDto
                {
                    IdPaciente = g.IdPaciente,
                    UltimoRegistro = g.UltimoRegistro
                });

            return await pipeline.ToListAsync();
        }

        public async Task<List<EstadisticasDiarias>> ObtenerEstadisticasPorPacienteAsync(string idPaciente,DateTime? fechaInicio = null, DateTime? fechaFin = null)
        {
            if (string.IsNullOrWhiteSpace(idPaciente))
            {
                throw new ArgumentException("El ID del paciente es obligatorio.");
            }

            var builder = Builders<EstadisticasDiarias>.Filter;
            var filtro = builder.Eq(e => e.IdPaciente, idPaciente);

            // 🟢 Si se envían fechas, aplicamos el rango
            if (fechaInicio.HasValue)
            {
                filtro &= builder.Gte(e => e.Fecha, DateTime.SpecifyKind(fechaInicio.Value.Date, DateTimeKind.Utc));
            }

            if (fechaFin.HasValue)
            {
                // Se ajusta a las 23:59:59 para incluir todo el día final
                var fechaFinAjustada = fechaFin.Value.Date.AddDays(1).AddTicks(-1);
                filtro &= builder.Lte(e => e.Fecha, DateTime.SpecifyKind(fechaFinAjustada, DateTimeKind.Utc));
            }

            // Si se especificó rango de fechas, ordenamos de forma ascendente (cronológico)
            if (fechaInicio.HasValue || fechaFin.HasValue)
            {
                return await _context.EstadisticasDiarias
                    .Find(filtro)
                    .SortBy(e => e.Fecha)
                    .ToListAsync();
            }

            // Si NO se enviaron fechas, tomamos únicamente el registro más reciente
            var ultimaEstadistica = await _context.EstadisticasDiarias
                .Find(filtro)
                .SortByDescending(e => e.Fecha)
                .FirstOrDefaultAsync();

            return ultimaEstadistica != null ? new List<EstadisticasDiarias> { ultimaEstadistica } : new List<EstadisticasDiarias>();
        }
        #endregion

        #region Métodos de consulta para gráficas y series de datos
        public async Task<GraficaBpmResponseDto> ObtenerGraficaBpmAsync(string idPaciente,DateTime? fechaInicio = null, DateTime? fechaFin = null,int dias = 7)
        {
            if (string.IsNullOrWhiteSpace(idPaciente))
            {
                throw new ArgumentException("El ID del paciente es obligatorio.");
            }

            var builder = Builders<EstadisticasDiarias>.Filter;
            var filtro = builder.Eq(e => e.IdPaciente, idPaciente);

            // Definir el rango de fechas
            if (fechaInicio.HasValue && fechaFin.HasValue)
            {
                var inicioUtc = DateTime.SpecifyKind(fechaInicio.Value.Date, DateTimeKind.Utc);
                var finUtc = DateTime.SpecifyKind(fechaFin.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);

                filtro &= builder.Gte(e => e.Fecha, inicioUtc) & builder.Lte(e => e.Fecha, finUtc);
            }
            else
            {
                // Por defecto, consulta los últimos N días transcurridos
                var fechaLimite = DateTime.UtcNow.Date.AddDays(-dias);
                filtro &= builder.Gte(e => e.Fecha, fechaLimite);
            }

            // Consulta en MongoDB proyectando los puntos de BPM
            var registros = await _context.EstadisticasDiarias
                .Find(filtro)
                .SortBy(e => e.Fecha)
                .ToListAsync();

            var puntos = registros.Select(e => new PuntoBpmDto
            {
                Fecha = e.Fecha.ToString("yyyy-MM-dd"),
                Promedio = e.FrecuenciaCardiaca.Promedio,
                Minimo = e.FrecuenciaCardiaca.Minimo,
                Maximo = e.FrecuenciaCardiaca.Maximo
            }).ToList();

            return new GraficaBpmResponseDto
            {
                IdPaciente = idPaciente,
                Puntos = puntos
            };
        }
        ///
        public async Task<GraficaEpisodiosResponseDto> ObtenerGraficaEpisodiosAsync(string idPaciente,DateTime? fechaInicio = null,DateTime? fechaFin = null, int dias = 7)
        {
            if (string.IsNullOrWhiteSpace(idPaciente))
            {
                throw new ArgumentException("El ID del paciente es obligatorio.");
            }

            var builder = Builders<EstadisticasDiarias>.Filter;
            var filtro = builder.Eq(e => e.IdPaciente, idPaciente);

            // Definición de rango de fechas
            if (fechaInicio.HasValue && fechaFin.HasValue)
            {
                var inicioUtc = DateTime.SpecifyKind(fechaInicio.Value.Date, DateTimeKind.Utc);
                var finUtc = DateTime.SpecifyKind(fechaFin.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);

                filtro &= builder.Gte(e => e.Fecha, inicioUtc) & builder.Lte(e => e.Fecha, finUtc);
            }
            else
            {
                // Por defecto, consulta los últimos N días
                var fechaLimite = DateTime.UtcNow.Date.AddDays(-dias);
                filtro &= builder.Gte(e => e.Fecha, fechaLimite);
            }

            // Consulta en MongoDB proyectando los episodios diarios
            var registros = await _context.EstadisticasDiarias
                .Find(filtro)
                .SortBy(e => e.Fecha)
                .ToListAsync();

            var episodios = registros.Select(e => new EpisodioGraficaDto
            {
                Fecha = e.Fecha.ToString("yyyy-MM-dd"),
                TotalArritmias = e.Arritmias.Total,
                Criticas = e.Arritmias.Criticas,
                DuracionTotalSegundos = e.Arritmias.DuracionTotalSeconds
            }).ToList();

            return new GraficaEpisodiosResponseDto
            {
                IdPaciente = idPaciente,
                Episodios = episodios
            };
        }
        ///
        public async Task<GraficaSeriesResponseDto> ObtenerGraficaSeriesAsync(string idPaciente,DateTime? fechaInicio = null,DateTime? fechaFin = null, string? metricas = null)
        {
            if (string.IsNullOrWhiteSpace(idPaciente))
            {
                throw new ArgumentException("El ID del paciente es obligatorio.");
            }

            var builder = Builders<EstadisticasDiarias>.Filter;
            var filtro = builder.Eq(e => e.IdPaciente, idPaciente);

            // Si no se especifican fechas, toma por defecto el último mes (30 días)
            if (fechaInicio.HasValue && fechaFin.HasValue)
            {
                var inicioUtc = DateTime.SpecifyKind(fechaInicio.Value.Date, DateTimeKind.Utc);
                var finUtc = DateTime.SpecifyKind(fechaFin.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
                filtro &= builder.Gte(e => e.Fecha, inicioUtc) & builder.Lte(e => e.Fecha, finUtc);
            }
            else
            {
                var fechaLimiteMes = DateTime.UtcNow.Date.AddDays(-30);
                filtro &= builder.Gte(e => e.Fecha, fechaLimiteMes);
            }

            var registros = await _context.EstadisticasDiarias
                .Find(filtro)
                .SortBy(e => e.Fecha)
                .ToListAsync();

            // Procesar lista de métricas solicitadas
            var listaMetricas = string.IsNullOrWhiteSpace(metricas)
                ? new List<string> { "BPMPromedio", "Pasos" } // Métricas por defecto
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
                Fecha = e.Fecha.ToString("yyyy-MM-dd"),
                BpmPromedio = incluirBpmPromedio ? e.FrecuenciaCardiaca.Promedio : null,
                BpmMinimo = incluirBpmMinimo ? e.FrecuenciaCardiaca.Minimo : null,
                BpmMaximo = incluirBpmMaximo ? e.FrecuenciaCardiaca.Maximo : null,
                Pasos = incluirPasos ? e.Actividad.Pasos : null,
                Calorias = incluirCalorias ? e.Actividad.Calorias : null,
                DistanciaKm = incluirDistancia ? e.Actividad.DistanciaKm : null,
                HorasSueno = incluirSueno ? e.Actividad.HorasSueno : null
            }).ToList();

            return new GraficaSeriesResponseDto
            {
                IdPaciente = idPaciente,
                MetricasSolicitadas = listaMetricas,
                Series = series
            };
        }

        public async Task<GraficaSeriesColumnarResponseDto> ObtenerGraficaSeriesColumnarAsync(string idPaciente,DateTime? fechaInicio = null,DateTime? fechaFin = null, string? metricas = null)
        {
            if (string.IsNullOrWhiteSpace(idPaciente))
            {
                throw new ArgumentException("El ID del paciente es obligatorio.");
            }

            var builder = Builders<EstadisticasDiarias>.Filter;
            var filtro = builder.Eq(e => e.IdPaciente, idPaciente);

            // Rango de fechas (últimos 30 días por defecto)
            if (fechaInicio.HasValue && fechaFin.HasValue)
            {
                var inicioUtc = DateTime.SpecifyKind(fechaInicio.Value.Date, DateTimeKind.Utc);
                var finUtc = DateTime.SpecifyKind(fechaFin.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
                filtro &= builder.Gte(e => e.Fecha, inicioUtc) & builder.Lte(e => e.Fecha, finUtc);
            }
            else
            {
                var fechaLimiteMes = DateTime.UtcNow.Date.AddDays(-30);
                filtro &= builder.Gte(e => e.Fecha, fechaLimiteMes);
            }

            var registros = await _context.EstadisticasDiarias
                .Find(filtro)
                .SortBy(e => e.Fecha)
                .ToListAsync();

            // Procesar lista de métricas solicitadas desde el Query String
            var listaMetricas = string.IsNullOrWhiteSpace(metricas)
                ? new List<string> { "BPMPromedio", "BPMMinimo", "BPMMaximo" }
                : metricas.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

            var dictSeries = new Dictionary<string, object>();

            // Arreglo de fechas obligatorio
            dictSeries["fechas"] = registros.Select(e => e.Fecha.ToString("yyyy-MM-dd")).ToList();

            // Llenar dinámicamente los arreglos de las métricas solicitadas
            foreach (var metrica in listaMetricas)
            {
                var keyUpper = metrica.ToUpperInvariant();

                switch (keyUpper)
                {
                    case "BPMPROMEDIO":
                    case "PROMEDIO":
                        dictSeries["BPMPromedio"] = registros.Select(e => e.FrecuenciaCardiaca.Promedio).ToList();
                        break;

                    case "BPMMINIMO":
                    case "MINIMO":
                        dictSeries["BPMMinimo"] = registros.Select(e => e.FrecuenciaCardiaca.Minimo).ToList();
                        break;

                    case "BPMMAXIMO":
                    case "MAXIMO":
                        dictSeries["BPMMAXIMO"] = registros.Select(e => e.FrecuenciaCardiaca.Maximo).ToList();
                        break;

                    case "PASOS":
                        dictSeries["Pasos"] = registros.Select(e => e.Actividad.Pasos).ToList();
                        break;

                    case "CALORIAS":
                        dictSeries["Calorias"] = registros.Select(e => e.Actividad.Calorias).ToList();
                        break;

                    case "DISTANCIAKM":
                    case "DISTANCIA":
                        dictSeries["DistanciaKm"] = registros.Select(e => e.Actividad.DistanciaKm).ToList();
                        break;

                    case "HORASSUENO":
                    case "SUENO":
                        dictSeries["HorasSueno"] = registros.Select(e => e.Actividad.HorasSueno).ToList();
                        break;

                    case "TOTALARRITMIAS":
                    case "ARRITMIAS":
                        dictSeries["TotalArritmias"] = registros.Select(e => e.Arritmias.Total).ToList();
                        break;
                }
            }

            return new GraficaSeriesColumnarResponseDto
            {
                IdPaciente = idPaciente,
                Series = dictSeries
            };
        }

        public async Task<GraficaResumenResponseDto> ObtenerResumenKpiAsync(string idPaciente,DateTime? fechaInicio = null,DateTime? fechaFin = null, int dias = 30)
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
                var inicioUtc = DateTime.SpecifyKind(fechaInicio.Value.Date, DateTimeKind.Utc);
                var finUtc = DateTime.SpecifyKind(fechaFin.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);

                filtro &= builder.Gte(e => e.Fecha, inicioUtc) & builder.Lte(e => e.Fecha, finUtc);
                etiquetaPeriodo = $"{fechaInicio.Value:yyyy-MM-dd} a {fechaFin.Value:yyyy-MM-dd}";
            }
            else
            {
                var fechaLimite = DateTime.UtcNow.Date.AddDays(-dias);
                filtro &= builder.Gte(e => e.Fecha, fechaLimite);
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

            // Cálculos de agregados / KPI
            double promedioBpm = Math.Round(registros.Average(e => e.FrecuenciaCardiaca.Promedio), 1);
            int totalPasos = registros.Sum(e => e.Actividad.Pasos);
            int totalArritmias = registros.Sum(e => e.Arritmias.Total);
            double promedioSueno = Math.Round(registros.Average(e => e.Actividad.HorasSueno), 1);

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