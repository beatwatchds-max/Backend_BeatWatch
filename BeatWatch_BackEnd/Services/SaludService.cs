using BeatWatch_BackEnd.Data;
using BeatWatch_BackEnd.Dtos;
using BeatWatch_BackEnd.infrescture;
using BeatWatch_BackEnd.Models;

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
            Fecha = DateTime.UtcNow
        };

        // A single MongoDB insert persists the reading and its symptom subdocument atomically.
        await _context.Arritmias.InsertOneAsync(arritmia, cancellationToken: cancellationToken);
        return arritmia;
    }
}
