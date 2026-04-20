using maverickApi.Interfaces;
using maverickApi.Data;
using maverickApi.Models;
using Microsoft.EntityFrameworkCore;

namespace maverick.ClienteService
{
    public class ClienteService : IClienteService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IConfiguration _configuration;
        public ClienteService(ApplicationDbContext DbContext, IConfiguration Configuration)
        {
            _dbContext = DbContext;
            _configuration = Configuration;
        }

        public async Task<RespuestaApi<Cliente>> CrearClienteAsync(Cliente cliente)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(cliente.Nombre) || string.IsNullOrWhiteSpace(cliente.Rfc) || string.IsNullOrWhiteSpace(cliente.Telefono) || string.IsNullOrWhiteSpace(cliente.Email))
                {
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
                    return new RespuestaApi<Cliente>
                    {
                        Exito = false,
                        Mensaje = "El RFC se encuentra registrado con otro cliente, intentelo de nuevo.",
                        Datos = null
                    };
                }

                _dbContext.Clientes.Add(cliente);
                await _dbContext.SaveChangesAsync();

                return new RespuestaApi<Cliente>
                {
                    Exito = true,
                    Mensaje = "El cliente se registro correctamente",
                    Datos = cliente
                };
            }
            catch
            {
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
                if (clientes.Count == 0)
                {
                    return new RespuestaApi<List<Cliente>>
                    {
                        Exito = false,
                        Mensaje = "No se encontraron clientes",
                        Datos = null
                    };
                }
                return new RespuestaApi<List<Cliente>>
                {
                    Exito = true,
                    Mensaje = "Clientes obtenidos correctamente",
                    Datos = clientes
                };
            }
            catch
            {
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
                    return new RespuestaApi<List<Cliente>>
                    {
                        Exito = true,
                        Mensaje = "No se encontraron clientes con el término de búsqueda proporcionado.",
                        Datos = null
                    };
                }
                return new RespuestaApi<List<Cliente>>
                {
                    Exito = true,
                    Mensaje = "Se obtuvieron los clientes con exito.",
                    Datos = clientes
                };
            }
            catch
            {
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