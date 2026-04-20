using maverickApi.Models;

namespace maverickApi.Interfaces
{
    public interface IVentaService
    {
        Task<RespuestaApi<Venta>> CrearVentaAsync(Venta venta);
        Task<RespuestaApi<List<Venta>>> ObtenerVentasAsync();
        Task<int> ObtenerSiguienteVentaAsync();
    }
}