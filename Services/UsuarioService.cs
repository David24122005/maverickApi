using Microsoft.EntityFrameworkCore;
using maverickApi.Data;
using maverickApi.Interfaces;
using maverickApi.Models;
using maverickApi.Dtos;
using System.Formats.Asn1;
using System.Net.Http.Headers;

namespace maverickApi.Services
{
    public class UsuarioService : IUsuarioService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _htttContextAccessor;
        private readonly ILogger<UsuarioService> _logger;
        public UsuarioService(ApplicationDbContext DbContext, IConfiguration Configuration, IHttpContextAccessor httpContextAccessor, ILogger<UsuarioService> logger)
        {
            _dbContext = DbContext;
            _configuration = Configuration;
            _htttContextAccessor = httpContextAccessor;
            _logger = logger;
        }
        public async Task<RespuestaApi<Usuario>> CrearUsuarioAsync(Usuario usuario)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(usuario.Nombre) || string.IsNullOrWhiteSpace(usuario.Apellidos) || string.IsNullOrWhiteSpace(usuario.PasswordHash) || string.IsNullOrWhiteSpace(usuario.Email))
                {
                    _logger.LogWarning("Validacion fallida. Faltan datos obligatorios. Nombre recibido: {Nombre}, apellidos recibidos: {Apellidos}, email recibido: {Email}.", usuario.Nombre, usuario.Apellidos, usuario.Email);
                    return new RespuestaApi<Usuario>
                    {
                        Exito = false,
                        Mensaje = "No debe haber campos vacios",
                        Datos = null
                    };
                }

                var existeCorreo = await _dbContext.Usuarios.AnyAsync(u => u.Email == usuario.Email);

                if (existeCorreo)
                {
                    _logger.LogWarning("Intento de registro duplicado. Ya existe un usuario con el correo electrónico: {Email}.", usuario.Email);
                    return new RespuestaApi<Usuario>
                    {
                        Exito = false,
                        Mensaje = "Este correo electrónico ya se encuentra registrado.",
                        Datos = null
                    };
                }

                usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(usuario.PasswordHash);

                _dbContext.Usuarios.Add(usuario);
                await _dbContext.SaveChangesAsync();

                usuario.BorrarHash();
                _logger.LogInformation("Usuario registrado exitosamente. Id: {Id}, nombre: {Nombre} y correo: {Email}.", usuario.Id, usuario.Nombre, usuario.Email);
                return new RespuestaApi<Usuario>
                {
                    Exito = true,
                    Mensaje = "El usuario se agrego correctamente",
                    Datos = usuario
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excepcion al crear el usuario.");
                return new RespuestaApi<Usuario>
                {
                    Exito = false,
                    Mensaje = "Ocurrio un error al intentar crear el usuario, intentelo de nuevo.",
                    Datos = null
                };
            }
        }
        public async Task<RespuestaApi<List<Usuario>>> ObtenerUsuariosAsync()
        {
            try
            {
                var usuarios = await _dbContext.Usuarios.Where(u => u.Activo == true).ToListAsync();
                // var usuarios = await _dbContext.Usuarios.ToListAsync();
                if (usuarios.Count() == 0 || usuarios == null)
                {
                    _logger.LogWarning("No se encontraron usuarios en la base de datos.");
                    return new RespuestaApi<List<Usuario>>
                    {
                        Exito = false,
                        Mensaje = "No se encontraron usuarios en la base de datos.",
                        Datos = null
                    };
                }
                usuarios.ForEach(u => u.BorrarHash());

                _logger.LogInformation("Se obtuvieron {Count} usuarios de la base de datos.", usuarios.Count());
                return new RespuestaApi<List<Usuario>>
                {
                    Exito = true,
                    Mensaje = "Los usuarios se obtuvieron correctamente",
                    Datos = usuarios
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excepcion al obtener los usuarios");
                return new RespuestaApi<List<Usuario>>
                {
                    Exito = false,
                    Mensaje = "Ocurrió un error interno al conectar con la base de datos. Intente más tarde.",
                    Datos = null
                };
            }
        }
        public async Task<RespuestaApi<List<Usuario>>> ObtenerUsuariosPorFiltrosAsync(string busqueda)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(busqueda))
                {
                    _logger.LogWarning("El termino de busqueda se recibio vacio o nulo.");
                    return new RespuestaApi<List<Usuario>>
                    {
                        Exito = false,
                        Mensaje = "El término de búsqueda no puede estar vacío.",
                        Datos = null
                    };
                }
                busqueda = busqueda.ToLower();
                var usuarios = await _dbContext.Usuarios.Where(u =>
                EF.Functions.Like(u.Nombre.ToLower(), $"%{busqueda}%") ||
                EF.Functions.Like(u.Apellidos.ToLower(), $"%{busqueda}%") ||
                EF.Functions.Like(u.Email.ToLower(), $"%{busqueda}%") ||
                u.Admin.ToString().Contains(busqueda) ||
                (busqueda == "activo" && u.Activo) ||
                (busqueda == "inactivo" && !u.Activo)
                ).ToListAsync();

                usuarios.ForEach(u => u.BorrarHash());

                if (usuarios == null || usuarios.Count() == 0)
                {
                    _logger.LogWarning("La busqueda de usuarios no arrojo resultados para el termino de busqueda: {Busqueda}.", busqueda);
                    return new RespuestaApi<List<Usuario>>
                    {
                        Exito = true,
                        Mensaje = "No se encontraron usuarios con el termino de busqueda proporcionado.",
                        Datos = null
                    };
                }
                _logger.LogInformation("Se encontraron {Count} usuarios para el termino  de busqueda: {busqueda}.", usuarios.Count(), busqueda);
                return new RespuestaApi<List<Usuario>>
                {
                    Exito = true,
                    Mensaje = "Los usuarios se obtuvieron correctamente",
                    Datos = usuarios
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excepcion al buscar usuarios con el termino de busqueda: {Busqueda}.", busqueda);
                return new RespuestaApi<List<Usuario>>
                {
                    Exito = false,
                    Mensaje = "Ocurrió un error interno al conectar con la base de datos. Intente más tarde.",
                    Datos = null
                };
            }
        }
        public async Task<RespuestaApi<Usuario>> EditarUsuarioAsync(EditarUsuarioDto editarUsuarioDto)
        {
            var tx = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var usuarioExistente = await _dbContext.Usuarios.FindAsync(editarUsuarioDto.Id);
                if (usuarioExistente == null)
                {
                    await tx.RollbackAsync();
                    _logger.LogWarning("No se encontro el usuario con id: {Id}.", editarUsuarioDto.Id);
                    return new RespuestaApi<Usuario>
                    {
                        Exito = false,
                        Mensaje = "El usuario que intentas editar no existe en la base de datos",
                        Datos = null
                    };
                }
                var existeCorreo = await _dbContext.Usuarios.AnyAsync(u => u.Email == editarUsuarioDto.Email && u.Id != editarUsuarioDto.Id);
                if (existeCorreo)
                {
                    await tx.RollbackAsync();
                    _logger.LogWarning("Intento de registro duplicado. El correo: {Email} ya se encuentra registrado en otro usuario.", editarUsuarioDto.Email);
                    return new RespuestaApi<Usuario>
                    {
                        Exito = false,
                        Mensaje = "Este correo electrónico ya se encuentra registrado por otro usuario.",
                        Datos = null
                    };
                }

                if (!string.IsNullOrWhiteSpace(editarUsuarioDto.Nombre))
                {
                    usuarioExistente.Nombre = editarUsuarioDto.Nombre;
                }
                if (!string.IsNullOrWhiteSpace(editarUsuarioDto.Apellidos))
                {
                    usuarioExistente.Apellidos = editarUsuarioDto.Apellidos;
                }
                if (!string.IsNullOrWhiteSpace(editarUsuarioDto.Email))
                {
                    usuarioExistente.Email = editarUsuarioDto.Email;
                }
                if (editarUsuarioDto.Admin != usuarioExistente.Admin)
                {
                    usuarioExistente.Admin = editarUsuarioDto.Admin;
                }
                var usuario = usuarioExistente;

                await _dbContext.SaveChangesAsync();
                await tx.CommitAsync();

                usuario.BorrarHash();
                _logger.LogInformation("Se actualizo correctamente el usuario con id: {Id} nombre: {Nombre} y correo: {Email}.", usuario.Id, usuario.Nombre, usuario.Email);
                return new RespuestaApi<Usuario>
                {
                    Exito = true,
                    Mensaje = "Usuario actualizado correctamente",
                    Datos = usuario
                };
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Excepcion al actualizada el usuario con id: {Id}.", editarUsuarioDto.Id);
                return new RespuestaApi<Usuario>
                {
                    Exito = false,
                    Mensaje = " Hubo un error con el servidor, intentelo de nuevo mas tarde.",
                    Datos = null
                };
            }
        }
        public async Task<RespuestaApi<Usuario>> CambiarPasswordAsync(CambiarPasswordDto cambiarPasswordDto)
        {
            try
            {
                var usuarioExistente = await _dbContext.Usuarios.FirstOrDefaultAsync(u => u.Id == cambiarPasswordDto.UsuarioId);
                if (usuarioExistente == null)
                {
                    _logger.LogWarning("No se encontro el usuario con id: {Id}.", cambiarPasswordDto.UsuarioId);
                    return new RespuestaApi<Usuario>
                    {
                        Exito = false,
                        Mensaje = "El usuario que intentas editar no existe en la base de datos",
                        Datos = null
                    };
                }

                bool contraseñaValida = BCrypt.Net.BCrypt.Verify(cambiarPasswordDto.PasswordActual, usuarioExistente.PasswordHash);

                if (!contraseñaValida)
                {
                    _logger.LogWarning("La contraseña ingresada es incorrecta.");
                    return new RespuestaApi<Usuario>
                    {
                        Exito = false,
                        Mensaje = "La contraseña que ingresaste es incorrecta.",
                        Datos = null
                    };
                }

                usuarioExistente.PasswordHash = BCrypt.Net.BCrypt.HashPassword(cambiarPasswordDto.NuevaPassword);

                await _dbContext.SaveChangesAsync();
                var usuarioMostrar = usuarioExistente;
                usuarioMostrar.BorrarHash();
                _logger.LogInformation("Se actualizo exitosamente la contraseña del usuario: {Nombre}.", usuarioExistente.Nombre);
                return new RespuestaApi<Usuario>
                {
                    Exito = true,
                    Mensaje = "Contraseña actualizada correctamente",
                    Datos = usuarioMostrar
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excepcion al actualizar la contraseña.");
                return new RespuestaApi<Usuario>
                {
                    Exito = false,
                    Mensaje = "Ocurrio un error al actualizar la contraseña de el usuario.",
                    Datos = null
                };
            }
        }
        public async Task<RespuestaApi<Usuario>> EditarEstadoAsync(EditarEstadoDto editarEstadoDto)
        {
            try
            {
                var usuarioExistente = await _dbContext.Usuarios.FindAsync(editarEstadoDto.Id);
                if (usuarioExistente == null)
                {
                    _logger.LogWarning("No se encontro el usuario con id: {Id}.", editarEstadoDto.Id);
                    return new RespuestaApi<Usuario>
                    {
                        Exito = false,
                        Mensaje = "No se encontro el usuarios, intentelo de nuevo.",
                        Datos = null
                    };
                }

                usuarioExistente.Activo = editarEstadoDto.NuevoEstado;
                await _dbContext.SaveChangesAsync();
                var usuarioMostrar = usuarioExistente;
                usuarioMostrar.BorrarHash();
                _logger.LogInformation("Se actualizo el estado de el usuario: {Nombre} exitosamente.", usuarioExistente.Nombre);
                return new RespuestaApi<Usuario>
                {
                    Exito = true,
                    Mensaje = "Se actualizo el usuario correctamente",
                    Datos = usuarioMostrar
                };

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excepcion al actualizar el estado de el usuario.");
                return new RespuestaApi<Usuario>
                {
                    Exito = false,
                    Mensaje = "El usuario no se actualizo correctamente.",
                    Datos = null
                };
            }
        }
    }
}