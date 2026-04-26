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
        private readonly ILogger<ProveedorService> _logger;

        public ProveedorService(ApplicationDbContext DbContext, IConfiguration Configuration, ILogger<ProveedorService> logger)
        {
            _dbContext = DbContext;
            _configuration = Configuration;
            _logger = logger;
        }

        public async Task<RespuestaApi<Proveedor>> CrearProveedorAsync(Proveedor proveedor)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(proveedor.Direccion) || string.IsNullOrWhiteSpace(proveedor.Email) || string.IsNullOrWhiteSpace(proveedor.Nombre) || string.IsNullOrWhiteSpace(proveedor.Telefono) || string.IsNullOrWhiteSpace(proveedor.Rfc))
                {
                    _logger.LogWarning("Validacion fallida. Faltan datos obligatorios. Direccion recibida: {Direccion}, Email recibido: {Email}, nombre recibido: {Nombre}, telefono recibido: {Telefono}, rfc recibido: {Rfc}.", proveedor.Direccion ?? "N/A", proveedor.Email ?? "N/A", proveedor.Nombre ?? "N/A", proveedor.Telefono ?? "N/A", proveedor.Rfc ?? "N/A");
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
                    _logger.LogWarning("Intento de registro duplicado. Ya existe un proveedor con el rfc: {Rfc}.", proveedor.Rfc);
                    return new RespuestaApi<Proveedor>
                    {
                        Exito = false,
                        Mensaje = "Ya existe un proveedor con ese RFC, compruebelo y intentelo de nuevo",
                        Datos = null
                    };
                }

                _dbContext.Proveedores.Add(proveedor);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Proveedor registrado exitosamente. Id: {Id}, nombre: {Nombre} y rfc: {Rfc}.", proveedor.Id, proveedor.Nombre, proveedor.Rfc);
                return new RespuestaApi<Proveedor>
                {
                    Exito = true,
                    Mensaje = "El proveedor se creo correctamente",
                    Datos = proveedor
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excepcion al crear el proveedor con rfc: {Rfc}.", proveedor.Rfc ?? "N/A");
                return new RespuestaApi<Proveedor>
                {
                    Exito = false,
                    Mensaje = "Ocurrió un error interno al intentar crear el proveedor. Intente más tarde.",
                    Datos = null
                };
            }
        }
        public async Task<RespuestaApi<List<Proveedor>>> ObtenerProveedoresAsync()
        {
            try
            {
                var proveedores = await _dbContext.Proveedores
                .Include(p => p.Productos)
                .ToListAsync();
                if (proveedores == null || proveedores.Count == 0)
                {
                    _logger.LogWarning("No se encontraron proveedores en la base de datos.");
                    return new RespuestaApi<List<Proveedor>>
                    {
                        Exito = false,
                        Mensaje = "No se encontraron proveedores",
                        Datos = null
                    };
                }
                _logger.LogInformation("Se obtuvieron {Count} proveedores de la base de datos.", proveedores.Count());
                return new RespuestaApi<List<Proveedor>>
                {
                    Exito = true,
                    Mensaje = "Se obtuvieron los proveedores con exito",
                    Datos = proveedores
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excepcion al obtener los proveedores.");
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
                    _logger.LogWarning("El termino de busqueda se recibio vacio o nulo.");
                    return new RespuestaApi<List<Proveedor>>
                    {
                        Exito = false,
                        Mensaje = "El término de búsqueda no puede estar vacío.",
                        Datos = null
                    };
                }
                busqueda = busqueda.ToLower();
                var proveedores = await _dbContext.Proveedores
                .Include(p => p.Productos)
                .Where(p =>
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
                    _logger.LogWarning("La busqueda de proveedores no arrojo resultados para el termino de busqueda: {Busqueda}.", busqueda);
                    return new RespuestaApi<List<Proveedor>>
                    {
                        Exito = true,
                        Mensaje = "No se encontraron proveedores con el término de búsqueda proporcionado.",
                        Datos = null
                    };
                }
                _logger.LogInformation("Se encontraron {Count} proveedores para el termino de busqueda: {Busqueda}.", proveedores.Count(), busqueda);
                return new RespuestaApi<List<Proveedor>>
                {
                    Exito = true,
                    Mensaje = "Se obtuvieron los proveedores con exito",
                    Datos = proveedores
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excepcion al buscar proveedores con el termino de busqueda: {Busqueda}.", busqueda);
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
            try
            {
                var proveedorExistente = await _dbContext.Proveedores.FindAsync(proveedorDto.Id);
                if (proveedorExistente == null)
                {
                    _logger.LogWarning("El proveedor con id: {Id}, nombre: {Nombre} y rfc: {Rfc} no existe o se encuentra inactivo.", proveedorDto.Id, proveedorDto.Nombre, proveedorDto.Rfc);
                    return new RespuestaApi<Proveedor>
                    {
                        Exito = false,
                        Mensaje = "El proveedor que intentas editar no existe en la base de datos",
                        Datos = null
                    };
                }
                if (!string.IsNullOrWhiteSpace(proveedorDto.Rfc))
                {
                    var existeRfc = await _dbContext.Proveedores.AnyAsync(p => p.Rfc == proveedorDto.Rfc && p.Id != proveedorDto.Id);
                    if (existeRfc)
                    {
                        _logger.LogWarning("El rfc: {Rfc} ya se encuentra registrado en otro proveedor.", proveedorDto.Rfc);
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

                _logger.LogInformation("Se actualizo correctamente el proveedor con id: {Id} y nombre: {Nombre}.", proveedorExistente.Id, proveedorExistente.Nombre);
                return new RespuestaApi<Proveedor>
                {
                    Exito = true,
                    Mensaje = "El proveedor se actualizo con exito.",
                    Datos = proveedorExistente
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excepcion al editar el proveedor con id: {Id}.", proveedorDto.Id);
                return new RespuestaApi<Proveedor>
                {
                    Exito = false,
                    Mensaje = "Ocurrio un error al editar el proveedor.",
                    Datos = null
                };

            }
        }
        public async Task<RespuestaApi<Proveedor>> EditarEstadoAsync(EditarEstadoDto editarEstadoDto)
        {
            try
            {
                var proveedorExistente = await _dbContext.Proveedores.FindAsync(editarEstadoDto.Id);
                if (proveedorExistente == null)
                {
                    _logger.LogWarning("No se encontro el proveedor con id: {Id}.", editarEstadoDto.Id);
                    return new RespuestaApi<Proveedor>
                    {
                        Exito = false,
                        Mensaje = "No se encontro el proveedor, intentelo de nuevo."
                    };
                }
                proveedorExistente.Activo = editarEstadoDto.NuevoEstado;
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Se actualizo correctamente el estado de el proveedor: {Nombre} com id: {Id} a estado: {Activo}.", proveedorExistente.Nombre, proveedorExistente.Id, proveedorExistente.Activo
                );
                return new RespuestaApi<Proveedor>
                {
                    Exito = true,
                    Mensaje = "Se actualizo el proveedor correctamente.",
                    Datos = proveedorExistente
                };

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excepcion al actualizar el proveedor.");
                return new RespuestaApi<Proveedor>
                {
                    Exito = false,
                    Mensaje = "No se pudo actualizar el proveedor, intentelo de nuevo."
                };
            }
        }
    }
}