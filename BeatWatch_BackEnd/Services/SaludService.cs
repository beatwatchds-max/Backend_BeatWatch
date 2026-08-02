using BeatWatch_BackEnd.Data;
using BeatWatch_BackEnd.Dtos;
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

        // 1. Consultar arritmias del periodo
        var arritmias = await _context.Arritmias
            .Find(a => a.IdPaciente == idPaciente && a.Fecha >= fechaInicio)
            .ToListAsync(cancellationToken);

        // 2. Consultar actividad diaria del periodo
        var actividades = await _context.ActividadesDiarias
            .Find(a => a.IdPaciente == idPaciente && a.FechaSincronizacion >= fechaInicio)
            .ToListAsync(cancellationToken);

        // Conteo de síntomas
        var conteoSintomas = new Dictionary<string, int>
    {
        { "Mareo", arritmias.Count(a => a.Sintomas?.Mareo == true) },
        { "Palpitaciones", arritmias.Count(a => a.Sintomas?.Palpitaciones == true) },
        { "DolorPecho", arritmias.Count(a => a.Sintomas?.DolorPecho == true) },
        { "Desmayo", arritmias.Count(a => a.Sintomas?.Desmayo == true) },
        { "FaltaAire", arritmias.Count(a => a.Sintomas?.FaltaAire == true) },
        { "Fatiga", arritmias.Count(a => a.Sintomas?.Fatiga == true) }
    };

        // Cálculos de BPM
        int bpmMaximo = arritmias.Any() ? arritmias.Max(a => a.FrecuenciaCardiaca) : 0;
        double bpmPromedio = arritmias.Any() ? Math.Round(arritmias.Average(a => a.FrecuenciaCardiaca), 1) : 0;

        // Porcentaje de días estables (días en el rango sin ningún episodio registrado)
        var diasConEpisodios = arritmias.Select(a => a.Fecha.ToString("yyyy-MM-dd")).Distinct().Count();
        double porcentajeEstables = dias > 0
            ? Math.Round(((double)(dias - diasConEpisodios) / dias) * 100, 1)
            : 100.0;

        // Gráfica de picos por día
        var graficaPicos = arritmias
            .GroupBy(a => a.Fecha.ToString("yyyy-MM-DD"))
            .Select(g => new PuntoGraficaDto
            {
                Fecha = g.Key,
                BpmMaximo = g.Max(x => x.FrecuenciaCardiaca),
                BpmPromedio = (int)Math.Round(g.Average(x => x.FrecuenciaCardiaca))
            })
            .OrderBy(p => p.Fecha)
            .ToList();

        return new ResumenTableroDto
        {
            TotalEpisodiosPeriodo = arritmias.Count,
            BpmPromedio = bpmPromedio,
            BpmMaximo = bpmMaximo,
            PorcentajeDiasEstables = Math.Max(0, porcentajeEstables),
            ConteoSintomas = conteoSintomas,
            TotalPasos = actividades.Sum(a => a.Pasos),
            TotalCalorias = Math.Round(actividades.Sum(a => a.Calorias), 1),
            TotalDistanciaKm = Math.Round(actividades.Sum(a => a.DistanciaKm), 2),
            GraficaPicos = graficaPicos
        };
    }
}
