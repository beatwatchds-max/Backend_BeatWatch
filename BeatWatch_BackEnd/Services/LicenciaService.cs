using BeatWatch_BackEnd.Models;
using BeatWatch_BackEnd.Data;
using MongoDB.Driver;

namespace BeatWatch_BackEnd.Services
{
    public class LicenciaService : ILicenciaService
    {
        private readonly MongoDbContext _context;

        public LicenciaService(MongoDbContext context)
        {
            _context = context;
        }

        public async Task<Licencia?> ProcesarPagoYCrearLicenciaAsync(ActivarLicenciaGratuitaDto dto)
        {
            // 1. Buscar al usuario por ID o por Correo para validar existencia
            var usuario = await _context.Usuarios
                .Find(u => u.Id == dto.UsuarioId || u.Correo == dto.CorreoElectronico)
                .FirstOrDefaultAsync();

            if (usuario == null)
            {
                throw new ArgumentException("El usuario proporcionado no existe en el sistema.");
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