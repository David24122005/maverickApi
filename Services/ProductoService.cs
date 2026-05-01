using System.Globalization;
using System.Linq.Expressions;
using System.Net.Http.Headers;
using maverickApi.Data;
using maverickApi.Dtos;
using maverickApi.Interfaces;
using maverickApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Any;

namespace maverickApi.Services
{
    public class ProductoService : IProductoService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<ProductoService> _logger;
        public ProductoService(ApplicationDbContext DbContext, IConfiguration Configuration, IHttpContextAccessor httpContextAccessor, ILogger<ProductoService> logger)
        {
            _dbContext = DbContext;
            _configuration = Configuration;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<RespuestaApi<Producto>> CrearProductoAsync(Producto producto)
        {
            using var tx = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                if (string.IsNullOrWhiteSpace(producto.CodigoBarras) || string.IsNullOrWhiteSpace(producto.Nombre) || string.IsNullOrWhiteSpace(producto.Descripcion) || string.IsNullOrWhiteSpace(producto.Sku) || string.IsNullOrWhiteSpace(producto.Marca) || producto.PrecioCompra <= 0 || producto.PrecioVenta <= 0 || string.IsNullOrWhiteSpace(producto.Modelo) || producto.CategoriaId <= 0 || producto.ProveedorId <= 0)
                {
                    await tx.RollbackAsync();
                    _logger.LogWarning("Validación fallida al crear producto: campos obligatorios vacíos o inválidos. Datos recibidos: {@Producto}", producto);
                    return new RespuestaApi<Producto>
                    {
                        Exito = false,
                        Mensaje = "No debe haber campos vacios",
                        Datos = null
                    };
                }
                producto.Sku = producto.Sku.Replace(" ", "");
                var SkuExiste = await _dbContext.Productos.AnyAsync(p => p.Sku == producto.Sku);
                if (SkuExiste)
                {
                    await tx.RollbackAsync();
                    _logger.LogWarning("El sku proporcionado existe en otro producto: {sku}", producto.Sku);
                    return new RespuestaApi<Producto>
                    {
                        Exito = false,
                        Mensaje = "El sku se encuentra registrado, intentelos de nuevo.",
                        Datos = null
                    };
                }
                producto.CodigoBarras = producto.CodigoBarras.Replace(" ", "");
                var codigoBarrasExiste = await _dbContext.Productos
                    .AnyAsync(p => p.CodigoBarras == producto.CodigoBarras);
                if (codigoBarrasExiste)
                {
                    await tx.RollbackAsync();
                    _logger.LogWarning("El codigo de barras existe en otro producto: {codigoBarras}", producto.CodigoBarras);
                    return new RespuestaApi<Producto>
                    {
                        Exito = false,
                        Mensaje = "El codigo de barras se encuentra registrado en otro producto.",
                        Datos = null
                    };
                }
                var proveedor = await _dbContext.Proveedores.FirstOrDefaultAsync(p => p.Id == producto.ProveedorId);
                if (proveedor == null)
                {
                    await tx.RollbackAsync();
                    _logger.LogWarning("El proveedor con id: {id} no existe", producto.ProveedorId);
                    return new RespuestaApi<Producto>
                    {
                        Exito = false,
                        Mensaje = "El proveedor no existe, intentelos de nuevo.",
                        Datos = null
                    };
                }
                producto.Proveedor = proveedor;
                var categoria = await _dbContext.Categorias.FirstOrDefaultAsync(c => c.Id == producto.CategoriaId);
                if (categoria == null)
                {
                    await tx.RollbackAsync();
                    _logger.LogWarning("La categoria con id: {id} no existe.", producto.CategoriaId);
                    return new RespuestaApi<Producto>
                    {
                        Exito = false,
                        Mensaje = "La categoria no existe.",
                        Datos = null
                    };
                }
                producto.Categoria = categoria;

                _dbContext.Productos.Add(producto);
                await _dbContext.SaveChangesAsync();
                await tx.CommitAsync();


                _logger.LogInformation("El producto: {Nombre} con el Id: {Id} fue creado exitosamente.", producto.Nombre, producto.Id);
                return new RespuestaApi<Producto>
                {
                    Exito = true,
                    Mensaje = "El producto se agrego correctamente.",
                    Datos = producto
                };
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Error al crear el producto");
                return new RespuestaApi<Producto>
                {
                    Exito = false,
                    Mensaje = "Ocurrió un error interno al conectar con la base de datos. Intente más tarde",
                    Datos = null
                };
            }
        }
        public async Task<RespuestaApi<List<Producto>>> ObtenerProductosAsync()
        {
            try
            {
                var productos = await _dbContext.Productos
                .Include(p => p.Categoria)
                .Include(p => p.Proveedor)
                .OrderByDescending(p => p.Activo)
                .ToListAsync();

                if (productos.Count == 0)
                {
                    _logger.LogWarning("No se encontraron productos en la base de datos.");
                    return new RespuestaApi<List<Producto>>
                    {
                        Exito = true,
                        Mensaje = "No se encontraron productos",
                        Datos = null
                    };
                }
                productos.ForEach(p => p.Categoria.Productos = null);
                productos.ForEach(p => p.Proveedor.Productos = null);
                _logger.LogInformation("Se encontraron {Count} cantidad de productos.", productos.Count());
                return new RespuestaApi<List<Producto>>
                {
                    Exito = true,
                    Mensaje = "Productos obtenidos correctamente.",
                    Datos = productos
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener los productos.");
                return new RespuestaApi<List<Producto>>
                {
                    Exito = false,
                    Mensaje = "Ocurrió un error interno al conectar con la base de datos. Intente más tarde",
                    Datos = null
                };
            }
        }
        //agregar el buscar por codigode barras para usar el scanner
        public async Task<RespuestaApi<Producto>> ObtenerProductoPorCodigoAsync(string codigo)
        {
            try
            {
                codigo.Replace(" ", "");
                var productoEncontrado = await _dbContext.Productos
                .Where(p => p.CodigoBarras == codigo)
                .FirstOrDefaultAsync();

                if (productoEncontrado == null)
                {
                    _logger.LogWarning("No se encontro el producto con el codigo de barras: {CodigoBarras}", codigo);
                    return new RespuestaApi<Producto>
                    {
                        Exito = false,
                        Mensaje = "No se pudo encontrar un producto con el codigo de barras.",
                        Datos = null
                    };
                }
                if (!productoEncontrado.Activo)
                {
                    _logger.LogWarning("El producto con codgio de barras: {CodigoBarras} se encuentra desactivado.", productoEncontrado.CodigoBarras);
                    return new RespuestaApi<Producto>
                    {
                        Exito = false,
                        Mensaje = "Verifique que el codigo pertencezca a un producto activo.",
                        Datos = null
                    };
                }

                _logger.LogInformation("Se obtuvo el producto con id: {Id} con el codigo de barras: {CodigoBarras}.", productoEncontrado.Id, productoEncontrado.CodigoBarras);
                return new RespuestaApi<Producto>
                {
                    Exito = true,
                    Mensaje = "El producto se obtuvo correctamente.",
                    Datos = productoEncontrado
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception al obtener el producto por el codigo de barras.");
                return new RespuestaApi<Producto>
                {
                    Exito = false,
                    Mensaje = "Ocurrió un error al obtener el producto por el codigo de barras.",
                    Datos = null
                };
            }
        }

        public async Task<RespuestaApi<List<Producto>>> ObtenerProductosPorFiltrosAsync(string busqueda)
        {

            try
            {
                if (string.IsNullOrWhiteSpace(busqueda))
                {
                    _logger.LogWarning("El campo de busqueda ingreso vacio.");
                    return new RespuestaApi<List<Producto>>
                    {
                        Exito = false,
                        Mensaje = "La búsqueda no puede estar vacía.",
                        Datos = null
                    };
                }

                busqueda = busqueda.Trim().ToLower();

                var productos = await _dbContext.Productos
                .Include(p => p.Categoria)
                .Where(p =>
                EF.Functions.Like(p.Nombre.ToLower(), $"%{busqueda}%") ||
                EF.Functions.Like(p.Descripcion.ToLower(), $"%{busqueda}%") ||
                EF.Functions.Like(p.CodigoBarras.ToLower(), $"%{busqueda}%") ||
                EF.Functions.Like(p.Sku.ToLower(), $"%{busqueda}%") ||
                EF.Functions.Like(p.Marca.ToLower(), $"%{busqueda}%") ||
                EF.Functions.Like(p.Modelo.ToLower(), $"%{busqueda}%") ||
                EF.Functions.Like(p.Categoria.Nombre.ToLower(), $"%{busqueda}%") ||
                (busqueda == "activo" && p.Activo) ||
                (busqueda == "inactivo" && !p.Activo))
                .ToListAsync();

                if (productos.Count == 0)
                {
                    _logger.LogWarning("No se encontraron productos para la busqueda: {busqueda}", busqueda);
                    return new RespuestaApi<List<Producto>>
                    {
                        Exito = true,
                        Mensaje = "No se encontraron productos que coincidan con la búsqueda.",
                        Datos = null
                    };
                }

                productos.ForEach(p => p.Categoria.Productos = null);
                _logger.LogInformation("Se obtuvieron los productos exitosamente.");
                return new RespuestaApi<List<Producto>>
                {
                    Exito = true,
                    Mensaje = "Productos obtenidos correctamente.",
                    Datos = productos
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener los producto que coincidan con la busqueda: {busqueda}", busqueda);
                return new RespuestaApi<List<Producto>>
                {
                    Exito = false,
                    Mensaje = "Ocurrió un error interno al conectar con la base de datos. Intente más tarde",
                    Datos = null
                };
            }
        }
        public async Task<RespuestaApi<Producto>> EditarProductoAsync(EditarProductoDto editarProductoDto)
        {
            using var tx = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var productoExistente = await _dbContext.Productos.FindAsync(editarProductoDto.Id);
                if (productoExistente == null)
                {
                    await tx.RollbackAsync();
                    _logger.LogWarning("No se encontro el producto: {Nombre}.", editarProductoDto.Nombre);
                    return new RespuestaApi<Producto>
                    {
                        Exito = false,
                        Mensaje = "El producto que intentas editar no existe en la base de datos.",
                        Datos = null
                    };
                }
                if (!string.IsNullOrWhiteSpace(editarProductoDto.Sku))
                {
                    var skuLimpio = editarProductoDto.Sku.Replace(" ", "");
                    var existeSku = await _dbContext.Productos.AnyAsync(p => p.Sku == skuLimpio && p.Id != editarProductoDto.Id);
                    if (existeSku)
                    {
                        await tx.RollbackAsync();
                        _logger.LogWarning("El sku: {sku} ya se encuentra registrado en otro producto.", editarProductoDto.Sku);
                        return new RespuestaApi<Producto>
                        {
                            Exito = false,
                            Mensaje = "El Sku ya se encuentra registrado en otro producto",
                            Datos = null
                        };
                    }
                }
                if (editarProductoDto.CategoriaId.HasValue && editarProductoDto.CategoriaId > 0)
                {
                    var categoriaNueva = await _dbContext.Categorias
                        .FirstOrDefaultAsync(c => c.Id == editarProductoDto.CategoriaId);

                    if (categoriaNueva == null)
                    {
                        await tx.RollbackAsync();
                        _logger.LogWarning("La categoria no existe.");
                        return new RespuestaApi<Producto>
                        {
                            Exito = false,
                            Mensaje = "La categoría seleccionada no existe.",
                            Datos = null
                        };
                    }
                    productoExistente.CategoriaId = editarProductoDto.CategoriaId;
                }
                if (editarProductoDto.ProveedorId.HasValue && editarProductoDto.ProveedorId > 0)
                {
                    var proveedorNuevo = await _dbContext.Proveedores
                        .FirstOrDefaultAsync(p => p.Id == editarProductoDto.ProveedorId);
                    if (proveedorNuevo == null)
                    {
                        await tx.RollbackAsync();
                        _logger.LogWarning("El proveedor no existe.");
                        return new RespuestaApi<Producto>
                        {
                            Exito = false,
                            Mensaje = "el proveedor seleccionado no existe.",
                            Datos = null
                        };
                    }
                    productoExistente.ProveedorId = editarProductoDto.ProveedorId;
                    productoExistente.Proveedor = proveedorNuevo;
                }
                if (!string.IsNullOrWhiteSpace(editarProductoDto.CodigoBarras))
                {
                    editarProductoDto.CodigoBarras = editarProductoDto.CodigoBarras.Replace(" ", "");
                    var codigoBarrasExiste = await _dbContext.Productos
                        .AnyAsync(p => p.CodigoBarras == editarProductoDto.CodigoBarras);
                    if (codigoBarrasExiste)
                    {
                        await tx.RollbackAsync();
                        _logger.LogWarning("El codigo de barras: {codigoBarras} se encuentra registrado en otro producto.", editarProductoDto.CodigoBarras);
                        return new RespuestaApi<Producto>
                        {
                            Exito = false,
                            Mensaje = "El codigo de barras ya existe en otro producto.",
                            Datos = null
                        };
                    }
                    productoExistente.CodigoBarras = editarProductoDto.CodigoBarras;
                }
                if (!string.IsNullOrWhiteSpace(editarProductoDto.Nombre))
                {
                    productoExistente.Nombre = editarProductoDto.Nombre;
                }
                if (!string.IsNullOrWhiteSpace(editarProductoDto.Descripcion))
                {
                    productoExistente.Descripcion = editarProductoDto.Descripcion;
                }
                if (!string.IsNullOrWhiteSpace(editarProductoDto.Marca))
                {
                    productoExistente.Marca = editarProductoDto.Marca;
                }
                if (!string.IsNullOrWhiteSpace(editarProductoDto.Modelo))
                {
                    productoExistente.Modelo = editarProductoDto.Modelo;
                }
                if (editarProductoDto.PrecioCompra > 0)
                {
                    productoExistente.PrecioCompra = editarProductoDto.PrecioCompra;
                }
                if (editarProductoDto.PrecioVenta > 0)
                {
                    productoExistente.PrecioVenta = editarProductoDto.PrecioVenta;
                }
                if (editarProductoDto.Stock >= 0)
                {
                    productoExistente.Stock = editarProductoDto.Stock;
                }

                await _dbContext.SaveChangesAsync();
                await tx.CommitAsync();
                _logger.LogInformation("Se actualizo correctamente el producto: {Nombre} correctamente.", productoExistente.Nombre);
                return new RespuestaApi<Producto>
                {
                    Exito = true,
                    Mensaje = "Producto actualizado con exito.",
                    Datos = productoExistente
                };
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Error al editar el producto: {Nombre}", editarProductoDto.Nombre);
                return new RespuestaApi<Producto>
                {
                    Exito = false,
                    Mensaje = "Hubo un error con el servidor, intentelo de nuevo mas tarde.",
                    Datos = null
                };
            }
        }
        public async Task<RespuestaApi<Producto>> EditarEstadoAsync(EditarEstadoDto editarEstadoDto)
        {
            try
            {
                var producto = await _dbContext.Productos.FirstOrDefaultAsync(p => p.Id == editarEstadoDto.Id);
                if (producto == null)
                {
                    _logger.LogWarning("El producto con id: {Id} no existe.", editarEstadoDto.Id);
                    return new RespuestaApi<Producto>
                    {
                        Exito = false,
                        Mensaje = "El producto no existe.",
                        Datos = null
                    };
                }
                producto.Activo = editarEstadoDto.NuevoEstado;
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("El estado del producto se actualizo correctamente.");
                return new RespuestaApi<Producto>
                {
                    Exito = true,
                    Mensaje = "El estado del producto se actualizo correctamente.",
                    Datos = producto
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar el estado del producto con id: {Id}", editarEstadoDto.Id);
                return new RespuestaApi<Producto>
                {
                    Exito = false,
                    Mensaje = "Ocurrió un error interno al conectar con la base de datos. Intente más tarde",
                    Datos = null
                };
            }
        }

    }
}