using maverickApi.Data;
using maverickApi.Interfaces;
using maverickApi.Models;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace maverickApi.Services
{
    public class VentaService : IVentaService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<VentaService> _logger;
        public VentaService(ApplicationDbContext DbContext, IConfiguration Configuration, IHttpContextAccessor httpContextAccessor, ILogger<VentaService> logger)
        {
            _dbContext = DbContext;
            _configuration = Configuration;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }
        public async Task<RespuestaApi<Venta>> CrearVentaAsync(Venta venta)
        {
            using var tx = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var user = _httpContextAccessor.HttpContext?.User;
                var usuarioId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value ?? "0");

                if (usuarioId == 0)
                {
                    _logger.LogWarning("El usuario no se autentico correctamente.");
                    return new RespuestaApi<Venta>
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
                    _logger.LogWarning("El usuario con id: {Id} no se encontro en la base de datos.", usuarioId);
                    return new RespuestaApi<Venta>
                    {
                        Exito = false,
                        Mensaje = "El usuario no existe, reinicia la app.",
                        Datos = null
                    };
                }
                var cliente = await _dbContext.Clientes.FirstOrDefaultAsync(c => c.Id == venta.ClienteId);
                if (venta.ClienteId != 0)
                {
                    if (cliente == null)
                    {
                        await tx.RollbackAsync();
                        _logger.LogWarning("El cliente con id: {Id} no es encontro.", venta.ClienteId);
                        return new RespuestaApi<Venta>
                        {
                            Exito = false,
                            Mensaje = "El cliente no se registro correctamente, reintentalo de nuevo.",
                            Datos = null
                        };
                    }
                }
                var nuevaVenta = new Venta
                {
                    NumeroVenta = $"001-{DateTime.Now:yyyyMMdd}-{await ObtenerSiguienteVentaAsync():D8}",
                    UsuarioId = usuarioId,
                    ClienteId = venta.ClienteId,
                    Cliente = cliente ?? null
                };

                nuevaVenta.Detalles = new List<DetalleVenta>();

                decimal SubtotalBruto = 0;
                foreach (var item in venta.Detalles)
                {
                    var producto = await _dbContext.Productos
                    .Include(p => p.Categoria)
                    .Include(p => p.Proveedor)
                    .Where(p => p.Id == item.ProductoId)
                    .FirstOrDefaultAsync();
                    if (producto == null)
                    {
                        await tx.RollbackAsync();
                        _logger.LogWarning("No se encontro el producto con id: {Id}.", item.Id);
                        return new RespuestaApi<Venta>
                        {
                            Exito = false,
                            Mensaje = "No se encontro algun producto, intentelo nuevamente.",
                            Datos = null
                        };
                    }
                    if (producto.Stock < item.Cantidad)
                    {
                        await tx.RollbackAsync();
                        _logger.LogWarning("No existe stock suficiente para el producto con id: {Id}.", item.Id);
                        return new RespuestaApi<Venta>
                        {
                            Exito = false,
                            Mensaje = $"Stock insuficiente para ''{producto.Nombre}",
                            Datos = null
                        };
                    }
                    var subDetalle = producto.PrecioVenta * item.Cantidad;
                    nuevaVenta.Detalles.Add(new DetalleVenta
                    {
                        ProductoId = item.ProductoId,
                        Producto = producto,
                        Cantidad = item.Cantidad,
                        PrecioUnitario = producto.PrecioVenta,
                        Subtotal = subDetalle
                    });
                    producto.Stock -= item.Cantidad;
                    SubtotalBruto += subDetalle;
                }
                nuevaVenta.SubtotalBruto = SubtotalBruto;
                nuevaVenta.Descuento = venta.Descuento;
                nuevaVenta.Subtotal = SubtotalBruto - nuevaVenta.Descuento;
                nuevaVenta.Iva = Math.Round(nuevaVenta.Subtotal * 0.16m, 2);
                nuevaVenta.Total = nuevaVenta.Subtotal + nuevaVenta.Iva;

                _dbContext.Ventas.Add(nuevaVenta);
                await _dbContext.SaveChangesAsync();
                await tx.CommitAsync();

                nuevaVenta.Usuario.BorrarHash();

                _logger.LogInformation("Venta registrada exitosamente. Con id: {Id}, con un monto total de: ${Total}", nuevaVenta.Id, nuevaVenta.Total);
                return new RespuestaApi<Venta>
                {
                    Exito = true,
                    Mensaje = "Venta creada con exito",
                    Datos = nuevaVenta
                };
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Excepcion al crear una nueva venta.");
                return new RespuestaApi<Venta>
                {
                    Exito = false,
                    Mensaje = "Hubo un error con la base de datos, intentelo de nuevo.",
                    Datos = null
                };
            }
        }
        public async Task<RespuestaApi<List<Venta>>> ObtenerVentasAsync()
        {
            try
            {
                var ventas = await _dbContext.Ventas
                .Include(v => v.Cliente)
                .Include(v => v.Usuario)
                .Include(v => v.Detalles)
                .ThenInclude(d => d.Producto)
                .OrderByDescending(v => v.FechaCreacion)
                .ToListAsync();

                if (ventas == null || ventas.Count == 0)
                {
                    _logger.LogInformation("No se encontraron ventas.");
                    return new RespuestaApi<List<Venta>>
                    {
                        Exito = false,
                        Mensaje = "No se encontraron ventas",
                        Datos = null
                    };
                }
                _logger.LogInformation("Se obtuvieron {Count} ventas en la base de datos.", ventas.Count());
                return new RespuestaApi<List<Venta>>
                {
                    Exito = true,
                    Mensaje = "Se obtuvieron las ventas con exito",
                    Datos = ventas
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excepcion al obtener las ventas.");
                return new RespuestaApi<List<Venta>>
                {
                    Exito = false,
                    Mensaje = "Hubo un error con la base de datos, intentelo de nuevo.",
                    Datos = null
                };
            }
        }
        public async Task<int> ObtenerSiguienteVentaAsync()
        {
            var fechaHoy = DateTime.Now.ToString("yyyyMMdd");

            var ultimaVenta = await _dbContext.Ventas
            .Where(v => v.NumeroVenta.StartsWith($"001-{fechaHoy}"))
            .OrderByDescending(v => v.NumeroVenta)
            .FirstOrDefaultAsync();

            if (ultimaVenta == null)
            {
                return 1;
            }

            var partes = ultimaVenta.NumeroVenta?.Split("-");
            if (partes?.Length == 3)
            {
                var ultimoNumeroVenta = int.Parse(partes[2]);
                return ultimoNumeroVenta + 1;
            }
            return 1;

        }
    }
}