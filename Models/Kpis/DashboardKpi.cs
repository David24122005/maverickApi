namespace maverickApi.Models.Kpis
{
    public class DashboardKpi
    {
        public int VentasHoy { get; set; }
        public decimal TotalVentasHoy { get; set; }
        public int VentasMes { get; set; }
        public decimal TotalVentasMes { get; set; }
        
        public decimal ValorInventario {get;set;}
        public int ProductosBajoStock {get;set;}

        public int OrdenesPendientes {get;set;}
        public decimal TotalOrdenesPendientes {get;set;}
        
    }


}