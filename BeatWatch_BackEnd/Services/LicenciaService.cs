using BeatWatch_BackEnd.Models;
using BeatWatch_BackEnd.Data;
using MongoDB.Driver;
using MongoDB.Bson;

namespace BeatWatch_BackEnd.Services
{
    public class LicenciaService : ILicenciaService
    {
        private readonly MongoDbContext _context;

        public LicenciaService(MongoDbContext context)
        {
            _context = context;
        }

        public async Task<Licencia?> ActivarLicenciaGratuitaAsync(ActivarLicenciaGratuitaDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.CorreoElectronico) && string.IsNullOrWhiteSpace(dto.UsuarioId))
            {
                throw new ArgumentException("Se requiere el correo electrónico o el ID del usuario para activar la licencia.");
            }

            Usuario? usuario = null;

            // 1. Intentar buscar por ID si se envió y es válido
            if (!string.IsNullOrWhiteSpace(dto.UsuarioId) && ObjectId.TryParse(dto.UsuarioId, out _))
            {
                usuario = await _context.Usuarios.Find(u => u.Id == dto.UsuarioId).FirstOrDefaultAsync();
            }

            // 2. Si no se encontró por ID (o no se envió), buscar por Correo (Dato de la pantalla)
            if (usuario == null && !string.IsNullOrWhiteSpace(dto.CorreoElectronico))
            {
                usuario = await _context.Usuarios.Find(u => u.Correo == dto.CorreoElectronico).FirstOrDefaultAsync();
            }

            if (usuario == null)
            {
                throw new ArgumentException("El usuario proporcionado no existe en el sistema.");
            }

            var usuarioId = usuario.Id!;

            if (await _context.Licencias.Find(l => l.UsuarioId == usuarioId && l.Activa && l.MetodoPago == "Gratuito").AnyAsync())
            {
                throw new InvalidOperationException("El usuario ya tiene una licencia gratuita activa.");
            }

            // Generar Código de Grupo único para el Plan Grupal Gratuito
            string codigoGrupoUnico = $"BW-GR-{Guid.NewGuid().ToString()[..8].ToUpper()}";

            var fechaInicio = DateTime.UtcNow;
            var fechaFin = fechaInicio.AddYears(1);

            var nuevaLicencia = new Licencia
            {
                UsuarioId = usuarioId,
                UsuariosAsociados = new List<string> { usuarioId },
                Tipo = "Grupal",
                CodigoGrupo = codigoGrupoUnico,
                FechaInicio = fechaInicio,
                FechaFin = fechaFin,
                MetodoPago = "Gratuito",
                EstadoPago = "Aprobado",
                Activa = true
            };

            await _context.Licencias.InsertOneAsync(nuevaLicencia);

            // Activar el estado del Usuario y Vincular la nueva Licencia
            var filter = Builders<Usuario>.Filter.Eq(u => u.Id, usuarioId);
            var update = Builders<Usuario>.Update
                .Set(u => u.Activo, true)
                .Set(u => u.IdLicencia, nuevaLicencia.Id);

            await _context.Usuarios.UpdateOneAsync(filter, update);

            return nuevaLicencia;
        }
    }
}