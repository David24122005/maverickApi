using System.Linq.Expressions;
using maverickApi.Data;
using maverickApi.Interfaces;
using maverickApi.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.AspNetCore.Http.Features;


namespace maverickApi.Services
{
    public class OrdenCompraService : IOrdenCompraService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IConfiguration _Configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public OrdenCompraService(ApplicationDbContext dbContext, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _dbContext = dbContext;
            _Configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<RespuestaApi<OrdenCompra>> CrearOrdenCompraAsync(OrdenCompra ordenCompra)
        {
            using var tx = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                if (ordenCompra.ProveedorId < 0)
                {
                    await tx.RollbackAsync();
                    return new RespuestaApi<OrdenCompra>
                    {
                        Exito = false,
                        Mensaje = "No debe haber campos vacios.",
                        Datos = null
                    };
                }
                var user = _httpContextAccessor.HttpContext?.User;
                var usuarioId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value ?? "0");

                if (usuarioId == 0)
                {
                    await tx.RollbackAsync();
                    return new RespuestaApi<OrdenCompra>
                    {
                        Exito = false,
                        Mensaje = "El usuario no se autentico correctamente.",
                        Datos = null
                    };
                }
                var usuario = await _dbContext.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId);
                if (usuario == null)
                {
                    await tx.RollbackAsync();
                    return new RespuestaApi<OrdenCompra>
                    {
                        Exito = false,
                        Mensaje = "El usuario no existe, reinicia la app.",
                        Datos = null
                    };
                }


                var proveedor = await _dbContext.Proveedores.FirstOrDefaultAsync(p => p.Id == ordenCompra.ProveedorId);
                if (proveedor == null)
                {
                    await tx.RollbackAsync();
                    return new RespuestaApi<OrdenCompra>
                    {
                        Exito = false,
                        Mensaje = "No se encontro el proveedor, intentelo de nuevo."
                    };
                }
                var nuevaOrden = new OrdenCompra
                {
                    NumeroCompra = $"ord-{DateTime.Now:yyyyMMdd}-{await ObtenerSiguenteCompraAsync():D8}",
                    UsuarioId = usuarioId,
                    ProveedorId = proveedor.Id,
                    Estado = ordenCompra.Estado?.Replace(" ", "").Trim() ?? "Pendiente",
                    Detalles = new List<DetalleOrdenCompra>()
                };
                decimal totalOrden = 0;
                foreach (var item in ordenCompra.Detalles)
                {
                    var producto = await _dbContext.Productos
                    .Where(p => p.ProveedorId == ordenCompra.ProveedorId)
                    .FirstOrDefaultAsync(p => p.Id == item.ProductoId);

                    if (producto == null)
                    {
                        await tx.RollbackAsync();
                        return new RespuestaApi<OrdenCompra>
                        {
                            Exito = false,
                            Mensaje = "No se encontro algun producto o el proveedor no coincide con el del producto, intentelo nuevamente.",
                            Datos = null
                        };
                    }

                    var detalle = new DetalleOrdenCompra
                    {
                        ProductoId = producto.Id,
                        Cantidad = item.Cantidad,
                        PrecioUnitario = producto.PrecioCompra,
                        Total = item.Cantidad * producto.PrecioCompra
                    };
                    nuevaOrden.Detalles.Add(detalle);
                    totalOrden += detalle.Total;
                }

                nuevaOrden.Total = totalOrden;

                _dbContext.OrdenCompras.Add(nuevaOrden);
                await _dbContext.SaveChangesAsync();
                await tx.CommitAsync();
                nuevaOrden.Usuario?.PasswordHash = "";
                nuevaOrden.Proveedor?.Productos = null;

