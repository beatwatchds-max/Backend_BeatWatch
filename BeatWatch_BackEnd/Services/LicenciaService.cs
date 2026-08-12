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

        public async Task<Licencia?> ActivarLicenciaGratuitaAsync(string usuarioId)
        {
            if (!ObjectId.TryParse(usuarioId, out _)) throw new ArgumentException("El usuario autenticado no tiene un identificador válido.");

            // The identity must come from the authenticated JWT, never from client input.
            var usuario = await _context.Usuarios
                .Find(u => u.Id == usuarioId)
                .FirstOrDefaultAsync();

            if (usuario == null)
            {
                throw new ArgumentException("El usuario autenticado no existe en el sistema.");
            }

            if (await _context.Licencias.Find(l => l.UsuarioId == usuarioId && l.Activa && l.MetodoPago == "Gratuito").AnyAsync())
            {
                throw new InvalidOperationException("El usuario ya tiene una licencia gratuita activa.");
            }

            // 2. Generar Código de Grupo único para el Plan Grupal Gratuito
            string codigoGrupoUnico = $"BW-GR-{Guid.NewGuid().ToString()[..8].ToUpper()}";

            // 3. Vigencia por defecto de 1 año (o 1 mes según tus reglas de negocio)
            var fechaInicio = DateTime.UtcNow;
            var fechaFin = fechaInicio.AddYears(1);

            var nuevaLicencia = new Licencia
            {
                UsuarioId = usuario.Id!,
                UsuariosAsociados = new List<string> { usuario.Id! },
                Tipo = "Grupal",
                CodigoGrupo = codigoGrupoUnico,
                FechaInicio = fechaInicio,
                FechaFin = fechaFin,
                MetodoPago = "Gratuito",
                EstadoPago = "Aprobado",
                Activa = true
            };

            // Guardar en MongoDB
            await _context.Licencias.InsertOneAsync(nuevaLicencia);

            // 4. Activar el estado del Usuario y Vincular la nueva Licencia
            var filter = Builders<Usuario>.Filter.Eq(u => u.Id, usuario.Id);
            var update = Builders<Usuario>.Update
                .Set(u => u.Activo, true)
                .Set(u => u.IdLicencia, nuevaLicencia.Id);

            await _context.Usuarios.UpdateOneAsync(filter, update);

            return nuevaLicencia;
        }
    }
}
