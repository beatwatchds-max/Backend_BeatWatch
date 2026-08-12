using BeatWatch_BackEnd.Data;
using BeatWatch_BackEnd.Dtos.pacientesDtos;
using BeatWatch_BackEnd.infrescture;
using BeatWatch_BackEnd.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Security.Cryptography;

namespace BeatWatch_BackEnd.Services
{
    public class PacienteService : IPacienteService
    {
        private readonly MongoDbContext _context;

        public PacienteService(MongoDbContext context)
        {
            _context = context;
        }

        #region metodo auxiliar para generar token movil unico
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
        #endregion

        #region metodos para registrar paciente y perfil
        public async Task<Usuario> RegistrarPacienteAsync(CrearPacienteDto pacienteDto, string idLicencia)
        {
            // 1. Validar la Licencia
            if (string.IsNullOrWhiteSpace(idLicencia) || !ObjectId.TryParse(idLicencia, out _))
            {
                throw new ArgumentException("La licencia del usuario autenticado no es válida.");
            }

            var licenciaExiste = await _context.Licencias
                .Find(l => l.Id == idLicencia && l.Activa)
                .AnyAsync();

            if (!licenciaExiste)
            {
                throw new ArgumentException("La licencia especificada no existe o no se encuentra activa.");
            }

            // 2. Generar TokenMovil único
            string nuevoToken = await GenerarTokenNumericoUnicoAsync();

            // 3. Validar los Cuidadores provistos
            var cuidadoresValidos = new List<string>();
            if (pacienteDto.CuidadoresIds != null && pacienteDto.CuidadoresIds.Any())
            {
                foreach (var cId in pacienteDto.CuidadoresIds)
                {
                    if (ObjectId.TryParse(cId, out _))
                    {
                        cuidadoresValidos.Add(cId);
                    }
                }
            }

            // 4. Crear el Usuario del Paciente guardando la lista de cuidadores
            var nuevoPacienteUsuario = new Usuario
            {
                Nombre = pacienteDto.NombreCompleto,
                Correo = pacienteDto.Correo.Trim().ToLowerInvariant(),
                Telefono = pacienteDto.Telefono ?? string.Empty,
                Rol = "Paciente",
                TokenMovil = nuevoToken,
                Activo = true,
                FechaCreacion = DateTime.UtcNow,
                IdLicencia = idLicencia,
                Cuidadores = cuidadoresValidos // 👈 Guardamos la relación de cuidadores
            };

            await _context.Usuarios.InsertOneAsync(nuevoPacienteUsuario);

            // 5. Vincular a UsuariosAsociados de la Licencia
            var filterLicencia = Builders<Licencia>.Filter.Eq(l => l.Id, idLicencia);
            var updateLicencia = Builders<Licencia>.Update.Push(l => l.UsuariosAsociados, nuevoPacienteUsuario.Id);
            await _context.Licencias.UpdateOneAsync(filterLicencia, updateLicencia);

            return nuevoPacienteUsuario;
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

        public async Task<(Usuario Usuario, Paciente Paciente)> RegistrarPacienteCompletoAsync(RegistrarPacienteCompletoDto dto,  string idLicencia)
        {
            var curp = dto.CURP.Trim().ToUpperInvariant();
            var tipoSangre = dto.TipoSangre.Trim().ToUpperInvariant();

            // 1. Validar la Licencia
            if (string.IsNullOrWhiteSpace(idLicencia) || !ObjectId.TryParse(idLicencia, out _))
            {
                throw new ArgumentException("La licencia asociada a la sesión no es válida.");
            }

            var licencia = await _context.Licencias
                .Find(l => l.Id == idLicencia && l.Activa)
                .FirstOrDefaultAsync();

            if (licencia is null || licencia.FechaFin < DateTime.UtcNow)
            {
                throw new ArgumentException("La licencia indicada no existe o no se encuentra activa.");
            }

            // 2. Validar Unicidad de CURP
            if (await _context.Pacientes.Find(p => p.CURP == curp).AnyAsync())
            {
                throw new InvalidOperationException("Ya existe un paciente registrado con esta CURP.");
            }

            // 3. Validar y filtrar formato de los IDs de Cuidadores recibidos
            var cuidadoresValidos = new List<string>();
            if (dto.CuidadoresIds != null && dto.CuidadoresIds.Any())
            {
                foreach (var cId in dto.CuidadoresIds)
                {
                    if (ObjectId.TryParse(cId, out _))
                    {
                        cuidadoresValidos.Add(cId);
                    }
                }
            }

            // 4. Generar Token Único de 9 dígitos para la app móvil
            string tokenMovil = await GenerarTokenNumericoUnicoAsync();

            // 5. Crear e insertar la cuenta de Usuario (AQUÍ se guardan los cuidadores)
            var nuevoUsuario = new Usuario
            {
                Nombre = dto.NombreCompleto,
                Correo = dto.Correo.Trim().ToLowerInvariant(),
                Telefono = dto.Telefono ?? string.Empty,
                Rol = "Paciente",
                TokenMovil = tokenMovil,
                Activo = true,
                FechaCreacion = DateTime.UtcNow,
                IdLicencia = idLicencia,
                Cuidadores = cuidadoresValidos // 👈 Se asignan únicamente a la colección Usuarios
            };

            await _context.Usuarios.InsertOneAsync(nuevoUsuario);

            // 6. Vincular el Usuario a la Licencia
            var filterLicencia = Builders<Licencia>.Filter.Eq(l => l.Id, idLicencia);
            var updateLicencia = Builders<Licencia>.Update.Push(l => l.UsuariosAsociados, nuevoUsuario.Id);
            await _context.Licencias.UpdateOneAsync(filterLicencia, updateLicencia);

            // 7. Convertir Fotografía (si aplica Base64)
            byte[]? fotoBytes = !string.IsNullOrEmpty(dto.Fotografia)
                ? Convert.FromBase64String(dto.Fotografia)
                : null;

            // 8. Crear e insertar la entidad Paciente (sólo perfil clínico, vinculado mediante UsuarioId)
            var nuevoPaciente = new Paciente
            {
                UsuarioId = nuevoUsuario.Id!,
                CURP = curp,
                Edad = dto.Edad,
                Sexo = dto.Sexo.Trim(),
                Peso = dto.Peso,
                Estatura = dto.Estatura,
                TipoSangre = tipoSangre,
                IdLicencia = idLicencia,
                Fotografia = fotoBytes,
                FechaNacimiento = dto.FechaNacimiento,
                Direccion = dto.Direccion ?? string.Empty
            };

            try
            {
                await _context.Pacientes.InsertOneAsync(nuevoPaciente);
            }
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                throw new InvalidOperationException("Ya existe un paciente registrado con esta CURP.", ex);
            }

            return (nuevoUsuario, nuevoPaciente);
        }
        #endregion

