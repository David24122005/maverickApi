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
        public ProductoService(ApplicationDbContext DbContext, IConfiguration Configuration, IHttpContextAccessor httpContextAccessor)
        {
            _dbContext = DbContext;
            _configuration = Configuration;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<RespuestaApi<Producto>> CrearProductoAsync(Producto producto)
        {
            using var tx = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                if (string.IsNullOrWhiteSpace(producto.CodigoBarras) || string.IsNullOrWhiteSpace(producto.Nombre) || string.IsNullOrWhiteSpace(producto.Descripcion) || string.IsNullOrWhiteSpace(producto.Sku) || string.IsNullOrWhiteSpace(producto.Marca) || producto.PrecioCompra <= 0 || producto.PrecioVenta <= 0 || string.IsNullOrWhiteSpace(producto.Modelo) || producto.CategoriaId <= 0 || producto.ProveedorId <= 0)
                {
                    await tx.RollbackAsync();
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

                return new RespuestaApi<Producto>
                {
                    Exito = true,
                    Mensaje = "El producto se agrego correctamente.",
                    Datos = producto
                };
            }
            catch
            {
                await tx.RollbackAsync();
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
                    return new RespuestaApi<List<Producto>>
                    {
                        Exito = true,
                        Mensaje = "No se encontraron productos",
                        Datos = null
                    };
                }
                productos.ForEach(p => p.Categoria.Productos = null);
                productos.ForEach(p => p.Proveedor.Productos = null);

                return new RespuestaApi<List<Producto>>
                {
                    Exito = true,
                    Mensaje = "Productos obtenidos correctamente.",
                    Datos = productos
                };
            }
            catch
            {
                return new RespuestaApi<List<Producto>>
                {
                    Exito = false,
                    Mensaje = "Ocurrió un error interno al conectar con la base de datos. Intente más tarde",
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
                    return new RespuestaApi<List<Producto>>
                    {
                        Exito = true,
                        Mensaje = "No se encontraron productos que coincidan con la búsqueda.",
                        Datos = null
                    };
                }

                productos.ForEach(p => p.Categoria = null);

                return new RespuestaApi<List<Producto>>
                {
                    Exito = true,
                    Mensaje = "Productos obtenidos correctamente.",
                    Datos = productos
                };
            }
            catch
            {
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
                    editarProductoDto.CodigoBarras.Replace(" ", "");
                    var codigoBarrasExiste = await _dbContext.Productos
                        .AnyAsync(p => p.CodigoBarras == editarProductoDto.CodigoBarras);
                    if (codigoBarrasExiste)
                    {
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
                return new RespuestaApi<Producto>
                {
                    Exito = true,
                    Mensaje = "Producto actualizado con exito.",
                    Datos = productoExistente
                };
            }
            catch
            {
                await tx.RollbackAsync();
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
                    return new RespuestaApi<Producto>
                    {
                        Exito = false,
                        Mensaje = "El producto no existe.",
                        Datos = null
                    };
                }
                producto.Activo = editarEstadoDto.NuevoEstado;
                await _dbContext.SaveChangesAsync();
                return new RespuestaApi<Producto>
                {
                    Exito = true,
                    Mensaje = "El estado del producto se actualizo correctamente.",
                    Datos = producto
                };
            }
            catch
            {
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