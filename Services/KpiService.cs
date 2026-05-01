using maverickApi.Data;
using maverickApi.Models;
using maverickApi.Interfaces;
using maverickApi.Models.Kpis;
using Microsoft.EntityFrameworkCore;

namespace maverickApi.Services
{
    public class KpiService : IKpiService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IConfiguration _Configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<OrdenCompraService> _logger;
        public KpiService(ApplicationDbContext dbContext, IConfiguration configuration, IHttpContextAccessor httpContextAccessor, ILogger<OrdenCompraService> logger)
        {
            _dbContext = dbContext;
            _Configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }
        public async Task<RespuestaApi<DashboardKpi>> ObtenerKpiDashboardAsync()
        {
            var dashboardKpi = new DashboardKpi();
            var fechaHoy = DateTime.Today;

            var queryVentasHoy = _dbContext.Ventas
            .Where(v => v.FechaCreacion.Date == fechaHoy);
            dashboardKpi.VentasHoy = await queryVentasHoy.CountAsync();
            if (dashboardKpi.VentasHoy > 0)
            {
                dashboardKpi.TotalVentasHoy = await queryVentasHoy.SumAsync(v => v.Subtotal);
            }

            var queryVentasMes = _dbContext.Ventas
            .Where(v => v.FechaCreacion.Month == fechaHoy.Month && v.FechaCreacion.Year == fechaHoy.Year);
            dashboardKpi.VentasMes = await queryVentasMes.CountAsync();
            if (dashboardKpi.VentasMes > 0)
            {
                dashboardKpi.TotalVentasMes = await queryVentasMes.SumAsync(v => v.Subtotal);
            }

            var queryProductos = _dbContext.Productos;
            dashboardKpi.ValorInventario = await queryProductos.SumAsync(p => p.PrecioCompra * p.Stock);
            dashboardKpi.ProductosBajoStock = await queryProductos.Where(p => p.Stock <= 10).CountAsync();

            var queryOrdenesPendientes = _dbContext.OrdenCompras.Where(oc => oc.Estado == "Pendiente");
            dashboardKpi.OrdenesPendientes = await queryOrdenesPendientes.CountAsync();
            if (dashboardKpi.OrdenesPendientes > 0)
            {
                dashboardKpi.TotalOrdenesPendientes = await queryOrdenesPendientes.SumAsync(oc => oc.Total);
            }

            _logger.LogInformation("KPIs procesados correctamente.");
            return new RespuestaApi<DashboardKpi>
            {
                Exito = true,
                Mensaje = "Datos del dashboard obtenidos.",
                Datos = dashboardKpi
            };
        }
    }
}