                return new RespuestaApi<OrdenCompra>
                {
                    Exito = true,
                    Mensaje = "La orden se realizo correctamente.",
                    Datos = nuevaOrden
                };
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return new RespuestaApi<OrdenCompra>
                {
                    Exito = false,
                    Mensaje = $"Error: {ex.Message}",
                    Datos = null
                };
            }
        }
        public async Task<RespuestaApi<List<OrdenCompra>>> ObtenerOrdenesCompraAsync()
        {

            try
            {
                var ordenes = await _dbContext.OrdenCompras
                .Include(oc => oc.Proveedor)
                .Include(oc => oc.Usuario)
                .Include(oc => oc.Detalles)
                .OrderByDescending(oc => oc.Fecha)
                .ToListAsync();
                if (ordenes.Count() == 0)
                {
                    return new RespuestaApi<List<OrdenCompra>>
                    {
                        Exito = true,
                        Mensaje = "No se encontraron ordenes de compra.",
                        Datos = ordenes

                    };
                }
                foreach (var item in ordenes)
                {
                    item.Proveedor.Productos = null;
                    item.Usuario.PasswordHash = null;
                }
                return new RespuestaApi<List<OrdenCompra>>
                {
                    Exito = true,
                    Mensaje = "Se obtuvieron las ordenes correctamente.",
                    Datos = ordenes
                };
            }
            catch
            {
                return new RespuestaApi<List<OrdenCompra>>
                {
                    Exito = false,
                    Mensaje = "Ocurrió un error interno al conectar con la base de datos. Intente más tarde.",
                    Datos = null
                };

            }
        }
        public async Task<RespuestaApi<OrdenCompra>> EditarOrdenCompraAsync(OrdenCompra ordenCompraEditar)
        {
            using var tx = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var ordenExistente = await _dbContext.OrdenCompras
                .Include(oc => oc.Proveedor)
                .Include(oc => oc.Usuario)
                .Include(oc => oc.Detalles)
                .FirstOrDefaultAsync(oc => oc.Id == ordenCompraEditar.Id);
                if (ordenExistente == null)
                {
                    return new RespuestaApi<OrdenCompra>
                    {
                        Exito = false,
                        Mensaje = "No se encontro la orden de compra.",
                        Datos = null
                    };
                }
                if (ordenExistente.Estado == "Recibida" || ordenExistente.Estado == "Cancelada")
                {
                    await tx.RollbackAsync();
                    return new RespuestaApi<OrdenCompra>
                    {
                        Exito = false,
                        Mensaje = "No se puede editar una orden si ya a sido Recibida o Cancelada.",
                        Datos = ordenExistente
                    };
                }

                var user = _httpContextAccessor.HttpContext?.User;
                var usuarioId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                               user.FindFirst("sub")?.Value ?? "0");
                if (usuarioId == 0)
                {
                    await tx.RollbackAsync();
                    return new RespuestaApi<OrdenCompra>
                    {
                        Exito = false,
                        Mensaje = "El usuario no se autentico correctamente",
                        Datos = null
                    };
                }

                if (ordenExistente.ProveedorId != ordenCompraEditar.ProveedorId)
                {
                    var nuevoProveedor = await _dbContext.Proveedores
                    .Where(p => p.Activo == true)
                    .FirstOrDefaultAsync(p => p.Id == ordenCompraEditar.ProveedorId);
                    if (nuevoProveedor == null)
                    {
                        await tx.RollbackAsync();
                        return new RespuestaApi<OrdenCompra>
                        {
                            Exito = false,
                            Mensaje = "El proveedor especificado no existe.",
                            Datos = null
                        };
                    }
                    ordenExistente.ProveedorId = nuevoProveedor.Id;
                    ordenExistente.Proveedor = nuevoProveedor;
                }
                ordenExistente.Estado = ordenCompraEditar.Estado.Trim() ?? ordenExistente.Estado;
                ordenExistente.FechaEntrega = ordenCompraEditar.FechaEntrega ?? ordenExistente.FechaEntrega;

                if (ordenCompraEditar.Detalles != null && ordenCompraEditar.Detalles.Any())
                {
                    _dbContext.DetalleOrdenCompras.RemoveRange(ordenExistente.Detalles);
                    Decimal nuevoTotal = 0;
                    foreach (var item in ordenCompraEditar.Detalles)
                    {
                        var producto = await _dbContext.Productos.FirstOrDefaultAsync(p => p.Id == item.ProductoId);
                        if (producto == null)
                        {
                            await tx.RollbackAsync();
                            return new RespuestaApi<OrdenCompra>
                            {
                                Exito = false,
                                Mensaje = "No se encontro algun producto.",
                                Datos = null
                            };
                        }
                        var nuevoDetalle = new DetalleOrdenCompra
                        {
                            ProductoId = producto.Id,
                            Cantidad = item.Cantidad,
                            PrecioUnitario = producto.PrecioCompra,
                            Total = item.Cantidad * producto.PrecioCompra
                        };
                        nuevoTotal += nuevoDetalle.Total;
                        ordenExistente.Detalles.Add(nuevoDetalle);
                    }
                    ordenExistente.Total = nuevoTotal;
                }
                else
                {
                    ordenExistente.Total = ordenExistente.Detalles?.Sum(d => d.Total) ?? 0;
                }
                await _dbContext.SaveChangesAsync();
                await tx.CommitAsync();

                var ordenActualizada = await _dbContext.OrdenCompras
                .Include(oc => oc.Proveedor)
                .Include(oc => oc.Detalles)
                .Include(oc => oc.Usuario)
                .FirstOrDefaultAsync(oc => oc.Id == ordenExistente.Id);

                ordenActualizada?.Usuario?.PasswordHash = "";

                return new RespuestaApi<OrdenCompra>
                {
                    Exito = true,
                    Mensaje = "La orden se actualizo con Exito",
                    Datos = ordenActualizada
                };


            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return new RespuestaApi<OrdenCompra>
                {
                    Exito = false,
                    Mensaje = $"Ocurrio Un error al editar la orden.{ex.Message}",
                    Datos = null
                };
            }
        }
        public async Task<int> ObtenerSiguenteCompraAsync()
        {
            var fechaHoy = DateTime.Now.ToString("yyyyMMdd");

            var ultimaCompra = await _dbContext.OrdenCompras
            .Where(oc => oc.NumeroCompra.StartsWith($"ord-{fechaHoy}"))
            .OrderByDescending(oc => oc.NumeroCompra)
            .FirstOrDefaultAsync();

            if (ultimaCompra == null)
            {
                return 1;
            }

            var partes = ultimaCompra.NumeroCompra.Split("-");
            if (partes.Length == 3)
            {
                var ultimoNumeroCompra = int.Parse(partes[2]);
                return ultimoNumeroCompra + 1;
            }
            return 1;

        }
    }
}