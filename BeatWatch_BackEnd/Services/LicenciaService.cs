using BeatWatch_BackEnd.Models;
using BeatWatch_BackEnd.Data;
using MongoDB.Driver;

namespace BeatWatch_BackEnd.Services
{
    public class LicenciaService : ILicenciaService
    {
        private readonly MongoDbContext _context;

        // Inyectamos tu clase de contexto real directamente
        public LicenciaService(MongoDbContext context)
        {
            _context = context;
        }

        public async Task<Licencia?> ProcesarPagoYCrearLicenciaAsync(PagoSimuladoDto pagoDto)
        {
            // 1. Validar tipo de plan (HU2.2)
            if (pagoDto.TipoLicencia != "Individual" && pagoDto.TipoLicencia != "Grupal")
            {
                throw new ArgumentException("Tipo de licencia no soportado.");
            }

            // 2. Determinar estado inicial según el método seleccionado en la maqueta
            string estadoPago = "Aprobado";
            bool licenciaActiva = true;

            if (pagoDto.MetodoPago.ToUpper() == "OXXO")
            {
                estadoPago = "Pendiente";
                licenciaActiva = false; // Requiere pago físico en tienda
            }
            else if (pagoDto.MetodoPago.ToUpper() == "TARJETA")
            {
                // Simulación de validación bancaria real basada en la maqueta
                if (string.IsNullOrWhiteSpace(pagoDto.NumeroTarjeta) ||
                    string.IsNullOrWhiteSpace(pagoDto.NombreTitular) ||
                    string.IsNullOrWhiteSpace(pagoDto.FechaExpiracion) ||
                    string.IsNullOrWhiteSpace(pagoDto.Cvv))
                {
                    throw new ArgumentException("Todos los campos de la tarjeta son obligatorios para procesar el pago.");
                }

                if (pagoDto.Cvv.Length < 3 || pagoDto.Cvv.Length > 4)
                {
                    throw new ArgumentException("El código CVV no es válido (Debe tener 3 o 4 dígitos).");
                }
            }

            // 3. Generar Código de Grupo único si es Plan Grupal
            string codigoGrupoUnico = pagoDto.TipoLicencia == "Grupal"
                ? $"BW-GR-{Guid.NewGuid().ToString()[..8].ToUpper()}"
                : "INDIVIDUAL-NONE";

            // 4. Calcular vigencia (1 mes)
            var fechaInicio = DateTime.UtcNow;
            var fechaFin = fechaInicio.AddMonths(1);

            var nuevaLicencia = new Licencia
            {
                UsuarioId = pagoDto.UsuarioId,
                Tipo = pagoDto.TipoLicencia, // Mapeado a tu propiedad 'Tipo'
                CodigoGrupo = codigoGrupoUnico,
                FechaInicio = fechaInicio,
                FechaFin = fechaFin,
                MetodoPago = pagoDto.MetodoPago,
                EstadoPago = estadoPago,
                Activa = licenciaActiva
            };

            // Guardar usando la propiedad directa de tu MongoDbContext
            await _context.Licencias.InsertOneAsync(nuevaLicencia);

            // 5. HU2.3: Actualizar el estado del usuario a Activo (si aplica)
            // 5. HU2.3: Actualizar el estado del usuario a Activo y Vincular el IdLicencia
            if (licenciaActiva)
            {
                var filter = Builders<Usuario>.Filter.Eq(u => u.Id, pagoDto.UsuarioId);

                // Asignamos Activo = true Y guardamos la IdLicencia recién generada
                var update = Builders<Usuario>.Update
                    .Set(u => u.Activo, true)
                    .Set(u => u.IdLicencia, nuevaLicencia.Id);

                await _context.Usuarios.UpdateOneAsync(filter, update);
            }

            return nuevaLicencia;
        }
    }
}