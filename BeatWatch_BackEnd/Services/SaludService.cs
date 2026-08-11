using BeatWatch_BackEnd.Data;
using BeatWatch_BackEnd.Dtos.arritmia;
using BeatWatch_BackEnd.Dtos.historial;
using BeatWatch_BackEnd.infrescture;
using BeatWatch_BackEnd.Models;
using MongoDB.Driver;

namespace BeatWatch_BackEnd.Services;

public class SaludService : ISaludService
{
    private readonly MongoDbContext _context;

    public SaludService(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<Arritmia> RegistrarArritmiaAsync(RegistrarArritmiaDto solicitud, CancellationToken cancellationToken)
    {
        var arritmia = new Arritmia
        {
            Tipo = solicitud.Tipo,
            FrecuenciaCardiaca = solicitud.FrecuenciaCardiaca,
            DuracionEpisodioSeconds = solicitud.DuracionEpisodioSeconds,
            IdPaciente = solicitud.IdPaciente,
            Sintomas = new Sintomas
            {
                Mareo = solicitud.Sintomas.Mareo,
                Palpitaciones = solicitud.Sintomas.Palpitaciones,
                DolorPecho = solicitud.Sintomas.DolorPecho,
                Desmayo = solicitud.Sintomas.Desmayo,
                FaltaAire = solicitud.Sintomas.FaltaAire,
                Fatiga = solicitud.Sintomas.Fatiga
            },
            // 🟢 NUEVO: Mapeo de Factores de Riesgo al crear la entidad
            FactoresRiesgo = new FactoresRiesgo
            {
                HipertensionArterial = solicitud.FactoresRiesgo.HipertensionArterial,
                ObesidadImcElevado = solicitud.FactoresRiesgo.ObesidadImcElevado,
                ApneaSueno = solicitud.FactoresRiesgo.ApneaSueno,
                Tabaquismo = solicitud.FactoresRiesgo.Tabaquismo,
                Alcoholismo = solicitud.FactoresRiesgo.Alcoholismo,
                EstresCronico = solicitud.FactoresRiesgo.EstresCronico
            },
            Fecha = DateTime.UtcNow
        };

        // Continúa haciendo la inserción atómica en un solo documento
        await _context.Arritmias.InsertOneAsync(arritmia, cancellationToken: cancellationToken);
        return arritmia;
    }

    public async Task<IReadOnlyList<EpisodioArritmia>> ObtenerHistorialArritmiasAsync(string idPaciente, CancellationToken cancellationToken)
    {
        return await _context.EpisodiosArritmia
            .Find(episodio => episodio.IdPaciente == idPaciente)
            .Sort(Builders<EpisodioArritmia>.Sort.Descending(episodio => episodio.Fecha))
            .ToListAsync(cancellationToken);
    }
    public async Task<EpisodioArritmia> RegistrarAlertaFrecuenciaAsync(RegistrarAlertaFrecuenciaDto solicitud, CancellationToken cancellationToken)
    {
        var episodio = new EpisodioArritmia
        {
            IdPaciente = solicitud.IdPaciente,
            TipoAnomalia = solicitud.TipoAnomalia,
            FrecuenciaCardiaca = solicitud.FrecuenciaCardiaca,
            DuracionEpisodioSeconds = solicitud.DuracionEpisodioSeconds,
            EsAlertaCritica = true,
            Fecha = DateTime.UtcNow
        };

        await _context.EpisodiosArritmia.InsertOneAsync(episodio, cancellationToken: cancellationToken);
        return episodio;
    }

    public async Task RegistrarActividadDiariaAsync(RegistrarActividadDiariaDto solicitud, CancellationToken cancellationToken)
    {
        var filter = Builders<ActividadDiaria>.Filter.And(
            Builders<ActividadDiaria>.Filter.Eq(a => a.IdPaciente, solicitud.IdPaciente),
            Builders<ActividadDiaria>.Filter.Eq(a => a.FechaCorta, solicitud.Fecha)
        );

        var update = Builders<ActividadDiaria>.Update
     .Set(a => a.Pasos, solicitud.Pasos)
     .Set(a => a.Calorias, solicitud.Calorias)
     .Set(a => a.DistanciaKm, solicitud.DistanciaKm)
     .Set(a => a.HorasSueno, solicitud.HorasSueno)
     .Set(a => a.FechaSincronizacion, DateTime.UtcNow)
     .SetOnInsert(a => a.IdPaciente, solicitud.IdPaciente)
     .SetOnInsert(a => a.FechaCorta, solicitud.Fecha);

        // Upsert = true: si existe lo actualiza, si no existe lo crea.
        var options = new UpdateOptions { IsUpsert = true };

        await _context.ActividadesDiarias.UpdateOneAsync(filter, update, options, cancellationToken);
    }

    public async Task<ResumenTableroDto> ObtenerResumenTableroAsync(string idPaciente, int dias, CancellationToken cancellationToken)
    {
        var fechaInicio = DateTime.UtcNow.Date.AddDays(-dias);

        // 1. Obtener episodios automáticos del reloj
        var episodios = await _context.EpisodiosArritmia
            .Find(e => e.IdPaciente == idPaciente && e.Fecha >= fechaInicio)
            .ToListAsync(cancellationToken);

        // 2. Obtener registros manuales con síntomas
        var arritmiasManuales = await _context.Arritmias
            .Find(a => a.IdPaciente == idPaciente && a.Fecha >= fechaInicio)
            .ToListAsync(cancellationToken);

        // Conteo de síntomas (tomados de los formularios manuales en Arritmias)
        var conteoSintomas = new Dictionary<string, int>
    {
        { "Palpitaciones", arritmiasManuales.Count(a => a.Sintomas?.Palpitaciones == true) },
        { "Mareo", arritmiasManuales.Count(a => a.Sintomas?.Mareo == true) },
        { "Fatiga", arritmiasManuales.Count(a => a.Sintomas?.Fatiga == true) },
        { "FaltaAire", arritmiasManuales.Count(a => a.Sintomas?.FaltaAire == true) },
        { "DolorPecho", arritmiasManuales.Count(a => a.Sintomas?.DolorPecho == true) },
        { "Desmayo", arritmiasManuales.Count(a => a.Sintomas?.Desmayo == true) }
    };

        // Combinar BPM de ambas fuentes para la estadística global
        var todosLosBpm = episodios.Select(e => e.FrecuenciaCardiaca)
            .Concat(arritmiasManuales.Select(a => a.FrecuenciaCardiaca))
            .ToList();

        int totalEpisodios = episodios.Count + arritmiasManuales.Count;
        int bpmMaximo = todosLosBpm.Any() ? todosLosBpm.Max() : 0;
        double bpmPromedio = todosLosBpm.Any() ? Math.Round(todosLosBpm.Average(), 1) : 0;

        // Fechas con alguna anomalía para calcular % de días estables
        var fechasConIncidencia = episodios.Select(e => e.Fecha.Date)
            .Concat(arritmiasManuales.Select(a => a.Fecha.Date))
            .Distinct()
            .Count();

        double porcentajeEstables = dias > 0
            ? Math.Round(((double)(dias - fechasConIncidencia) / dias) * 100, 1)
            : 100.0;

        return new ResumenTableroDto
        {
            TotalEpisodiosPeriodo = totalEpisodios,
            BpmPromedio = bpmPromedio,
            BpmMaximo = bpmMaximo,
            PorcentajeDiasEstables = Math.Max(0, porcentajeEstables),
            ConteoSintomas = conteoSintomas
        };
    }
}
