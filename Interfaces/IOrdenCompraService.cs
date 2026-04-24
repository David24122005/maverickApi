using maverickApi.Models;

namespace maverickApi.Interfaces
{
    public interface IOrdenCompraService
    {
        Task<RespuestaApi<OrdenCompra>> CrearOrdenCompraAsync(OrdenCompra ordenCompra);
        Task<RespuestaApi<List<OrdenCompra>>> ObtenerOrdenesCompraAsync();
        Task<RespuestaApi<OrdenCompra>> EditarOrdenCompraAsync(OrdenCompra ordenCompraEditar);


        Task<int> ObtenerSiguenteCompraAsync();
    }
}