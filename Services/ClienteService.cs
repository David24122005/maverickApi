using maverickApi.Interfaces;
using maverickApi.Data;
using maverickApi.Models;
using Microsoft.EntityFrameworkCore;

namespace maverickApi.Services
{
    public class ClienteService : IClienteService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ClienteService> _logger;
        public ClienteService(ApplicationDbContext DbContext, IConfiguration Configuration, ILogger<ClienteService> logger)

        {
            _dbContext = DbContext;
            _configuration = Configuration;
            _logger = logger;
        }

        public async Task<RespuestaApi<Cliente>> CrearClienteAsync(Cliente cliente)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(cliente.Nombre) || string.IsNullOrWhiteSpace(cliente.Rfc) || string.IsNullOrWhiteSpace(cliente.Telefono) || string.IsNullOrWhiteSpace(cliente.Email))
                {
                    _logger.LogWarning("Validación fallida al crear cliente. Faltan campos obligatorios, Rfc recibido: {Rfc}, Nombre recibido: {Nombre}, Telefono recibido: {Telefono} y Email recibido: {Email}.", cliente.Rfc ?? "N/A", cliente.Nombre ?? "N/A", cliente.Telefono ?? "N/A", cliente.Email ?? "N/A");
                    return new RespuestaApi<Cliente>
                    {
                        Exito = false,
                        Mensaje = "No debe haber campos vacios.",
                        Datos = null
                    };
                }
                var ClienteExiste = await _dbContext.Clientes.AnyAsync(c => c.Rfc == cliente.Rfc);
                if (ClienteExiste)
                {
                    _logger.LogWarning("Intento de registro duplicado. Ya existe un cliente con el Rfc: {Rfc}.", cliente.Rfc);
                    return new RespuestaApi<Cliente>
                    {
                        Exito = false,
                        Mensaje = "El RFC se encuentra registrado con otro cliente, intentelo de nuevo.",
                        Datos = null
                    };
                }

                _dbContext.Clientes.Add(cliente);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Cliente registrado exitosamente. Id: {Id} ,Rfc: {Rfc}", cliente.Id, cliente.Rfc);
                return new RespuestaApi<Cliente>
                {
                    Exito = true,
                    Mensaje = "El cliente se registro correctamente",
                    Datos = cliente
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excepcion al intentar crear al cliente con Rfc: {Rfc}.", cliente.Rfc);
                return new RespuestaApi<Cliente>
                {
                    Exito = false,
                    Mensaje = "Ocurrió un error interno al conectar con la base de datos. Intente más tarde"
                };
            }

        }
        public async Task<RespuestaApi<List<Cliente>>> ObtenerClientesAsync()
        {
            try
            {
                var clientes = await _dbContext.Clientes.ToListAsync();
                if (clientes.Count() == 0)
                {
                    _logger.LogWarning("No se encontraron clientes en la base de datos.");
                    return new RespuestaApi<List<Cliente>>
                    {
                        Exito = false,
                        Mensaje = "No se encontraron clientes",
                        Datos = null
                    };
                }
                _logger.LogInformation("Se obtuvieron {Count} clientes de la base de datos.", clientes.Count());
                return new RespuestaApi<List<Cliente>>
                {
                    Exito = true,
                    Mensaje = "Clientes obtenidos correctamente",
                    Datos = clientes
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excepcion al obtener los clientes.");
                return new RespuestaApi<List<Cliente>>
                {
                    Exito = false,
                    Mensaje = "Ocurrió un error interno al conectar con la base de datos. Intente más tarde",
                    Datos = null
                };
            }
        }
        public async Task<RespuestaApi<List<Cliente>>> ObtenerClientesPorFiltrosAsync(string busqueda)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(busqueda))
                {
                    _logger.LogWarning("El termino de busqueda de clientes se recibio vacio o nulo.");
                    return new RespuestaApi<List<Cliente>>
                    {
                        Exito = false,
                        Mensaje = "El término de búsqueda no puede estar vacío.",
                        Datos = null
                    };
                }
                busqueda = busqueda.ToLower();
                var clientes = await _dbContext.Clientes.Where(c =>
                EF.Functions.Like(c.Nombre.ToLower(), $"%{busqueda}%") ||
                EF.Functions.Like(c.Rfc.ToLower(), $"%{busqueda}%") ||
                EF.Functions.Like(c.Telefono.ToLower(), $"%{busqueda}%") ||
                EF.Functions.Like(c.Email.ToLower(), $"%{busqueda}%") ||
                (busqueda == "activo" && c.Activo) ||
                (busqueda == "inactivo" && !c.Activo)
                ).ToListAsync();
                if (clientes == null || clientes.Count() == 0)
                {
                    _logger.LogInformation("La busqueda de clientes no arrojo resultados para el termino de busqueda: {Busqueda}.", busqueda);
                    return new RespuestaApi<List<Cliente>>
                    {
                        Exito = true,
                        Mensaje = "No se encontraron clientes con el término de búsqueda proporcionado.",
                        Datos = null
                    };
                }
                _logger.LogInformation("Se encontraron {Count} clientes para el termino de busqueda: {Busqueda}.", clientes.Count(), busqueda);
                return new RespuestaApi<List<Cliente>>
                {
                    Exito = true,
                    Mensaje = "Se obtuvieron los clientes con exito.",
                    Datos = clientes
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excepcion al buscar clientes con el termino: {Busqueda},", busqueda);
                return new RespuestaApi<List<Cliente>>
                {
                    Exito = false,
                    Mensaje = "Ocurrió un error interno al conectar con la base de datos. Intente más tarde.",
                    Datos = null
                };
            }
        }
    }
}