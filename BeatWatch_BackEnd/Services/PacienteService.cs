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
    }
}