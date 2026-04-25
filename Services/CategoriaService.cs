using maverickApi.Data;
using maverickApi.Interfaces;
using maverickApi.Models;
using Microsoft.EntityFrameworkCore;

namespace maverickApi.Services
{
    public class CategoriaService : ICategoriaService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CategoriaService> _logger;
        public CategoriaService(ApplicationDbContext DbContext, IConfiguration Configuration, ILogger<CategoriaService> logger)
        {
            _dbContext = DbContext;
            _configuration = Configuration;
            _logger = logger;
        }

        public async Task<RespuestaApi<Categoria>> CrearCategoriaAsync(Categoria categoria)
        {
            using var tx = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                if (string.IsNullOrWhiteSpace(categoria.Nombre) || string.IsNullOrWhiteSpace(categoria.Descripcion))
                {
                    await tx.RollbackAsync();
                    _logger.LogWarning("No debe de haber campos vacios en la categoria.");
                    return new RespuestaApi<Categoria>
                    {
                        Exito = false,
                        Mensaje = "Toda categoria debe tener un nombre y una Descripcion",
                        Datos = null
                    };
                }
                var categoriaExiste = await _dbContext.Categorias.AnyAsync(c => c.Nombre.ToLower() == categoria.Nombre.ToLower());
                if (categoriaExiste)
                {
                    await tx.RollbackAsync();
                    _logger.LogWarning("El nombre: {Nombre} de categoria ya existe.", categoria.Nombre);
                    return new RespuestaApi<Categoria>
                    {
                        Exito = false,
                        Mensaje = "Ya existe una categoria con ese nombre.",
                        Datos = null
                    };
                }

                _dbContext.Categorias.Add(categoria);
                await _dbContext.SaveChangesAsync();
                await tx.CommitAsync();
                _logger.LogInformation("La categoria con nombre: {Nombre} se creo correctamente.", categoria.Nombre);

                return new RespuestaApi<Categoria>
                {
                    Exito = true,
                    Mensaje = "La categoria se agrego correctamente.",
                    Datos = categoria
                };
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Error al crear la categoria.");
                return new RespuestaApi<Categoria>
                {
                    Exito = false,
                    Mensaje = "Ocurrió un error interno al conectar con la base de datos. Intente más tarde",
                    Datos = null
                };
            }
        }
        public async Task<RespuestaApi<List<Categoria>>> ObtenerCategoriasAsync()
        {
            try
            {
                var categorias = await _dbContext.Categorias.ToListAsync();
                if (categorias == null || categorias.Count() == 0)
                {
                    _logger.LogWarning("No se encontraron categorias en la base de datos.");
                    return new RespuestaApi<List<Categoria>>
                    {

                        Exito = true,
                        Mensaje = "No hay categorias disponibles.",
                        Datos = null
                    };
                }
                _logger.LogInformation("Se encontraron {Count} cantidad de categorias.", categorias.Count());
                return new RespuestaApi<List<Categoria>>
                {
                    Exito = true,
                    Mensaje = "Se obtuvieron las categorias correctamente.",
                    Datos = categorias
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener categorias.");
                return new RespuestaApi<List<Categoria>>
                {
                    Exito = false,
                    Mensaje = "Hubo un error, intentelo de nuevo mas tarde.",
                    Datos = null
                };
            }
        }
        public async Task<RespuestaApi<List<Categoria>>> ObtenerCategoriasPorFiltroAsync(string busqueda)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(busqueda))
                {
                    _logger.LogWarning("El termino de busqueda ingreso vacio.");
                    return new RespuestaApi<List<Categoria>>
                    {
                        Exito = false,
                        Mensaje = "La busqueda no puede estar vacia.",
                        Datos = null
                    };
                }
                busqueda = busqueda.Trim().ToLower();

                var categorias = await _dbContext.Categorias
                .Where(c =>
                EF.Functions.Like(c.Descripcion.ToLower(), $"%{busqueda}%") ||
                EF.Functions.Like(c.Nombre.ToLower(), $"%{busqueda}%")
                ).ToListAsync();

                if (categorias.Count == 0 || categorias == null)
                {
                    _logger.LogWarning("No se encontraron categorias para el termino de busqueda: {busqueda}", busqueda);
                    return new RespuestaApi<List<Categoria>>
                    {
                        Exito = true,
                        Mensaje = "Ne se encontraron categorias con el termino de busqueda proporcionado.",
                        Datos = null
                    };
                }
                _logger.LogInformation("Se obtuvieron las caategorias correctamente.");
                return new RespuestaApi<List<Categoria>>
                {
                    Exito = true,
                    Mensaje = "Se obtuvieron las categorias con exito.",
                    Datos = categorias
                };

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener las categorias con el termino de busqueda: {busqueda}", busqueda);
                return new RespuestaApi<List<Categoria>>
                {
                    Exito = false,
                    Mensaje = "Hubo un error, intentalo de nuevo mas tarde.",
                    Datos = null
                };
            }
        }
    }
}