using maverickApi.Interfaces;
using Microsoft.EntityFrameworkCore;
using maverickApi.Data;
using maverickApi.Models;
using System.Security.Cryptography.X509Certificates;
using System.Reflection;
using maverickApi.Dtos;

namespace maverickApi.Services
{
    public class ProveedorService : IProveedorService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IConfiguration _configuration;

        public ProveedorService(ApplicationDbContext DbContext, IConfiguration Configuration)
        {
            _dbContext = DbContext;
            _configuration = Configuration;
        }

        public async Task<RespuestaApi<Proveedor>> CrearProveedorAsync(Proveedor proveedor)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(proveedor.Direccion) || string.IsNullOrWhiteSpace(proveedor.Email) || string.IsNullOrWhiteSpace(proveedor.Nombre) || string.IsNullOrWhiteSpace(proveedor.Telefono) || string.IsNullOrWhiteSpace(proveedor.Rfc))
                {
                    return new RespuestaApi<Proveedor>
                    {
                        Exito = false,
                        Mensaje = "No debe haber campos vacios",
                        Datos = null
                    };
                }
                var proveedorExiste = await _dbContext.Proveedores.AnyAsync(u => u.Rfc == proveedor.Rfc);
                if (proveedorExiste)
                {
                    return new RespuestaApi<Proveedor>
                    {
                        Exito = false,
                        Mensaje = "Ya existe un proveedor con ese RFC, compruebelo y intentelo de nuevo",
                        Datos = null
                    };
                }

                _dbContext.Proveedores.Add(proveedor);
                await _dbContext.SaveChangesAsync();
                return new RespuestaApi<Proveedor>
                {
                    Exito = true,
                    Mensaje = "El proveedor se creo correctamente",
                    Datos = proveedor
                };
            }
            catch
            {
                return new RespuestaApi<Proveedor>
                {
                    Exito = false,
                    Mensaje = "Ocurrió un error interno al conectar con la base de datos. Intente más tarde.",
                    Datos = null
                };
            }
        }
        public async Task<RespuestaApi<List<Proveedor>>> ObtenerProveedoresAsync()
        {
            try
            {
                var proveedores = await _dbContext.Proveedores.ToListAsync();
                if (proveedores == null || proveedores.Count == 0)
                {
                    return new RespuestaApi<List<Proveedor>>
                    {
                        Exito = false,
                        Mensaje = "No se encontraron proveedores",
                        Datos = null
                    };
                }
                return new RespuestaApi<List<Proveedor>>
                {
                    Exito = true,
                    Mensaje = "Se obtuvieron los proveedores con exito",
                    Datos = proveedores
                };
            }
            catch
            {
                return new RespuestaApi<List<Proveedor>>
                {
                    Exito = false,
                    Mensaje = "Ocurrió un error interno al conectar con la base de datos. Intente más tarde.",
                    Datos = null
                };
            }
        }
        public async Task<RespuestaApi<List<Proveedor>>> ObtenerProveedoresPorFiltrosAsync(string busqueda)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(busqueda))
                {
                    return new RespuestaApi<List<Proveedor>>
                    {
                        Exito = false,
                        Mensaje = "El término de búsqueda no puede estar vacío.",
                        Datos = null
                    };
                }
                busqueda = busqueda.ToLower();
                var proveedores = await _dbContext.Proveedores.Where(p =>
                EF.Functions.Like(p.Nombre.ToLower(), $"%{busqueda}%") ||
                EF.Functions.Like(p.Rfc.ToLower(), $"%{busqueda}%") ||
                EF.Functions.Like(p.Email.ToLower(), $"%{busqueda}%") ||
                EF.Functions.Like(p.Telefono.ToLower(), $"%{busqueda}%") ||
                EF.Functions.Like(p.Direccion.ToLower(), $"%{busqueda}%") ||
                (busqueda == "activo" && p.Activo) ||
                (busqueda == "inactivo" && !p.Activo)
                ).ToListAsync();

                if (proveedores == null || proveedores.Count == 0)
                {
                    return new RespuestaApi<List<Proveedor>>
                    {
                        Exito = true,
                        Mensaje = "No se encontraron proveedores con el término de búsqueda proporcionado.",
                        Datos = null
                    };
                }
                return new RespuestaApi<List<Proveedor>>
                {
                    Exito = true,
                    Mensaje = "Se obtuvieron los proveedores con exito",
                    Datos = proveedores
                };
            }
            catch
            {
                return new RespuestaApi<List<Proveedor>>
                {
                    Exito = false,
                    Mensaje = "Ocurrió un error interno al conectar con la base de datos. Intente más tarde.",
                    Datos = null
                };
            }
        }
        public async Task<RespuestaApi<Proveedor>> EditarProveedorAsync(EditarProveedorDto proveedorDto)
        {
            var proveedorExistente = await _dbContext.Proveedores.FindAsync(proveedorDto.Id);
            if (proveedorExistente == null)
            {
                return new RespuestaApi<Proveedor>
                {
                    Exito = false,
                    Mensaje = "El usuario que intentas editar no existe en la base de datos",
                    Datos = null
                };
            }
            if (!string.IsNullOrWhiteSpace(proveedorDto.Rfc))
            {
                var existeRfc = await _dbContext.Proveedores.AnyAsync(p => p.Rfc == proveedorDto.Rfc && p.Id != proveedorDto.Id);
                if (existeRfc)
                {
                    return new RespuestaApi<Proveedor>
                    {
                        Exito = false,
                        Mensaje = "Ya existe un proveedor con el mismo RFC, revisa que todo este bien escrito y reintentalo.",
                        Datos = null
                    };
                }
                proveedorExistente.Rfc = proveedorDto.Rfc;
            }

            if (!string.IsNullOrWhiteSpace(proveedorDto.Nombre))
            {
                proveedorExistente.Nombre = proveedorDto.Nombre;
            }

            if (!string.IsNullOrWhiteSpace(proveedorDto.Email))
            {
                proveedorExistente.Email = proveedorDto.Email;
            }

            if (!string.IsNullOrWhiteSpace(proveedorDto.Telefono))
            {
                proveedorExistente.Telefono = proveedorDto.Telefono;
            }

            if (!string.IsNullOrWhiteSpace(proveedorDto.Direccion))
            {
                proveedorExistente.Direccion = proveedorDto.Direccion;
            }



            await _dbContext.SaveChangesAsync();

            return new RespuestaApi<Proveedor>
            {
                Exito = true,
                Mensaje = "El proveedor se actualizo con exito.",
                Datos = proveedorExistente
            };
        }
        public async Task<RespuestaApi<Proveedor>> EditarEstadoAsync(EditarEstadoDto editarEstadoDto)
        {
            try
            {
                var proveedorExistente = await _dbContext.Proveedores.FindAsync(editarEstadoDto.Id);
                if (proveedorExistente == null)
                {
                    return new RespuestaApi<Proveedor>
                    {
                        Exito = false,
                        Mensaje = "No se encontro el proveedor, intentelo de nuevo."
                    };
                }
                proveedorExistente.Activo = editarEstadoDto.NuevoEstado;
                await _dbContext.SaveChangesAsync();
                return new RespuestaApi<Proveedor>
                {
                    Exito = true,
                    Mensaje = "Se actualizo el proveedor correctamente.",
                    Datos = proveedorExistente
                };

            }
            catch
            {
                return new RespuestaApi<Proveedor>
                {
                    Exito = false,
                    Mensaje = "No se pudo actualizar el proveedor, intentelo de nuevo."
                };
            }
        }
    }
}