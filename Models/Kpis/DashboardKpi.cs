namespace maverickApi.Models.Kpis
{
    public class DashboardKpi
    {
        public int VentasHoy { get; set; } = 0;
        public decimal TotalVentasHoy { get; set; } = 0;
        public int VentasMes { get; set; } = 0;
        public decimal TotalVentasMes { get; set; } = 0;

        public decimal ValorInventario { get; set; } = 0;
        public int ProductosBajoStock { get; set; } = 0;

        public int OrdenesPendientes { get; set; } = 0;
        public decimal TotalOrdenesPendientes { get; set; } = 0;

    }


}