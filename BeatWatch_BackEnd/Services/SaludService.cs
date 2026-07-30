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

    public async Task<IReadOnlyList<Arritmia>> ObtenerHistorialArritmiasAsync(string idPaciente, CancellationToken cancellationToken)
    {
        return await _context.Arritmias
            .Find(arritmia => arritmia.IdPaciente == idPaciente)
            .Sort(Builders<Arritmia>.Sort.Descending(arritmia => arritmia.Fecha))
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
}
