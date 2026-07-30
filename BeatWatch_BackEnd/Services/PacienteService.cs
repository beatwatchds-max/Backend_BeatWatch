using BeatWatch_BackEnd.Data;
using BeatWatch_BackEnd.Dtos;
using BeatWatch_BackEnd.Models;
using MongoDB.Bson;
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
            // 1. Validar que la licencia exista antes de asociar
            if (!string.IsNullOrWhiteSpace(pacienteDto.IdLicencia))
            {
                if (!ObjectId.TryParse(pacienteDto.IdLicencia, out _))
                {
                    throw new ArgumentException("El IdLicencia proporcionado no tiene un formato válido.");
                }

                var licenciaExiste = await _context.Licencias
                    .Find(l => l.Id == pacienteDto.IdLicencia && l.Activa)
                    .AnyAsync();

                if (!licenciaExiste)
                {
                    throw new ArgumentException("La licencia especificada no existe o no se encuentra activa.");
                }
            }

            // 2. Generar el token de 9 dígitos
            string nuevoToken = await GenerarTokenNumericoUnicoAsync();

            // 3. Crear el objeto del nuevo paciente (sin guardar IdLicencia en el usuario)
            var nuevoPaciente = new Usuario
            {
                Nombre = pacienteDto.NombreCompleto,
                Correo = pacienteDto.Correo,
                Telefono = pacienteDto.Telefono,
                Rol = "Paciente",
                TokenMovil = nuevoToken,
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            };

            // 4. Insertar el Usuario en MongoDB
            await _context.Usuarios.InsertOneAsync(nuevoPaciente);

            // 🟢 5. Vincular el ID del usuario recién creado a la Licencia (Lista de UsuariosAsociados)
            if (!string.IsNullOrWhiteSpace(pacienteDto.IdLicencia))
            {
                var filterLicencia = Builders<Licencia>.Filter.Eq(l => l.Id, pacienteDto.IdLicencia);
                var updateLicencia = Builders<Licencia>.Update.Push(l => l.UsuariosAsociados, nuevoPaciente.Id);

                await _context.Licencias.UpdateOneAsync(filterLicencia, updateLicencia);
            }

            return nuevoPaciente;
        }

        public async Task<Paciente> CrearPerfilAsync(CrearPerfilPacienteDto perfilDto)
        {
            var curp = perfilDto.CURP.Trim().ToUpperInvariant();
            var tipoSangre = perfilDto.TipoSangre.Trim().ToUpperInvariant();

            // Validar que el Usuario exista
            if (!ObjectId.TryParse(perfilDto.UsuarioId, out _))
            {
                throw new ArgumentException("El UsuarioId no tiene un formato de ObjectId válido.");
            }

            var usuarioExiste = await _context.Usuarios.Find(u => u.Id == perfilDto.UsuarioId).AnyAsync();
            if (!usuarioExiste)
            {
                throw new ArgumentException("El usuario especificado no existe.");
            }

            if (await _context.Pacientes.Find(p => p.CURP == curp).AnyAsync())
            {
                throw new InvalidOperationException("Ya existe un paciente registrado con esta CURP.");
            }

            var licencia = await _context.Licencias.Find(l => l.Id == perfilDto.IdLicencia).FirstOrDefaultAsync();
            if (licencia is null || !licencia.Activa || licencia.FechaFin < DateTime.UtcNow)
            {
                throw new ArgumentException("La licencia indicada no existe o no está activa.");
            }

            var paciente = new Paciente
            {
                UsuarioId = perfilDto.UsuarioId, // 🟢 Asignamos la relación
                CURP = curp,
                Edad = perfilDto.Edad,
                Sexo = perfilDto.Sexo.Trim(),
                Peso = perfilDto.Peso,
                Estatura = perfilDto.Estatura,
                TipoSangre = tipoSangre,
                IdLicencia = perfilDto.IdLicencia,
                Fotografia = perfilDto.Fotografia,
                FechaNacimiento = perfilDto.FechaNacimiento,
                Direccion = perfilDto.Direccion
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

        public async Task<Paciente?> ObtenerPorUsuarioIdAsync(string usuarioId)
        {
            return await _context.Pacientes
                .Find(p => p.UsuarioId == usuarioId)
                .FirstOrDefaultAsync();
        }
    }
}
