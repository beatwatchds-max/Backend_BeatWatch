using BeatWatch_BackEnd.Data;
using BeatWatch_BackEnd.Dtos;
using BeatWatch_BackEnd.Models;
using MongoDB.Driver;
using System.Security.Cryptography;

namespace BeatWatch_BackEnd.Services
{
    public class PacienteService
    {
        private readonly MongoDbContext _context;

        public PacienteService(MongoDbContext context)
        {
            _context = context;
        }

        // Método privado para generar y garantizar la unicidad del token de 9 dígitos
        private async Task<string> GenerarTokenNumericoUnicoAsync()
        {
            string tokenGenerado;
            bool tokenExiste;

            do
            {
                // Genera un número aleatorio seguro entre 100,000,000 y 999,999,999
                int numeroAleatorio = RandomNumberGenerator.GetInt32(100000000, 999999999);
                tokenGenerado = numeroAleatorio.ToString();

                // Validar contra la base de datos que no exista ya (Unicidad)
                var filter = Builders<Usuario>.Filter.Eq(u => u.TokenMovil, tokenGenerado);
                tokenExiste = await _context.Usuarios.Find(filter).AnyAsync();

            } while (tokenExiste); // Si por algún milagro se repite, genera uno nuevo

            return tokenGenerado;
        }

        public async Task<Usuario> RegistrarPacienteAsync(CrearPacienteDto pacienteDto)
        {
            // 1. Generar el token de 9 dígitos
            string nuevoToken = await GenerarTokenNumericoUnicoAsync();

            // 2. Crear el objeto del nuevo paciente
            var nuevoPaciente = new Usuario
            {
                Nombre = pacienteDto.NombreCompleto,
                Correo = pacienteDto.Correo,
                Telefono = pacienteDto.Telefono,
               Rol = "Paciente",
                TokenMovil = nuevoToken, // Guardado seguro en el documento
                Activo = true,
                FechaCreacion = DateTime.UtcNow
                // Nota: Asigna aquí cualquier otro campo obligatorio de tu modelo Usuario
            };

            // 3. Insertar en MongoDB
            await _context.Usuarios.InsertOneAsync(nuevoPaciente);

            return nuevoPaciente;
        }

        public async Task<Paciente> CrearPerfilAsync(CrearPerfilPacienteDto perfilDto)
        {
            var curp = perfilDto.CURP.Trim().ToUpperInvariant();
            var tipoSangre = perfilDto.TipoSangre.Trim().ToUpperInvariant();

            if (await _context.Pacientes.Find(p => p.CURP == curp).AnyAsync())
            {
                throw new InvalidOperationException("Ya existe un paciente registrado con esta CURP.");
            }

            var licencia = await _context.Licencias.Find(l => l.Id == perfilDto.IdLicencia).FirstOrDefaultAsync();
            if (licencia is null || !licencia.Activa || licencia.FechaFin < DateTime.UtcNow)
            {
                throw new ArgumentException("La licencia indicada no existe o no esta activa.");
            }

            var paciente = new Paciente
            {
                CURP = curp,
                Edad = perfilDto.Edad,
                Sexo = perfilDto.Sexo.Trim(),
                Peso = perfilDto.Peso,
                Estatura = perfilDto.Estatura,
                TipoSangre = tipoSangre,
                IdLicencia = perfilDto.IdLicencia,
                Fotografia = perfilDto.Fotografia
            };

            try
            {
                await _context.Pacientes.InsertOneAsync(paciente);
            }
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                throw new InvalidOperationException("Ya existe un paciente registrado con esta CURP.", ex);
            }

            return paciente;
        }
    }
}
