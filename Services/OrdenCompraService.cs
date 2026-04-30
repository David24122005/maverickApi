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
        private readonly ILogger<OrdenCompraService> _logger;
        public OrdenCompraService(ApplicationDbContext dbContext, IConfiguration configuration, IHttpContextAccessor httpContextAccessor, ILogger<OrdenCompraService> logger)
        {
            _dbContext = dbContext;
            _Configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<RespuestaApi<OrdenCompra>> CrearOrdenCompraAsync(OrdenCompra ordenCompra)
        {
            using var tx = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                if (ordenCompra.ProveedorId < 0 || ordenCompra.Detalles.Count() == 0)
                {
                    await tx.RollbackAsync();
                    _logger.LogWarning("Validacion fallida al crear orden de compra. Faltan campos obligatorios. Id de proveedor: {Id}, catidad de detalles: {Detalles}.", ordenCompra.ProveedorId, ordenCompra.Detalles?.Count() ?? 0);
                    return new RespuestaApi<OrdenCompra>
                    {
                        Exito = false,
                        Mensaje = "No debe haber campos vacios.",
                        Datos = null
                    };
                }
                var user = _httpContextAccessor.HttpContext?.User;
                var usuarioId = int.Parse(user?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user?.FindFirst("sub")?.Value ?? "0");

                if (usuarioId == 0)
                {
                    await tx.RollbackAsync();
                    _logger.LogWarning("El usuario no se autentico correctamente.");
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
                    _logger.LogWarning("El usuario con id: {Id} no existe en la base de datos.", usuarioId);
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
                    _logger.LogWarning("El proveedor con id: {Id} no existe en la base de datos.", ordenCompra.ProveedorId);
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
                        _logger.LogWarning("El producto con id: {Id} no existe en la base de datos o no pertenece al proveedor con id: {Id}.", item.ProductoId, ordenCompra.ProveedorId);
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

                _logger.LogInformation("Orden de compra registrada correctamente con id: {Id} con un monto total de {Total} pesos mexicanos.", nuevaOrden.Id, nuevaOrden.Total);
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
                _logger.LogError(ex, "Excepcion al crear la orden de compra.");
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
                    _logger.LogWarning("No se encontraron ordenes de compra en la base de datos.");
                    return new RespuestaApi<List<OrdenCompra>>
                    {
                        Exito = true,
                        Mensaje = "No se encontraron ordenes de compra.",
                        Datos = ordenes

                    };
                }
                foreach (var item in ordenes)
                {
                    item.Proveedor?.Productos = null;
                    item.Usuario?.PasswordHash = null;
                }
                _logger.LogInformation("Se obtuvieron {Count} ordenes de compra de la base de datos.", ordenes.Count());
                return new RespuestaApi<List<OrdenCompra>>
                {
                    Exito = true,
                    Mensaje = "Se obtuvieron las ordenes correctamente.",
                    Datos = ordenes
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excepcion al obtener ordenes de compras.");
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
                    _logger.LogWarning("No se encontro la orden de compra con id: {Id}.", ordenCompraEditar.Id);
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
                    _logger.LogWarning("La orden con id: {Id}, numero de orden: {NumeroCompra} se encuentra con estado: {Estado}", ordenExistente.Id, ordenExistente.NumeroCompra, ordenExistente.Estado);
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
                    _logger.LogWarning("El usuario no se autentico correctamente.");
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
                        _logger.LogWarning("El proveedor con id: {Id} no existe en la base de datos o esta inactivo.", ordenExistente.ProveedorId);
                        return new RespuestaApi<OrdenCompra>
                        {
                            Exito = false,
                            Mensaje = "El proveedor especificado no existe o se encuentra inactivo.",
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
                    foreach (var detalle in ordenExistente.Detalles.ToList())
                    {
                        _dbContext.DetalleOrdenCompras.Remove(detalle);
                    }
                    Decimal nuevoTotal = 0;
                    foreach (var item in ordenCompraEditar.Detalles)
                    {
                        var producto = await _dbContext.Productos
                        .Where(p => p.Activo)
                        .FirstOrDefaultAsync(p => p.Id == item.ProductoId);
                        if (producto == null)
                        {
                            await tx.RollbackAsync();
                            _logger.LogWarning("No se encontro el producto con id: {Id} en la base de datos.", item.ProductoId);
                            return new RespuestaApi<OrdenCompra>
                            {
                                Exito = false,
                                Mensaje = "No se encontro algun producto en la base de datos.",
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
                _logger.LogInformation("La orden con id: {Id} y numero de orden: {NumeroCompra} se actualizo correctamente.", ordenActualizada.Id, ordenActualizada.NumeroCompra);
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
                _logger.LogError(ex, "Excepcion al editar la orden con id: {Id} y numero de orden: {NumeroCompra}.", ordenCompraEditar.Id, ordenCompraEditar.NumeroCompra);
                return new RespuestaApi<OrdenCompra>
                {
                    Exito = false,
                    Mensaje = $"Ocurrio Un error al editar la orden.{ex.Message}",
                    Datos = null
                };
            }
        }

        public async Task<RespuestaApi<OrdenCompra>> MarcarOrdenRecibidaAsync(int id)
        {
            using var tx = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                if (id == 0)
                {
                    await tx.RollbackAsync();
                    _logger.LogWarning("El id recibido no puede ser: {Id}", id);
                    return new RespuestaApi<OrdenCompra>
                    {
                        Exito = false,
                        Mensaje = "Hubo un error al recibir el identificador de la orden de compra.",
                        Datos = null
                    };
                }
                var ordenCompra = await _dbContext.OrdenCompras
                .Where(oc => oc.Id == id)
                .Include(oc => oc.Detalles)
                .FirstOrDefaultAsync();

                if (ordenCompra == null)
                {
                    await tx.RollbackAsync();
                    _logger.LogWarning("La orden de compra con id: {Id} no se encontro en la base de datos.", id);
                    return new RespuestaApi<OrdenCompra>
                    {
                        Exito = false,
                        Mensaje = "No se encontro la orden de compra, intentelo de nuevo mas tarde.",
                        Datos = null
                    };
                }
                if (ordenCompra.Estado == "Cancelada")
                {
                    await tx.RollbackAsync();
                    _logger.LogWarning("La orden de compra con id: {Id} se encuentra con estado cancelada", id);
                    return new RespuestaApi<OrdenCompra>
                    {
                        Exito = false,
                        Mensaje = "La orden de compra se encuentra cancelada, no se puede marcar como recibida.",
                        Datos = null
                    };
                }
                if (ordenCompra.Estado == "Recibida")
                {
                    await tx.RollbackAsync();
                    _logger.LogWarning("La orden de compra con id: {Id} ya estaba marcada como recibida", id);
                    return new RespuestaApi<OrdenCompra>
                    {
                        Exito = false,
                        Mensaje = "La orden de compra ya fue marcada como recibida anteriormente.",
                        Datos = null
                    };
                }

                foreach (var item in ordenCompra.Detalles)
                {
                    var producto = await _dbContext.Productos
                    .Where(p => p.Id == item.ProductoId)
                    .Where(p => p.Activo == true)
                    .FirstOrDefaultAsync();

                    if (producto == null)
                    {
                        await tx.RollbackAsync();
                        _logger.LogWarning("El producto con id: {Id} no se encontro en la base de datos.", item.ProductoId);
                        return new RespuestaApi<OrdenCompra>
                        {
                            Exito = false,
                            Mensaje = $"El producto '{item.Producto?.Nombre ?? "Desconocido"}' " +
                  "ya no está disponible en el catálogo.",
                            Datos = null
                        };
                    }
                    producto.Stock += item.Cantidad;
                }


                ordenCompra.Estado = "Recibida";
                ordenCompra.FechaEntrega = DateTime.Now;
                await _dbContext.SaveChangesAsync();

                await tx.CommitAsync();
                _logger.LogInformation("Se marco como recibida la orden de compra con id: {Id}.", id);
                return new RespuestaApi<OrdenCompra>
                {
                    Exito = true,
                    Mensaje = "La orden se marco como recibida correctamente.",
                    Datos = ordenCompra
                };
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Excepcion al marcar orden como recibida.");
                return new RespuestaApi<OrdenCompra>
                {
                    Exito = false,
                    Mensaje = "Error al marcar orden como recibida.",
                    Datos = null
                };
            }
        }
        public async Task<RespuestaApi<OrdenCompra>> MarcarOrdenCanceladaAsync(int id)
        {
            using var tx = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                if (id == 0)
                {
                    await tx.RollbackAsync();
                    _logger.LogWarning("El id recibido no puede ser: {Id}", id);
                    return new RespuestaApi<OrdenCompra>
                    {
                        Exito = false,
                        Mensaje = "Ocurrio un error al recibir el identificador de la orden de compra.",
                        Datos = null
                    };
                }
                var ordenCompra = await _dbContext.OrdenCompras
                .Where(oc => oc.Id == id)
                .FirstOrDefaultAsync();
                if (ordenCompra == null)
                {
                    await tx.RollbackAsync();
                    _logger.LogWarning("La orden de compra con id: {Id} no se encontro en la base de datos.", id);
                    return new RespuestaApi<OrdenCompra>
                    {
                        Exito = false,
                        Mensaje = "No se encontro la orden de compra.",
                        Datos = null
                    };
                }
                if (ordenCompra.Estado == "Recibida")
                {
                    await tx.RollbackAsync();
                    _logger.LogWarning("La orden de compra con id: {Id} se encuentra con estado Recibida", id);
                    return new RespuestaApi<OrdenCompra>
                    {
                        Exito = false,
                        Mensaje = "La orden que intenta cancelar se encuentra con estado recibida.",
                        Datos = null
                    };
                }

                ordenCompra.Estado = "Cancelada";

                await _dbContext.SaveChangesAsync();
                await tx.CommitAsync();

                _logger.LogInformation("La orden de compra con id: {Id} se cancelo correctamente.", id);
                return new RespuestaApi<OrdenCompra>
                {
                    Exito = true,
                    Mensaje = "Se cancelo correctamente la orden de compra.",
                    Datos = ordenCompra
                };
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Excepcion al marcar la orden con id: {Id} como cancelada.", id);
                return new RespuestaApi<OrdenCompra>
                {
                    Exito = false,
                    Mensaje = "Ocurrio un error al marcar la orden como cancelada.",
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