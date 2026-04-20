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
        public UsuarioService(ApplicationDbContext DbContext, IConfiguration Configuration, IHttpContextAccessor httpContextAccessor)
        {
            _dbContext = DbContext;
            _configuration = Configuration;
            _htttContextAccessor = httpContextAccessor;
        }
        public async Task<RespuestaApi<Usuario>> CrearUsuarioAsync(Usuario usuario)
        {
            if (string.IsNullOrWhiteSpace(usuario.Nombre) || string.IsNullOrWhiteSpace(usuario.Apellidos) || string.IsNullOrWhiteSpace(usuario.PasswordHash) || string.IsNullOrWhiteSpace(usuario.Email))
            {
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

            BorrarHash(usuario);

            return new RespuestaApi<Usuario>
            {
                Exito = true,
                Mensaje = "El usuario se agrego correctamente",
                Datos = usuario
            };
        }
        public async Task<RespuestaApi<List<Usuario>>> ObtenerUsuariosAsync()
        {
            try
            {
                //var usuarios = await _dbContext.Usuarios.Where(u => u.Activo == true).ToListAsync();
                var usuarios = await _dbContext.Usuarios.ToListAsync();

                usuarios.ForEach(u => BorrarHash(u));

                return new RespuestaApi<List<Usuario>>
                {
                    Exito = true,
                    Mensaje = "Los usuarios se obtuvieron correctamente",
                    Datos = usuarios
                };
            }
            catch
            {
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

                usuarios.ForEach(u => BorrarHash(u));

                if (usuarios == null || usuarios.Count() == 0)
                {
                    return new RespuestaApi<List<Usuario>>
                    {
                        Exito = true,
                        Mensaje = "No se encontraron usuarios con el termino de busqueda proporcionado.",
                        Datos = null
                    };
                }

                return new RespuestaApi<List<Usuario>>
                {
                    Exito = true,
                    Mensaje = "Los usuarios se obtuvieron correctamente",
                    Datos = usuarios
                };
            }
            catch
            {
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
                if (editarUsuarioDto.Admin != null)
                {
                    usuarioExistente.Admin = editarUsuarioDto.Admin;
                }
                var usuario = usuarioExistente;

                await _dbContext.SaveChangesAsync();
                await tx.CommitAsync();

                BorrarHash(usuario);

                return new RespuestaApi<Usuario>
                {
                    Exito = true,
                    Mensaje = "Usuario actualizado correctamente",
                    Datos = usuarioExistente
                };
            }
            catch
            {
                await tx.RollbackAsync();
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
            var usuarioExistente = await _dbContext.Usuarios.FirstOrDefaultAsync(u => u.Id == cambiarPasswordDto.UsuarioId);
            if (usuarioExistente == null)
            {
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
                return new RespuestaApi<Usuario>
                {
                    Exito = false,
                    Mensaje = "La contraseña que ingresaste es incorrecta.",
                    Datos = null
                };
            }

            usuarioExistente.PasswordHash = BCrypt.Net.BCrypt.HashPassword(cambiarPasswordDto.NuevaPassword);

            await _dbContext.SaveChangesAsync();

            BorrarHash(usuarioExistente);

            return new RespuestaApi<Usuario>
            {
                Exito = true,
                Mensaje = "Contraseña actualizada correctamente",
                Datos = usuarioExistente
            };
        }
        public async Task<RespuestaApi<Usuario>> EditarEstadoAsync(EditarEstadoDto editarEstadoDto)
        {
            try
            {
                var usuarioExistente = await _dbContext.Usuarios.FindAsync(editarEstadoDto.Id);
                if (usuarioExistente == null)
                {
                    return new RespuestaApi<Usuario>
                    {
                        Exito = false,
                        Mensaje = "No se encontro el usuarios, intentelo de nuevo.",
                        Datos = null
                    };
                }

                usuarioExistente.Activo = editarEstadoDto.NuevoEstado;
                await _dbContext.SaveChangesAsync();
                BorrarHash(usuarioExistente);

                return new RespuestaApi<Usuario>
                {
                    Exito = true,
                    Mensaje = "Se actualizo el usuario correctamente",
                    Datos = usuarioExistente
                };

            }
            catch
            {
                return new RespuestaApi<Usuario>
                {
                    Exito = false,
                    Mensaje = "El usuario no se actualizo correctamente.",
                    Datos = null
                };
            }
        }
        private void BorrarHash(Usuario usuario)
        {
            usuario.PasswordHash = "";
        }
    }
}