        #region metodos para obtener detalle de paciente
        public async Task<DetallePacienteResponseDto?> ObtenerDetallePorUsuarioIdAsync(string usuarioId)
        {
            if (!ObjectId.TryParse(usuarioId, out _))
            {
                throw new ArgumentException("El identificador de usuario no es válido.");
            }

            // 1. Obtener la cuenta de Usuario logueado
            var usuarioCuenta = await _context.Usuarios
                .Find(u => u.Id == usuarioId)
                .FirstOrDefaultAsync();

            if (usuarioCuenta == null) return null;

            Paciente? paciente = null;
            var esPaciente = string.Equals(usuarioCuenta.Rol, "Paciente", StringComparison.OrdinalIgnoreCase);

            // 2. Si es Paciente, buscar directamente por UsuarioId
            if (esPaciente)
            {
                paciente = await _context.Pacientes
                    .Find(p => p.UsuarioId == usuarioId)
                    .FirstOrDefaultAsync();
            }
            else
            {
                // 🟢 Si es Administrador o Cuidador:
                // A. Intentar buscar por IdLicencia
                if (!string.IsNullOrEmpty(usuarioCuenta.IdLicencia))
                {
                    paciente = await _context.Pacientes
                        .Find(p => p.IdLicencia == usuarioCuenta.IdLicencia)
                        .FirstOrDefaultAsync();
                }

                // B. Fallback: Si no tiene IdLicencia o no encontró, buscar si el usuario es creador o tomar el primer paciente
                if (paciente == null)
                {
                    paciente = await _context.Pacientes.Find(_ => true).FirstOrDefaultAsync();
                }

                // C. Si encontramos al paciente de la licencia, cargamos la cuenta de Usuario del Paciente para sus datos de contacto
                if (paciente != null)
                {
                    var usuarioPaciente = await _context.Usuarios
                        .Find(u => u.Id == paciente.UsuarioId)
                        .FirstOrDefaultAsync();

                    if (usuarioPaciente != null)
                    {
                        usuarioCuenta = usuarioPaciente;
                    }
                }
            }

            if (paciente == null) return null;

            // 3. Obtener Cuidadores asignados
            var cuidadoresIds = usuarioCuenta?.Cuidadores ?? new List<string>();
            var cuidadoresList = new List<CuidadorInfoDto>();

            if (cuidadoresIds.Any())
            {
                var filterCuidadores = Builders<Usuario>.Filter.In(u => u.Id, cuidadoresIds);
                cuidadoresList = await _context.Usuarios
                    .Find(filterCuidadores)
                    .Project(u => new CuidadorInfoDto
                    {
                        Nombre = u.Nombre
                    })
                    .ToListAsync();
            }

            // 4. Consultar Arritmias del Paciente
            var arritmias = await _context.Arritmias
                .Find(a => a.IdPaciente == paciente.Id)
                .ToListAsync();

            // 🟢 Extraer el Diagnóstico principal del primer registro de arritmia
            string diagnosticoPrincipal = arritmias.FirstOrDefault()?.Tipo ?? "Sin diagnóstico registrado";

            // 5. Mapeo final garantizando datos del Paciente
            return new DetallePacienteResponseDto
            {
                PacienteId = paciente.Id!,
                UsuarioId = paciente.UsuarioId,
                NombreCompleto = usuarioCuenta?.Nombre ?? string.Empty,
                Correo = usuarioCuenta?.Correo ?? string.Empty,
                Telefono = usuarioCuenta?.Telefono ?? string.Empty,
                Diagnostico = diagnosticoPrincipal, // 🟢 Ahora se envía explícito
                CURP = paciente.CURP,
                Edad = paciente.Edad,
                Sexo = paciente.Sexo,
                Peso = paciente.Peso,
                Estatura = paciente.Estatura,
                FechaNacimiento = paciente.FechaNacimiento,
                Direccion = paciente.Direccion,
                TipoSangre = paciente.TipoSangre,
                IdLicencia = paciente.IdLicencia,
                Fotografia = paciente.Fotografia,
                Rol = usuarioCuenta?.Rol ?? "Paciente",
                Cuidadores = cuidadoresList,
                CondicionesArritmias = arritmias.Cast<object>().ToList()
            };
        }

        public async Task<DetallePacienteResponseDto?> ObtenerDetallePorPacienteIdAsync(string pacienteId)
        {
            if (!ObjectId.TryParse(pacienteId, out _))
            {
                throw new ArgumentException("El identificador de paciente no es válido.");
            }

            var paciente = await _context.Pacientes.Find(p => p.Id == pacienteId).FirstOrDefaultAsync();
            return paciente is null ? null : await ObtenerDetallePorUsuarioIdAsync(paciente.UsuarioId);
        }


        public async Task<bool> ActualizarPerfilPacienteAsync(string usuarioId, ActualizarPerfilPacienteDto dto)
        {
            if (!ObjectId.TryParse(usuarioId, out _))
            {
                return false;
            }

            // 1. Obtener la cuenta del usuario recibido
            var usuarioCuenta = await _context.Usuarios
                .Find(u => u.Id == usuarioId)
                .FirstOrDefaultAsync();

            if (usuarioCuenta == null) return false;

            Paciente? paciente = null;
            var esPaciente = string.Equals(usuarioCuenta.Rol, "Paciente", StringComparison.OrdinalIgnoreCase);

            // 2. Resolver cuál es el Paciente a modificar
            if (esPaciente)
            {
                paciente = await _context.Pacientes
                    .Find(p => p.UsuarioId == usuarioId)
                    .FirstOrDefaultAsync();
            }
            else
            {
                // Si es Administrador/Cuidador, buscamos el Paciente por la IdLicencia
                if (!string.IsNullOrEmpty(usuarioCuenta.IdLicencia))
                {
                    paciente = await _context.Pacientes
                        .Find(p => p.IdLicencia == usuarioCuenta.IdLicencia)
                        .FirstOrDefaultAsync();
                }

                // Respaldo por si no tiene IdLicencia asignada en el documento de Usuario
                if (paciente == null)
                {
                    paciente = await _context.Pacientes.Find(_ => true).FirstOrDefaultAsync();
                }
            }

            if (paciente == null)
            {
                return false;
            }

            // 3. Construimos la lista de updates condicionales
            var updates = new List<UpdateDefinition<Paciente>>();

            if (!string.IsNullOrWhiteSpace(dto.Curp))
                updates.Add(Builders<Paciente>.Update.Set(p => p.CURP, dto.Curp.Trim().ToUpperInvariant()));

            if (dto.Edad.HasValue)
                updates.Add(Builders<Paciente>.Update.Set(p => p.Edad, dto.Edad.Value));

            if (!string.IsNullOrWhiteSpace(dto.Sexo))
                updates.Add(Builders<Paciente>.Update.Set(p => p.Sexo, dto.Sexo.Trim()));

            if (dto.Peso.HasValue)
                updates.Add(Builders<Paciente>.Update.Set(p => p.Peso, dto.Peso.Value));

            if (dto.Estatura.HasValue)
                updates.Add(Builders<Paciente>.Update.Set(p => p.Estatura, dto.Estatura.Value));

            if (dto.FechaNacimiento.HasValue)
                updates.Add(Builders<Paciente>.Update.Set(p => p.FechaNacimiento, dto.FechaNacimiento.Value));

            if (dto.Direccion != null)
                updates.Add(Builders<Paciente>.Update.Set(p => p.Direccion, dto.Direccion));

            if (!string.IsNullOrWhiteSpace(dto.TipoSangre))
                updates.Add(Builders<Paciente>.Update.Set(p => p.TipoSangre, dto.TipoSangre.Trim().ToUpperInvariant()));

            if (dto.Fotografia != null)
            {
                byte[]? fotoBytes = !string.IsNullOrEmpty(dto.Fotografia)
                    ? Convert.FromBase64String(dto.Fotografia)
                    : null;

                updates.Add(Builders<Paciente>.Update.Set(p => p.Fotografia, fotoBytes));
            }

            // Si no enviaron ningún campo para modificar
            if (updates.Count == 0)
            {
                return true;
            }

            // 4. Ejecutar actualización sobre el paciente encontrado
            var updateCombined = Builders<Paciente>.Update.Combine(updates);

            var result = await _context.Pacientes.UpdateOneAsync(
                p => p.Id == paciente.Id,
                updateCombined
            );

            return result.ModifiedCount > 0 || result.MatchedCount > 0;
        }

        #endregion


    }
}
